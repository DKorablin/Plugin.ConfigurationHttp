using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Plugin.ConfigurationHttp.Controllers.Message;
using Plugin.ConfigurationHttp.Ipc;
using SAL.Flatbed;

namespace Plugin.ConfigurationHttp
{
	internal class PluginsController
	{
		private readonly IHost _host;
		public PluginsController(IHost host)
			=> this._host = host;

		public Int32[] GetInstance()
			=> ServiceFactory.Proxies.Keys.ToArray();

		public PluginResponse[] GetPlugins(String searchText)
		{
			List<PluginResponse> result = new List<PluginResponse>();

			foreach(IPluginDescription plugin in this._host.Plugins.OrderBy(p => p.Name))
			{
				if(String.IsNullOrEmpty(searchText))
					result.Add(new PluginResponse(plugin));
				else
					foreach(String str in Utils.GetPluginSearchMembers(plugin))
						if(str.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) > -1)
						{
							result.Add(new PluginResponse(plugin));
							break;
						}
			}
			return result.ToArray();
		}

		public Object GetPluginParams(String pluginId)
		{
			IPluginDescription plugin = this._host.Plugins[pluginId];
			if(plugin == null)
				return new ErrorResponse("Plugin not found");

			if(!(plugin.Instance is IPluginSettings settings) || settings.Settings == null)
				return String.Empty;

			Object objSettings = settings.Settings;
			PropertyInfo[] properties = objSettings.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
			SettingsCategoryResponse[] result = properties.GroupBy(p => p.GetCategory()).Select(p => new SettingsCategoryResponse(p.Key, p.ToArray(), objSettings)).ToArray();
			return result;
		}

		public Object SetPluginParams(String pluginId, String paramName, String value)
		{
			IPluginDescription plugin = this._host.Plugins[pluginId];
			if(plugin == null)
				return new ErrorResponse("Plugin not found");

			if(!(plugin.Instance is IPluginSettings settings) || settings.Settings == null)
				return new ErrorResponse("Settings for plugin not found");

			PropertyInfo prop = settings.Settings.GetType().GetProperty(paramName, BindingFlags.Instance | BindingFlags.Public);

			try
			{
				Object objValue = TypeDescriptor.GetConverter(prop.PropertyType).ConvertFromInvariantString(value);
				prop.SetValue(settings.Settings, objValue, null);
				objValue = prop.GetValue(settings.Settings, null);//I get the value back because the property may not change.
				this._host.Plugins.Settings(plugin.Instance).SaveAssemblyParameter(prop.Name, objValue);
				return String.Empty;
			} catch(TargetInvocationException exc)
			{
				ErrorResponse result = new ErrorResponse(exc.InnerException.Message);
				return Serializer.JavaScriptSerialize(result);
			}
			catch(Exception exc)
			{
				ErrorResponse result = new ErrorResponse(exc.Message);
				return Serializer.JavaScriptSerialize(result);
			}
		}

		#region Ipc
		public Object GetPlugins(String searchText, String instanceId)
		{
			PluginsServiceProxy proxy = this.GetProxy(instanceId);
			if(proxy == null)
				return new ErrorResponse("Instance not found");
			else
				return proxy.Plugins.GetPlugins(searchText);
		}

		public Object GetPluginParams(String instanceId, String pluginId)
		{
			PluginsServiceProxy proxy = this.GetProxy(instanceId);
			if(proxy == null)
				return new ErrorResponse("Instance not found");
			else
				return proxy.Plugins.GetPluginParams(pluginId);
		}

		public Object SetPluginParams(String instanceId, String pluginId, String paramName, String value)
		{
			PluginsServiceProxy proxy = this.GetProxy(instanceId);
			if(proxy == null)
				return new ErrorResponse("Instance not found");
			else
				return proxy.Plugins.SetPluginParams(pluginId, paramName, value);
		}

		private PluginsServiceProxy GetProxy(String instance)
			=> Int32.TryParse(instance, out Int32 instanceId)
				&& ServiceFactory.Proxies.TryGetValue(instanceId, out PluginsServiceProxy result)
				? result : null;
		#endregion Ipc
	}
}