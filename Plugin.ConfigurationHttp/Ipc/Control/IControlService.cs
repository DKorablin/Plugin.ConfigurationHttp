using System;
#if NET8_0_OR_GREATER
using CoreWCF;
#else
using System.ServiceModel;
#endif

namespace Plugin.ConfigurationHttp.Ipc.Control
{
	/// <summary>WCF control service interface</summary>
	[ServiceContract]
	public interface IControlService
	{
		/// <summary>Attach a child process to the controlling process</summary>
		/// <param name="processId">Child process ID</param>
		/// <param name="endpointAddress">Client process address</param>
		/// <returns>Host process ID</returns>
		[OperationContract(IsOneWay = false)]
		Int32 Connect(Int32 processId, String endpointAddress);

		/// <summary>Detach a child process from the controlling process</summary>
		/// <param name="processId">Child process ID</param>
		[OperationContract(IsOneWay = true)]
		void Disconnect(Int32 processId);

		/// <summary>Checking the operation of the main host</summary>
		/// <param name="processId">Child process ID</param>
		/// <returns>Control process identifier</returns>
		[OperationContract(IsOneWay = false)]
		Int32 Ping(Int32 processId);
	}
}