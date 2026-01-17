using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AlphaOmega.NamedPipes.DTOs;

namespace AlphaOmega.NamedPipes
{
	/// <summary>
	/// Base class for pipe-based servers (Registry and Worker).
	/// Encapsulates common pipe communication logic: read, invoke handler, send response.
	/// </summary>
	public abstract class PipeServerBase
	{
		/// <summary>Sends a message on the pipe.</summary>
		protected async Task SendMessageAsync(ServerSideConnection connection, PipeMessage message, CancellationToken token)
		{
			await connection.ReadWriteLock.WaitAsync(token);
			try
			{
				await PipeProtocol.WriteMessageAsync(connection.Pipe, message, token);
			} finally
			{
				connection.ReadWriteLock.Release();
			}
		}

		/// <summary>Reads and sends messages in a loop until disconnection or cancellation.</summary>
		protected async Task ListenLoopAsync(ServerSideConnection connection, Func<PipeMessage, CancellationToken, Task<PipeMessage>> handler, CancellationToken token)
		{
			while(!token.IsCancellationRequested && connection.Pipe.IsConnected)
			{
				try
				{
					PipeMessage message = await PipeProtocol.ReadMessageAsync(connection.Pipe, token);

					PipeMessage response = await handler.Invoke(message, token);
					if(response != null)
						await this.SendMessageAsync(connection, response, token);
				} catch(EndOfStreamException)
				{
					Console.WriteLine("Lost connection to named pipe instance");
					break;
				} catch(ObjectDisposedException)
				{
					// Pipe was disposed
					throw;
				} catch(IOException ex)
				{
					// Pipe communication error
					Console.WriteLine($"Pipe communication error: {ex.Message}");
					throw;
				} catch(OperationCanceledException)
				{
					// Listening was cancelled
					throw;
				} catch(Exception ex)
				{
					Console.WriteLine($"Unexpected error in listen loop: {ex.Message}");
					break;
				}
			}
		}
	}
}