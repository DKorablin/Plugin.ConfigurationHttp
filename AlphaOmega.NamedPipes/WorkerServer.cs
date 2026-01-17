using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AlphaOmega.NamedPipes.DTOs;
using AlphaOmega.NamedPipes.Interfaces;

namespace AlphaOmega.NamedPipes
{
	public sealed class WorkerServer : PipeServerBase, IWorkerServer
	{
		private ServerSideConnection _connection;
		private CancellationTokenSource _cts;
		private readonly Object _workerLogic;
		private Task _listenTask;
		private Boolean _cleanedUp = false;

		public event Func<PipeMessage, CancellationToken, Task<PipeMessage>> RequestReceived;
		public event Func<Task> ConnectionLost;

		public String RegistryPipeName { get; }
		public String WorkerId { get; }
		public String PipeName { get; }

		public Boolean IsStarted { get; private set; }

		public WorkerServer(Object workerLogic)
			: this(RegistryServer.RegistryPipeName, "AlphaOmega.NamedPipes.Worker.", Guid.NewGuid().ToString("N"), workerLogic)
		{
		}

		public WorkerServer(String registryPipeName, String workerPipeName, String workerId, Object workerLogic)
		{
			if(String.IsNullOrWhiteSpace(registryPipeName))
				throw new ArgumentNullException(nameof(registryPipeName));
			if(String.IsNullOrWhiteSpace(workerId))
				throw new ArgumentNullException(nameof(workerId));
			if(String.IsNullOrWhiteSpace(workerPipeName))
				throw new ArgumentNullException(nameof(workerPipeName));

			this.RegistryPipeName = registryPipeName ?? throw new ArgumentNullException(nameof(registryPipeName));
			this.WorkerId = workerId ?? throw new ArgumentNullException(nameof(workerId));
			this.PipeName = workerPipeName + this.WorkerId;

			this._workerLogic = workerLogic ?? throw new ArgumentNullException(nameof(workerLogic));
		}

		/// <inheritdoc/>
		public async Task StartAsync(CancellationToken token)
		{
			if(this._cts == null || this._cts.IsCancellationRequested)
				this._cts = new CancellationTokenSource();

			this._cleanedUp = false;
			CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(this._cts.Token, token);

			await this.RegisterWorkerAsync(linkedCts.Token);
			this._listenTask = Task.Run(() => this.ListenAsync(linkedCts), linkedCts.Token);
		}

		private async Task RegisterWorkerAsync(CancellationToken token)
		{
			var registryPipe = new NamedPipeClientStream(".", this.RegistryPipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

			Console.WriteLine("Connecting to registry...");
			await registryPipe.ConnectAsync(5000, token);

			this._connection = new ServerSideConnection(registryPipe);

			Console.WriteLine("Registering with registry...");
			await SendMessageAsync(this._connection,
				new PipeMessage(PipeMessageType.RegisterWorker.ToString(), new RegisterWorkerRequest(this.WorkerId, this.PipeName)),
				token);

			Console.WriteLine("Connected to registry");
			this.IsStarted = true;
		}

		private async Task ListenAsync(CancellationTokenSource linkedCtsToken)
		{
			try
			{
				await this.ListenLoopAsync(this._connection, this.HandleMessageAsync, linkedCtsToken.Token);
			}
			catch(OperationCanceledException)
			{
				// Expected on shutdown
			} catch(IOException exc)
			{
				Console.WriteLine($"Worker listener error: {exc.Message}");
				this._cts.Cancel();
			}
			catch(Exception exc)
			{
				Console.WriteLine($"Worker listener critical error: {exc.Message}");
			}
			finally
			{
				await this.CleanupAsync();
				linkedCtsToken.Dispose();
			}
		}

		private async Task<PipeMessage> HandleMessageAsync(PipeMessage message, CancellationToken token)
		{
			try
			{
				var result = await this.InvokeMethodAsync(message, token);

				return result == null
					? new PipeMessage(message, PipeMessageType.Null.ToString(), new NullResponse())
					: result;
			}catch(InvalidOperationException exc)
			{
				return new PipeMessage(message, PipeMessageType.Error.ToString(), new ErrorResponse(exc.Message));
			}
			catch(Exception exc)
			{
				Console.WriteLine($"Error handling message {message.Type}: {exc.Message}");
				return new PipeMessage(message, PipeMessageType.Error.ToString(), new ErrorResponse(exc.Message));
			}
		}

		/// <inheritdoc/>
		public async Task StopAsync()
		{
			if(!this.IsStarted) return;

			if(this._cts?.IsCancellationRequested == false)
				this._cts?.Cancel();

			if(this._listenTask != null)
				await Task.WhenAny(this._listenTask, Task.Delay(TimeSpan.FromSeconds(2)));

			if(this._cts != null)
			{
				this._cts.Dispose();
				this._cts = null;
			}
		}

		private async Task CleanupAsync()
		{
			if(this._cleanedUp)
				return;//We don't need to invoke ConnectionLost event multiple times

			this._cleanedUp = true;

			this._connection?.Dispose();
			this._connection = null;

			// Notify subscribers
			if(this.ConnectionLost != null)
				await this.ConnectionLost.Invoke();

			this.IsStarted = false;
			Console.WriteLine("Worker server fully stopped and cleaned up.");
		}

		public async Task<PipeMessage> TryHandleAsync(PipeMessage request, CancellationToken token)
		{
			return this.RequestReceived == null
				? null
				: await this.RequestReceived.Invoke(request, token);
		}

		private async Task<PipeMessage> InvokeMethodAsync(PipeMessage message, CancellationToken token)
		{
			String methodName = message.Type;

			MethodInfo method = this._workerLogic.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance)
				?? throw new InvalidOperationException($"Method {methodName} not found in the {this._workerLogic.GetType()}");

			ParameterInfo[] parameters = method.GetParameters();
			Type[] requestTypes = Array.ConvertAll(parameters, p => p.ParameterType);
			Object[] requestPayload = message.Deserialize(requestTypes);

			Object resultValue = method.Invoke(this._workerLogic, requestPayload);
			if(resultValue is Task task)
			{
				await task;

				var returnType = method.ReturnType;
				if(returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
				{
					var property = task.GetType().GetProperty(nameof(Task<Object>.Result));
					Object result = property.GetValue(task);
					return new PipeMessage(message, method.Name, result);
				}

				// Task without result
				return null;
			} else if(resultValue != null)
				return new PipeMessage(message, method.Name, resultValue);

			return null;
		}

		public void Dispose()
		{
			_ = Task.Run(this.CleanupAsync);

			if(this._cts != null)
			{
				this._cts.Cancel();
				this._cts.Dispose();
				this._cts = null;
			}
			this._connection?.Dispose();
		}
	}
}