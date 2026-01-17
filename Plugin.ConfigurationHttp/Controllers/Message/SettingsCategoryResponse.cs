using System;
using System.Linq;
using System.Reflection;

namespace Plugin.ConfigurationHttp.Controllers.Message
{
	public class SettingsCategoryResponse
	{
		public String Category { get; private set; }

		public SettingsResponse[] Items { get; private set; }

		internal SettingsCategoryResponse(String category, PropertyInfo[] properties, Object target)
		{
			this.Category = category;
			this.Items = properties.Select(p => new SettingsResponse(p, target)).ToArray();
		}
	}
}