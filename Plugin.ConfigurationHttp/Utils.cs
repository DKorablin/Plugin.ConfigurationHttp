using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using SAL.Flatbed;

namespace Plugin.ConfigurationHttp
{
	internal static class Utils
	{
		/// <summary>Проверка исключения на фатальное, после которого дальнейшее выполнение кода невозможно</summary>
		/// <param name="exception">Исключение для проверки</param>
		/// <returns>Исключение фатальное</returns>
		public static Boolean IsFatal(Exception exception)
		{
			while(exception != null)
			{
				if((exception is OutOfMemoryException && !(exception is InsufficientMemoryException)) || exception is ThreadAbortException || exception is AccessViolationException || exception is SEHException)
					return true;
				if(!(exception is TypeInitializationException) && !(exception is TargetInvocationException))
					break;
				exception = exception.InnerException;
			}
			return false;
		}

		public static UInt32[] BitToInt(params Boolean[] bits)
		{
			UInt32[] result = new UInt32[] { };
			Int32 counter = 0;
			for(Int32 loop = 0; loop < bits.Length; loop++)
			{
				if(result.Length <= loop)//Увеличиваю массив на один, если не помещается значение
					Array.Resize<UInt32>(ref result, result.Length + 1);

				for(Int32 innerLoop = 0; innerLoop < 32; innerLoop++)
				{
					result[loop] |= Convert.ToUInt32(bits[counter++]) << innerLoop;
					if(counter >= bits.Length)
						break;
				}
				if(counter >= bits.Length)
					break;
			}
			return result;
		}

		#region Search
		/// <summary>Получить список поисковых параметров в плагине</summary>
		/// <param name="plugin">Экземпляр плагин для возврата поисковых строк</param>
		/// <returns>Найденные поисковые строки в плагине</returns>
		public static IEnumerable<String> GetPluginSearchMembers(IPluginDescription plugin)
		{
			foreach(String value in SearchProperties(plugin, false))
				yield return value;

			if(plugin.Instance is IPluginSettings settings)
				foreach(String value in SearchProperties(settings.Settings, true))
					yield return value;
		}

		/// <summary>Поиск по свойствам экземпляра объекта</summary>
		/// <param name="instance">Объект, по свойствам которого поискать</param>
		/// <param name="searchAttributes">Поиск по атрибутам каждого свойства</param>
		/// <returns>Найденные поисковые строки в свойствах экземпляра объекта</returns>
		private static IEnumerable<String> SearchProperties(Object instance, Boolean searchAttributes)
		{
			PropertyInfo[] properties = instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy);
			foreach(PropertyInfo property in properties)
			{
				yield return property.Name;

				if(searchAttributes)
				{//Ищем по всем атрибутам, дабы не хардкодить конкретные атрибуты
					Object[] attributes = property.GetCustomAttributes(false);
					if(attributes != null)
						foreach(Object attribute in attributes)
							foreach(String value in Utils.SearchProperties(attribute, false))
								yield return value;
				}

				if(property.CanRead
					&& property.GetIndexParameters().Length == 0
					&& Array.Exists<Type>(property.PropertyType.GetInterfaces(), p => { return p == typeof(IComparable); }))
				{
					Object value = property.GetValue(instance, null);
					if(value != null)
						yield return value.ToString();
				}
			}
		}
		#endregion Search
	}
}