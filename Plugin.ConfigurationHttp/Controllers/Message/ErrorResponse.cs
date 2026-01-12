using System;
using System.Runtime.Serialization;

namespace Plugin.ConfigurationHttp.Controllers.Message
{
	[DataContract]
	public class ErrorResponse
	{
		[DataMember]
		public String Message { get; private set; }

		internal ErrorResponse(String message)
			=> this.Message = message;
	}
}