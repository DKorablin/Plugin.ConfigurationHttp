using System;
using System.ServiceModel;

namespace Plugin.ConfigurationHttp.Ipc.Control
{
	/// <summary>Интерфейс контрольного WCF сервиса</summary>
	[ServiceContract]
	public interface IControlService
	{
		/// <summary>Подключить дочерний процесс к контрольному процессу</summary>
		/// <param name="processId">Идентификатор дочернего процесса</param>
		/// <param name="endpointAddress">Адрес клиентского процесса</param>
		/// <returns>Идентификатор хост процесса</returns>
		[OperationContract(IsOneWay = false)]
		Int32 Connect(Int32 processId, String endpointAddress);

		/// <summary>Отключить дочерний процесс от контрольного процесса</summary>
		/// <param name="processId">Идентификатор дочернего процесса</param>
		[OperationContract(IsOneWay = true)]
		void Disconnect(Int32 processId);

		/// <summary>Проверка работы основного хоста</summary>
		/// <param name="processId">Идентификатор дочернего процесса</param>
		/// <returns>Идентификатор контрольного процесса</returns>
		[OperationContract(IsOneWay = false)]
		Int32 Ping(Int32 processId);
	}
}