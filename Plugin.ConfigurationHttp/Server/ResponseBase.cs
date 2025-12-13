using System;
using System.Collections.Generic;
using System.IO;

namespace Plugin.ConfigurationHttp
{
	internal class ResponseBase
	{
		public Boolean IsMethodFound { get;private set; }
		public Byte[] Payload { get; set; }
		public String ContentType { get; set; }

		private static readonly Dictionary<String, String> PathContentType = new Dictionary<String, String>{
				{".html","text/html"},
				{".htm","text/html"},
				{".js","application/x-javascript"},
				{".css","text/css"}
			};

		public ResponseBase(Byte[] payload, String contentType)
		{
			this.IsMethodFound = true;
			this.Payload = payload;
			this.ContentType = contentType;
		}

		public ResponseBase(String resourceName, Stream payload)
			: this(resourceName, ResponseBase.ToByteArray(payload))
		{ }

		public ResponseBase(String resourceName, Byte[] payload)
			: this()
		{
			this.Payload = payload;
			this.ContentType = ResponseBase.DetectContentType(resourceName);
		}

		public ResponseBase()
			=> this.IsMethodFound = false;

		/// <summary>Determining Content-Type from File Extension</summary>
		/// <param name="resourceName">Name of the resource file</param>
		/// <returns>Content-Type</returns>
		private static String DetectContentType(String resourceName)
		{
			String extension = Path.GetExtension(resourceName);
			if(extension == null)
				return null;

			String result = PathContentType.TryGetValue(extension.ToLowerInvariant(), out result)
				? result
				: null;

			return result;
		}

		private static Byte[] ToByteArray(Stream stream)
		{
			stream.Position = 0;
			Byte[] buffer = new Byte[stream.Length];
			for(Int32 totalBytesCopied = 0; totalBytesCopied < stream.Length; )
				totalBytesCopied += stream.Read(buffer, totalBytesCopied, Convert.ToInt32(stream.Length) - totalBytesCopied);
			return buffer;
		}
	}
}