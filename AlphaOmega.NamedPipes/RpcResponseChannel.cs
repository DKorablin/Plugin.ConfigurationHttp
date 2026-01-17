using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AlphaOmega.NamedPipes.DTOs;

namespace AlphaOmega.NamedPipes
{
	public sealed class RpcResponseChannel
	{
		private readonly ConcurrentDictionary<Guid, TaskCompletionSource<PipeMessage>> _pendingResponses = new ConcurrentDictionary<Guid, TaskCompletionSource<PipeMessage>>();

		/// <summary>Registers a pending request and returns a task that completes when the response arrives.</summary>
		public Task<PipeMessage> WaitForResponseAsync(PipeMessage message, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<PipeMessage>();

			if(!this._pendingResponses.TryAdd(message.MessageId, tcs))
				throw new InvalidOperationException($"Request already pending. Message={message.MessageId}");

			var cts = new CancellationTokenSource(timeout);
			cts.Token.Register(() =>
			{
				this._pendingResponses.TryRemove(message.MessageId, out _);
				tcs.TrySetException(new TimeoutException($"RPC call timed out after {timeout.TotalSeconds} seconds"));
			});

			return tcs.Task;
		}

		/// <summary>Completes a pending request with a response.</summary>
		public Boolean CompleteResponse(PipeMessage message, PipeMessage response)
		{
			if(this._pendingResponses.TryRemove(message.MessageId, out var tcs))
			{
				tcs.TrySetResult(response);
				return true;
			}

			Console.WriteLine($"No pending request found for response. Message={message.ToString()}");
			return false;
		}

		/// <summary>Fails a pending request with an error.</summary>
		public void FailResponse(PipeMessage message, Exception ex)
		{
			if(this._pendingResponses.TryRemove(message.MessageId, out var tcs))
				tcs.TrySetException(ex);
		}
	}
}