using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Plugin.ConfigurationHttp.Controllers.Message
{
	[DataContract]
	public class SettingsCategoryResponse
	{
		[DataMember]
		public String Category { get; private set; }

		[DataMember]
		public SettingsResponse[] Items { get; private set; }

		internal SettingsCategoryResponse(String category, PropertyInfo[] properties, Object target)
		{
			this.Category = category;
			this.Items = properties.Select(p => new SettingsResponse(p, target)).ToArray();
		}
	}
}