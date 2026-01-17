using System;
using Plugin.ConfigurationHttp.Controllers.Message;
using SAL.Flatbed;

namespace Plugin.ConfigurationHttp.Ipc
{
	public class PluginsIpcService : IPluginsIpcService
	{
		private readonly PluginsController _controller;

		internal PluginsIpcService(IHost host, ServiceFactory factory)
		{
			this._controller= new PluginsController(host, factory);
		}

		public PluginResponse[] GetPlugins(String searchText)
			=> this._controller.GetPlugins(searchText);

		public String GetPluginParams(String pluginId)
		{
			Object result = this._controller.GetPluginParams(pluginId);
			return result == null || result is String
				? (String)result
				: Serializer.JavaScriptSerialize(result);
		}

		public String SetPluginParams(String pluginId, String paramName, String value)
		{
			Object result = this._controller.SetPluginParams(pluginId, paramName, value);
			return result == null || result is String
				? (String)result
				: Serializer.JavaScriptSerialize(result);
		}
	}
}