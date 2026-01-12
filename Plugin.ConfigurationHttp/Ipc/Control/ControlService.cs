using System;
using System.Diagnostics;
using System.Net;
using System.ServiceModel;
#if NET8_0_OR_GREATER
using CoreWCF;
using SMFaultException = System.ServiceModel.FaultException;
using SMFaultCode = System.ServiceModel.FaultCode;
#endif

namespace Plugin.ConfigurationHttp.Ipc.Control
{
#if NET8_0_OR_GREATER
	[CoreWCF.ServiceBehavior(InstanceContextMode = CoreWCF.InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
#else
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
#endif
	public class ControlService : IControlService
	{
		private readonly Int32 _processId = Process.GetCurrentProcess().Id;

		/// <summary>Connect a process to an alert</summary>
		/// <param name="processId">The process ID of the connecting process</param>
		/// <param name="endpointAddress">IPC process address</param>
		public Int32 Connect(Int32 processId, String endpointAddress)
		{
			if(ServiceFactory.Proxies.ContainsKey(processId))
#if NET8_0_OR_GREATER
				throw new SMFaultException($"Connect -> ControlServiceProxy ({processId:N0}) already registered");
#else
				throw new FaultException($"Connect -> ControlServiceProxy ({processId:N0}) already registered", new FaultCode(HttpStatusCode.BadRequest.ToString()));
#endif

			PluginsServiceProxy proxy = new PluginsServiceProxy(endpointAddress);
			ServiceFactory.Proxies.Add(processId, proxy);

			Plugin.Trace.TraceEvent(TraceEventType.Information, 5, "ControlHost ({0:N0}): ControlServiceProxy ({1:N0}) connected. Address: {2} Total: {3:N0}", this._processId, processId, endpointAddress, ServiceFactory.Proxies.Count);
			return this._processId;
		}

		/// <summary>Disconnect a process to an alert</summary>
		/// <param name="processId">The process ID of the disconnecting process</param>
		public void Disconnect(Int32 processId)
		{
			if(!ServiceFactory.Proxies.Remove(processId))
#if NET8_0_OR_GREATER
				throw new SMFaultException($"Disconnect -> ControlServiceProxy ({processId:N0}) not registered");
#else
				throw new FaultException($"Disconnect -> ControlServiceProxy ({processId:N0}) not registered", new FaultCode(HttpStatusCode.BadRequest.ToString()));
#endif

			Plugin.Trace.TraceEvent(TraceEventType.Information, 5, "ControlHost ({0:N0}): ControlServiceProxy ({1:N0)) disconnected. Total: {2:N0}", this._processId, processId, ServiceFactory.Proxies.Count);
			/*foreach(PluginsServiceProxy item in this.Proxies.Values)
				item.ClientMethod(String.Format("ProcessId: {0:N0} disconnected", processId));*/
		}

		public Int32 Ping(Int32 processId)
			=> ServiceFactory.Proxies.ContainsKey(processId)
				? this._processId
#if NET8_0_OR_GREATER
				: throw new SMFaultException($"Ping -> ControlServiceProxy ({processId:N0}) not registered");
#else
				: throw new FaultException($"Ping -> ControlServiceProxy ({processId:N0}) not registered", new FaultCode(HttpStatusCode.BadRequest.ToString()));
#endif
	}
}