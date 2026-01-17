using System;
using System.Threading;
using System.Threading.Tasks;
using AlphaOmega.NamedPipes.DTOs;

namespace AlphaOmega.NamedPipes.Interfaces
{
	/// <summary>Base contract for a named pipe server.</summary>
	public interface IServerBase : IDisposable
	{
		/// <summary>Pipe name for server communication.</summary>
		String PipeName { get; }

		/// <summary>Indicates if the server is started.</summary>
		Boolean IsStarted { get; }

		/// <summary>Raised when a client request arrives.</summary>
		event Func<PipeMessage, CancellationToken, Task<PipeMessage>> RequestReceived;

		/// <summary>Starts the server asynchronously.</summary>
		/// <param name="token">Cancellation token.</param>
		Task StartAsync(CancellationToken token);

		/// <summary>Stops the server asynchronously.</summary>
		Task StopAsync();
	}
}