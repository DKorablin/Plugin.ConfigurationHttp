﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Script.Serialization;

namespace Plugin.ConfigurationHttp
{
	/// <summary>The serializer</summary>
	internal static class Serializer
	{
		/// <summary>Deserialize a string into an object</summary>
		/// <typeparam name="T">The type of the object to deserialize</typeparam>
		/// <param name="json">JSON string</param>
		/// <returns>Deserialized object</returns>
		public static Dictionary<String, Object> JavaScriptDeserialize(String json)
		{
			if(String.IsNullOrEmpty(json))
				return new Dictionary<String, Object>();

			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return (Dictionary<String, Object>)serializer.DeserializeObject(json);
		}

		/// <summary>Deserialize a string into an object</summary>
		/// <typeparam name="T">The type of an object</typeparam>
		/// <param name="json">JSON string</param>
		/// <returns>Deserialized object</returns>
		public static T JavaScriptDeserialize<T>(String json)
		{
			if(String.IsNullOrEmpty(json))
				return default;

			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.Deserialize<T>(json);
		}

		/// <summary>Deserialize a string into an object</summary>
		/// <typeparam name="T">The type of an object</typeparam>
		/// <param name="json">JSON string</param>
		/// <returns>Deserialized object</returns>
		public static Object JavaScriptDeserialize(Type type, String json)
		{
			if(String.IsNullOrEmpty(json))
				return null;

			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.GetType().InvokeMember("Deserialize", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new Object[] { serializer, json, type, serializer.RecursionLimit });
		}

		/// <summary>Serialize an object into JSON string</summary>
		/// <param name="item">Object to be serialized</param>
		/// <returns>JSON string</returns>
		public static String JavaScriptSerialize(Object item)
		{
			if(item == null)
				return null;

			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.Serialize(item);
		}
	}
}