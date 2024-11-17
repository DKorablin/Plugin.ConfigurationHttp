using System;
using System.ServiceModel;
using Plugin.ConfigurationHttp.Controllers.Message;

namespace Plugin.ConfigurationHttp.Ipc
{
	/// <summary>Сервис для IPC взаимодействия</summary>
	[ServiceContract]
	public interface IPluginsIpcService
	{
		/// <summary>Получить список всех плагинов, которые загружены в текущий хост</summary>
		/// <param name="searchText">Поисковая строка</param>
		/// <returns>Список всех плагинов, загруженные в текущий хост</returns>
		[OperationContract(IsOneWay = false)]
		PluginResponse[] GetPlugins(String searchText);

		/// <summary>Получить информацию о плагине со всеми членами плагина</summary>
		/// <param name="pluginId">Идентификатор плагина</param>
		/// <returns>Информация о плагине в JSON формате</returns>
		[OperationContract(IsOneWay = false)]
		String GetPluginParams(String pluginId);

		/// <summary>Вызвать свойство или метод плагина, котоые не ожидают на вход аргументов</summary>
		/// <param name="id">Идентификатор плагина</param>
		/// <param name="memberName">Наименование свойства или метода, который не ожидает входящих параметров</param>
		/// <returns>Ответ от плагина в JSON формате</returns>
		[OperationContract(IsOneWay = false)]
		String SetPluginParams(String pluginId, String paramName, String value);
	}
}