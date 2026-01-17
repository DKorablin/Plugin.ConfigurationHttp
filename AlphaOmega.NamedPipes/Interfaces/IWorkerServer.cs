using System;
using System.Threading.Tasks;

namespace AlphaOmega.NamedPipes.Interfaces
{
	/// <summary>Worker server contract for connecting to a registry and handling named pipe communication.</summary>
	public interface IWorkerServer: IServerBase
	{
		/// <summary>Registry server pipe name this worker connects to.</summary>
		String RegistryPipeName { get; }

		/// <summary>Unique identifier of the worker.</summary>
		String WorkerId { get; }

		/// <summary>Raised when the connection to the registry server is lost.</summary>
		event Func<Task> ConnectionLost;
	}
}