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

		public static T[] GetCustomAttributes<T>(this PropertyInfo item) where T : Attribute
		{
			Object[] items = item.GetCustomAttributes(typeof(T), false);
			T[] result = new T[items.Length];
			for(Int32 loop = 0; loop < items.Length; loop++)
				result[loop] = (T)items[loop];
			return result;
		}

		public static String GetCategory(this PropertyInfo item)
		{
			CategoryAttribute category = item.GetCustomAttribute<CategoryAttribute>();
			return category == null ? "Misc" : category.Category;
		}
	}
}