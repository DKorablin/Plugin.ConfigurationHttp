using System;
using System.Diagnostics;
using System.Net;
using System.ServiceModel;

namespace Plugin.ConfigurationHttp.Ipc.Control
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
	public class ControlService : IControlService
	{
		private readonly Int32 _processId = Process.GetCurrentProcess().Id;

		/// <summary>Подключить процесс к оповещению</summary>
		/// <param name="processId">Идентификатор подключаемого процесса</param>
		/// <param name="endpointAddress">IPC адрес процесса</param>
		public Int32 Connect(Int32 processId, String endpointAddress)
		{
			if(ServiceFactory.Proxies.ContainsKey(processId))
				throw new FaultException(String.Format("Connect -> ControlServiceProxy ({0:N0}) already registered", processId), new FaultCode(HttpStatusCode.BadRequest.ToString()));

			PluginsServiceProxy proxy = new PluginsServiceProxy(endpointAddress);
			ServiceFactory.Proxies.Add(processId, proxy);

			Plugin.Trace.TraceEvent(TraceEventType.Information, 5, "ControlHost ({0:N0}): ControlServiceProxy ({1:N0}) connected. Address: {2} Total: {3:N0}", this._processId, processId, endpointAddress, ServiceFactory.Proxies.Count);
			return this._processId;
		}

		/// <summary>Отключить процесс от оповещения</summary>
		/// <param name="processId">Идентификатор отключаемого процесса</param>
		public void Disconnect(Int32 processId)
		{
			if(!ServiceFactory.Proxies.Remove(processId))
				throw new FaultException(String.Format("Disconnect -> ControlServiceProxy ({0:N0}) not registered", processId), new FaultCode(HttpStatusCode.BadRequest.ToString()));

			Plugin.Trace.TraceEvent(TraceEventType.Information, 5, "ControlHost ({0:N0}): ControlServiceProxy ({1:N0)) disconnected. Total: {2:N0}", this._processId, processId, ServiceFactory.Proxies.Count);
			/*foreach(PluginsServiceProxy item in this.Proxies.Values)
				item.ClientMethod(String.Format("ProcessId: {0:N0} disconnected", processId));*/
		}

		public Int32 Ping(Int32 processId)
		{
			if(ServiceFactory.Proxies.ContainsKey(processId))
				return this._processId;
			else
				throw new FaultException(String.Format("Ping -> ControlServiceProxy ({0:N0}) not registered", processId), new FaultCode(HttpStatusCode.BadRequest.ToString()));
		}
	}
}