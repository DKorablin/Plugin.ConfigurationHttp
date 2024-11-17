using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Script.Serialization;

namespace Plugin.ConfigurationHttp
{
	/// <summary>Сериализация</summary>
	internal static class Serializer
	{
		/// <summary>Десериализовать строку в объект</summary>
		/// <typeparam name="T">Тип объекта</typeparam>
		/// <param name="json">Строка в формате JSON</param>
		/// <returns>Десериализованный объект</returns>
		public static Dictionary<String, Object> JavaScriptDeserialize(String json)
		{
			if(String.IsNullOrEmpty(json))
				return new Dictionary<String, Object>();

			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return (Dictionary<String, Object>)serializer.DeserializeObject(json);
		}

		/// <summary>Десериализовать строку в объект</summary>
		/// <typeparam name="T">Тип объекта</typeparam>
		/// <param name="json">Строка в формате JSON</param>
		/// <returns>Десериализованный объект</returns>
		public static T JavaScriptDeserialize<T>(String json)
		{
			if(String.IsNullOrEmpty(json))
				return default;

			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.Deserialize<T>(json);
		}

		/// <summary>Десериализовать строку в объект</summary>
		/// <typeparam name="T">Тип объекта</typeparam>
		/// <param name="json">Строка в формате JSON</param>
		/// <returns>Десериализованный объект</returns>
		public static Object JavaScriptDeserialize(Type type, String json)
		{
			if(String.IsNullOrEmpty(json))
				return null;

			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.GetType().InvokeMember("Deserialize", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new Object[] { serializer, json, type, serializer.RecursionLimit });
		}

		/// <summary>Сериализовать объект</summary>
		/// <param name="item">Объект для сериализации</param>
		/// <returns>Строка в формате JSON</returns>
		public static String JavaScriptSerialize(Object item)
		{
			if(item == null)
				return null;

			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.Serialize(item);
		}
	}
}