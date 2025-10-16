using System;
#if NET8_0_OR_GREATER
// Stub for .NET 8 while CoreWCF client implementation is pending
namespace Plugin.ConfigurationHttp.Ipc
{
	public class PluginsServiceProxy
	{
		public IPluginsIpcService Plugins => null;
		public PluginsServiceProxy(String address) { }
	}
}
#else
using System.ServiceModel;

namespace Plugin.ConfigurationHttp.Ipc
{
	public class PluginsServiceProxy : ClientBase<IPluginsIpcService>
	{
		public IPluginsIpcService Plugins => base.Channel;

		public PluginsServiceProxy(String address)
			: base(new NetNamedPipeBinding(NetNamedPipeSecurityMode.None),
			new EndpointAddress(address))
		{ }
	}
}
#endif
