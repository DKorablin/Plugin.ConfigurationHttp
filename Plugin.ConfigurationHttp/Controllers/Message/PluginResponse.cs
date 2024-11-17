using System;
using System.Runtime.Serialization;
using SAL.Flatbed;

namespace Plugin.ConfigurationHttp.Controllers.Message
{
	[DataContract]
	public class PluginResponse
	{
		/// <summary>Уникальный идентификатор плагина.</summary>
		/// <remarks>Пока Name является уникальным идентификатором плагина</remarks>
		[DataMember]
		public String ID { get; private set; }

		/// <summary>Наименование планина</summary>
		[DataMember]
		public String Name { get; private set; }

		/// <summary>Источник получения плагина</summary>
		[DataMember]
		public String Source { get; private set; }

		/// <summary>Версия плагина</summary>
		[DataMember]
		public Version Version { get; private set; }

		/// <summary>Описание сборки</summary>
		[DataMember]
		public String Description { get; private set; }

		/// <summary>Создатель сборки</summary>
		[DataMember]
		public String Company { get; private set; }

		/// <summary>Копирайт плагина</summary>
		[DataMember]
		public String Copyright { get; private set; }

		internal PluginResponse(IPluginDescription plugin)
		{
			this.Company = plugin.Company;
			this.Copyright = plugin.Copyright;
			this.Description = plugin.Description;
			this.ID = plugin.ID;
			this.Name = plugin.Name;
			this.Source = plugin.Source;
			this.Version = plugin.Version;
		}
	}
}