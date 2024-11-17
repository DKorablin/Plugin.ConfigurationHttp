using System;
using System.ComponentModel;
using System.Reflection;

namespace Plugin.ConfigurationHttp
{
	internal static class PropertyInfoExtension
	{
		public static T GetCustomAttribute<T>(this PropertyInfo item) where T : Attribute
		{
			Object[] items = item.GetCustomAttributes(typeof(T), false);
			return items.Length == 0 ? default : (T)items[0];
		}

		public static String GetCategory(this PropertyInfo item)
		{
			CategoryAttribute category = item.GetCustomAttribute<CategoryAttribute>();
			return category == null ? "Misc" : category.Category;
		}
	}
}