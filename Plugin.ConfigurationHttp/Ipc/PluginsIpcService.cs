﻿using System;
using System.ServiceModel;
using Plugin.ConfigurationHttp.Controllers.Message;

namespace Plugin.ConfigurationHttp.Ipc
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
	public class PluginsIpcService : IPluginsIpcService
	{
		private readonly PluginsController _controller = new PluginsController(Plugin.SHost);

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