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
		/// <summary>Checking for a fatal exception after which further code execution is impossible</summary>
		/// <param name="exception">The exception for verification</param>
		/// <returns>The exception is fatal</returns>
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
				if(result.Length <= loop)//Increase the array by one if the value does not fit.
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
		/// <summary>Get a list of search parameters in the plugin</summary>
		/// <param name="plugin">A plugin instance for returning search strings</param>
		/// <returns>Search strings found in the plugin</returns>
		public static IEnumerable<String> GetPluginSearchMembers(IPluginDescription plugin)
		{
			foreach(String value in SearchProperties(plugin, false))
				yield return value;

			if(plugin.Instance is IPluginSettings settings)
				foreach(String value in SearchProperties(settings.Settings, true))
					yield return value;
		}

		/// <summary>Search by properties of an object instance</summary>
		/// <param name="instance">The object whose properties to search by</param>
		/// <param name="searchAttributes">Search by attributes of each property</param>
		/// <returns>Search strings found in object instance properties</returns>
		private static IEnumerable<String> SearchProperties(Object instance, Boolean searchAttributes)
		{
			PropertyInfo[] properties = instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy);
			foreach(PropertyInfo property in properties)
			{
				yield return property.Name;

				if(searchAttributes)
				{//We search by all attributes so as not to hardcode specific attributes
					Object[] attributes = property.GetCustomAttributes(false);
					if(attributes != null)
						foreach(Object attribute in attributes)
							foreach(String value in Utils.SearchProperties(attribute, false))
								yield return value;
				}

				if(property.CanRead
					&& property.GetIndexParameters().Length == 0
					&& Array.Exists<Type>(property.PropertyType.GetInterfaces(), p => p == typeof(IComparable)))
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