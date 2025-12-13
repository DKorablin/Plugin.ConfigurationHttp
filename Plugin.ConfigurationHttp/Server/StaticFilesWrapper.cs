using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Plugin.ConfigurationHttp
{
	internal class StaticFilesWrapper
	{
		private const String ResourceDefaultFile = ".Index";
		private const String ResourceFolder = "Files";
		private readonly Dictionary<String, ResponseBase> _resources = new Dictionary<String, ResponseBase>();

		public ResponseBase Get(String path)
		{
			if(path == null || path.EndsWith("."))
				path = StaticFilesWrapper.ResourceDefaultFile;

			if(!this._resources.TryGetValue(path, out ResponseBase result))
			{
				result = StaticFilesWrapper.GetEmbeddedResource(StaticFilesWrapper.ResourceFolder + path);
				if(result != null)
					this._resources.Add(path, result);
			}

			return result;
		}

		private static ResponseBase GetEmbeddedResource(String resourceName)
		{
			Assembly asm = Assembly.GetExecutingAssembly();
			resourceName = StaticFilesWrapper.FormatResourceName(asm, resourceName);
			String[] resourceNames = asm.GetManifestResourceNames();

			foreach(String key in resourceNames)
				if(key.StartsWith(resourceName))
					using(Stream stream = asm.GetManifestResourceStream(key))
						return stream == null
							? null
							: new ResponseBase(key, stream);

			return null;
		}

		internal static String FormatResourceName(String resourceName)
			=> resourceName.Replace(" ", "_").Replace("\\", ".").Replace("/", ".");

		private static String FormatResourceName(Assembly assembly, String resourceName)
			=> assembly.GetName().Name + "." + FormatResourceName(resourceName);
	}
}