using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlphaOmega.NamedPipes.DTOs;
using AlphaOmega.NamedPipes.Interfaces;
using AlphaOmega.NamedPipes.Reflection;

namespace AlphaOmega.NamedPipes
{
	public sealed class RegistryServer : PipeServerBase, IRegisterServer
	{
		internal const String RegistryPipeName = "AlphaOmega.NamedPipes.Registry";

		private readonly Dictionary<String, ServerSideWorker> _workers = new Dictionary<String, ServerSideWorker>(StringComparer.OrdinalIgnoreCase);
		private readonly ConcurrentDictionary<Guid, ServerSideConnection> _activeConnections = new ConcurrentDictionary<Guid, ServerSideConnection>();

		private readonly CancellationTokenSource _cts = new CancellationTokenSource();

		public event Func<PipeMessage, CancellationToken, Task<PipeMessage>> RequestReceived;
		public event Func<String, Task> WorkerConnected;
		public event Func<String, Task> WorkerDisconnected;

		public RpcResponseChannel ResponseChannel { get; } = new RpcResponseChannel();

		public Boolean IsStarted { get; private set; }

		public String PipeName { get; }

		IEnumerable<String> IRegisterServer.ConnectedWorkerIDs => this._workers.Keys;

		public RegistryServer()
			: this(RegistryPipeName) { }

		public RegistryServer(String pipeName)
			=> this.PipeName = pipeName;

		public async Task StartAsync(CancellationToken token)
		{
			using(var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(this._cts.Token, token))
			{
				var linkedToken = linkedCts.Token;

				NamedPipeServerStream pipe = null;
				try
				{
					this.IsStarted = true;
					while(!linkedToken.IsCancellationRequested)
					{
						pipe = new NamedPipeServerStream(this.PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

						try
						{
							await pipe.WaitForConnectionAsync(linkedToken);
							var connectionId = Guid.NewGuid();
							_ = ListenConnectionAsync(new ServerSideConnection(connectionId, pipe), linkedToken);
						} catch(OperationCanceledException)
						{
							pipe.Dispose();
							break;
						}
					}
				} finally
				{
					this.IsStarted = false;
					pipe?.Dispose();
				}
			}
		}

		public T CreateProcessingLogic<T>() where T : class
			=> RemoteProcessingLogicBuilder.CreateProcessingLogic<T>(this, this._cts.Token);

		public T CreateProcessingLogicForWorker<T>(String workerId) where T : class
			=> RemoteProcessingLogicBuilder.CreateProcessingLogicForWorker<T>(this, workerId, this._cts.Token);

		private async Task ListenConnectionAsync(ServerSideConnection connection, CancellationToken token)
		{
			try
			{
				this._activeConnections.TryAdd(connection.ConnectionId, connection);

				// First message must be RegisterWorker
				PipeMessage firstMessage = await PipeProtocol.ReadMessageAsync(connection.Pipe, token);
				if(firstMessage.Type != PipeMessageType.RegisterWorker.ToString())
					throw new InvalidOperationException($"Expected {PipeMessageType.RegisterWorker}, got {firstMessage.Type}");

				var registerPayload = firstMessage.Deserialize<RegisterWorkerRequest>();
				var worker = new ServerSideWorker(registerPayload.WorkerId, registerPayload.PipeName, connection.ConnectionId);

				var listenTask = this.ListenLoopAsync(connection, HandleConnectionMessageAsync, token);

				await this.RegisterWorker(worker);

				await listenTask;
			} catch(EndOfStreamException)
			{
				Console.WriteLine($"Connection {connection.ConnectionId} closed");
			} catch(OperationCanceledException)
			{
				// Server shutting down
			} catch(Exception exc)
			{
				Console.WriteLine($"Error in connection {connection.ConnectionId}: {exc}");
			} finally
			{
				// Unregister any worker on this connection
				var workerToRemove = this._workers.Values.FirstOrDefault(w => w.ConnectionId == connection.ConnectionId);
				if(workerToRemove != null)
					await this.UnregisterWorker(workerToRemove);

				this._activeConnections.TryRemove(connection.ConnectionId, out _);
				connection.Dispose();
			}
		}

		private async Task<PipeMessage> HandleConnectionMessageAsync(PipeMessage message, CancellationToken token)
		{
			if(this.ResponseChannel.CompleteResponse(message, message))
				return null; // Don't send response to a response.

			return this.RequestReceived != null
				? await this.RequestReceived.Invoke(message, token)
				: null;
		}

		public async Task SendRequestToWorkers(PipeMessage request, CancellationToken token)
		{
			ServerSideWorker[] workers = this._workers.Values.ToArray();
			foreach(ServerSideWorker worker in workers)
				await this.SendRequestToWorker(worker.WorkerId, request, token);
		}

		public async Task<PipeMessage> SendRequestToWorker(String workerId, PipeMessage request, CancellationToken token)
		{
			if(!this._workers.TryGetValue(workerId, out var worker))
				throw new InvalidOperationException($"Worker {workerId} is not registered.");

			if(!this._activeConnections.TryGetValue(worker.ConnectionId, out var connection))
				throw new IOException($"Connection for worker {workerId} is no longer active.");

			Task<PipeMessage> responseTask = this.ResponseChannel.WaitForResponseAsync(request, TimeSpan.FromSeconds(30));

			try
			{
				// Send the request
				await this.SendMessageAsync(connection, request, token);
			} catch(EndOfStreamException)
			{
				await this.UnregisterWorker(worker);
				this.ResponseChannel.FailResponse(request, new IOException("Failed to send to worker"));
				return null;
			}

			return await responseTask;
		}

		private async Task RegisterWorker(ServerSideWorker worker)
		{
			this._workers[worker.WorkerId] = worker;
			Console.WriteLine($"Registering worker {worker.WorkerId} at pipe {worker.WorkerPipeName}. Total: {this._workers.Count:N0}");

			if(this.WorkerConnected != null)
				await this.WorkerConnected.Invoke(worker.WorkerId);
		}

		private async Task UnregisterWorker(ServerSideWorker worker)
		{
			this._workers.Remove(worker.WorkerId);
			Console.WriteLine($"Unregistering worker {worker.WorkerId} at pipe {worker.WorkerPipeName}. Total: {this._workers.Count:N0}");

			if(this.WorkerDisconnected != null)
				await this.WorkerDisconnected.Invoke(worker.WorkerId);
		}

		public async Task StopAsync()
		{
			if(!this._cts.IsCancellationRequested)
				this._cts.Cancel();

			var closeTimeout = Task.Delay(TimeSpan.FromSeconds(5));
			var allClosed = Task.Run(() =>
			{
				while(this._activeConnections.Count > 0)
					Thread.Sleep(10);
			});

			await Task.WhenAny(allClosed, closeTimeout);

			// Forcefully close any remaining connections
			foreach(var connection in this._activeConnections.Values.ToArray())
				connection.Dispose();

			this._activeConnections.Clear();
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);

			this._cts.Cancel();
			this._cts.Dispose();
		}
	}
}