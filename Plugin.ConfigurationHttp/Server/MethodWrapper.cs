using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Web;

namespace Plugin.ConfigurationHttp
{
	/// <summary>Обёртка метода контроллера</summary>
	internal class MethodWrapper
	{
		/// <summary>Рефлексия метода</summary>
		private readonly MethodInfo _method;
		/// <summary>Контроллер этого метода</summary>
		private readonly Object _controller;

		private static readonly String[] HttpMethods = new String[] { "GET", "HEAD", "POST", "PUT", "DELETE", "CONNECT", "OPTIONS", "TRACE", };

		/// <summary>HTTP метод под которым разрешён этот метод</summary>
		public String HttpMethod { get; }

		/// <summary>Внешняя ссылка на метод</summary>
		public String Id { get; }

		/// <summary>Создать обёртку для метода контроллера</summary>
		/// <param name="method">Рефлексия метода</param>
		/// <param name="controller">Контроллер, где есть этот метод</param>
		public MethodWrapper(Object controller, MethodInfo method)
		{
			String methodName = method.Name;
			foreach(String item in MethodWrapper.HttpMethods)
				if(method.Name.StartsWith(item, StringComparison.InvariantCultureIgnoreCase))
				{
					methodName = method.Name.Substring(item.Length);
					this.HttpMethod = item;
					break;
				}
			this._method = method;
			this._controller = controller;

			String ns = this.GetType().Namespace;
			String ctrlNamespace = controller.GetType().Namespace;
			if(ctrlNamespace.StartsWith(ns))
				ctrlNamespace = ctrlNamespace.Remove(0, ns.Length);

			String[] args = Array.ConvertAll(method.GetParameters(), delegate(ParameterInfo prm) { return prm.Name; });

			this.Id = ctrlNamespace + "." + methodName + "?" + String.Join(",", args);
		}

		/// <summary>Выполнить метод из HTTP(s) параметров</summary>
		/// <param name="keyValue">Кюч/значение</param>
		/// <returns>Результат выполнения метода</returns>
		public ResponseBase Invoke(NameValueCollection keyValue)
		{
			Object[] args = this.ConvertMethodParams(keyValue);

			Object result = this._method.Invoke(this._controller, args);
			if(this._method.ReturnType == typeof(void))
				return new ResponseBase(new Byte[] { }, null);
			else if(result == null)
				return null;
			else if(result is String s)
				return new ResponseBase(Encoding.UTF8.GetBytes(s), "text/plain");
			else
			{
				String strResult = Serializer.JavaScriptSerialize(result);
				return new ResponseBase(Encoding.UTF8.GetBytes(strResult), "application/json");
			}
		}

		/// <summary>Преобразовать HTTP(s) параметры в аргументы методов</summary>
		/// <param name="keyValue">Ключ/значение</param>
		/// <returns>Массив аргументов методов</returns>
		private Object[] ConvertMethodParams(NameValueCollection keyValue)
		{
			ParameterInfo[] parameters = this._method.GetParameters();
			if(parameters.Length == 0)
				return new Object[] { };

			Object[] result = new Object[parameters.Length];
			for(Int32 loop = 0; loop < parameters.Length; loop++)
			{
				ParameterInfo parameter = parameters[loop];
				String value = HttpUtility.UrlDecode(keyValue.Get(parameter.Name));

				result[loop] = parameter.ParameterType == typeof(System.String)
					? value
					: TypeDescriptor.GetConverter(parameter.ParameterType).ConvertFromString(value);
			}

			return result;
		}
	}
}