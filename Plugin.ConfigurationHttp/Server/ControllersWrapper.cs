using System;
using System.Collections.Generic;
using System.Reflection;

namespace Plugin.ConfigurationHttp
{
	internal class ControllersWrapper
	{
		private readonly Dictionary<String, MethodWrapper> _controllers = new Dictionary<String, MethodWrapper>();

		public MethodWrapper Get(String httpMethod, String localPath, String[] args)
		{
			String key = localPath.TrimEnd('.') + "?" + String.Join(",", args);
			return this._controllers.TryGetValue(key, out MethodWrapper result)
				? result
				: null;
		}

		public ControllersWrapper(params Object[] controllers)
		{
			foreach(Object ctrl in controllers)
			{
				MethodInfo[] methods = ctrl.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
				foreach(MethodInfo method in methods)
				{
					MethodWrapper wrapper = new MethodWrapper(ctrl, method);
					this._controllers.Add(wrapper.Id, wrapper);
				}
			}
		}
	}
}