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
		private readonly ServiceFactory _serviceFactory;

		public PluginsController(IHost host, ServiceFactory serviceFactory)
		{
			this._host = host;
			this._serviceFactory = serviceFactory;
		}

		public String[] GetInstance()
			=> this._serviceFactory.Proxies.ToArray();

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
				Object objValue = TypeDescriptor.GetConverter(prop.PropertyType).ConvertFromInvariantString(Uri.UnescapeDataString(value));
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
			IPluginsIpcService proxy = this.GetProxy(instanceId);
			if(proxy == null)
				return new ErrorResponse("Instance not found");
			else
				return proxy.GetPlugins(searchText);
		}

		public Object GetPluginParams(String instanceId, String pluginId)
		{
			IPluginsIpcService proxy = this.GetProxy(instanceId);
			if(proxy == null)
				return new ErrorResponse("Instance not found");
			else
				return proxy.GetPluginParams(pluginId);
		}

		public Object SetPluginParams(String instanceId, String pluginId, String paramName, String value)
		{
			IPluginsIpcService proxy = this.GetProxy(instanceId);
			if(proxy == null)
				return new ErrorResponse("Instance not found");
			else
				return proxy.SetPluginParams(pluginId, paramName, Uri.UnescapeDataString(value));
		}

		private IPluginsIpcService GetProxy(String instance)
			=> this._serviceFactory.GetWorkerInstance(instance);
		#endregion Ipc
	}
}