using System;
using System.ServiceModel;
using Plugin.ConfigurationHttp.Controllers.Message;

namespace Plugin.ConfigurationHttp.Ipc
{
	/// <summary>Service for IPC interaction</summary>
	[ServiceContract]
	public interface IPluginsIpcService
	{
		/// <summary>Get a list of all plugins loaded on the current host.</summary>
		/// <param name="searchText">The test to find.</param>
		/// <returns>List of all plugins loaded in the current host that match the search criteria.</returns>
		[OperationContract(IsOneWay = false)]
		PluginResponse[] GetPlugins(String searchText);

		/// <summary>Get plugin information with all plugin members</summary>
		/// <param name="pluginId">The plugin identifier.</param>
		/// <returns>Plugin information in JSON format</returns>
		[OperationContract(IsOneWay = false)]
		String GetPluginParams(String pluginId);

		/// <summary>Call a plugin property or method that does not expect any arguments.</summary>
		/// <param name="id">The plugin identifier.</param>
		/// <param name="memberName">The name of a property or method that does not expect input parameters.</param>
		/// <returns>Response from the plugin in JSON format.</returns>
		[OperationContract(IsOneWay = false)]
		String SetPluginParams(String pluginId, String paramName, String value);
	}
}