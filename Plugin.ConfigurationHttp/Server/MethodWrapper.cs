using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Web;

namespace Plugin.ConfigurationHttp
{
	/// <summary>The controller method wrapper</summary>
	internal class MethodWrapper
	{
		/// <summary>The method reflection information.</summary>
		private readonly MethodInfo _method;
		/// <summary>The instance of the controller.</summary>
		private readonly Object _controller;

		private static readonly String[] HttpMethods = new String[] { "GET", "HEAD", "POST", "PUT", "DELETE", "CONNECT", "OPTIONS", "TRACE", };

		/// <summary>The HTTP(s) method which is allowed to use with this method.</summary>
		public String HttpMethod { get; }

		/// <summary>The external url to this method.</summary>
		public String Id { get; }

		/// <summary>Create a wrapper for the controller method</summary>
		/// <param name="method">The method reflection.</param>
		/// <param name="controller">The controller information where this method declared.</param>
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

		/// <summary>Invoke current method using HTTP(s) request with arguments.</summary>
		/// <param name="keyValue">The list of Key/values that should used as method arguments.</param>
		/// <returns>The result of method invocation.</returns>
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

		/// <summary>Convert HTTP(s) parameters to method arguments.</summary>
		/// <param name="keyValue">The input arguments information in Key/Value format.</param>
		/// <returns>Converted arguments that could be used to invoke .NET method.</returns>
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