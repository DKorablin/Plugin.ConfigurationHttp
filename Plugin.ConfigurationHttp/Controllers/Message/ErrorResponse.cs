using System;

namespace Plugin.ConfigurationHttp.Controllers.Message
{
	public class ErrorResponse
	{
		public String Message { get; private set; }

		internal ErrorResponse(String message)
			=> this.Message = message;
	}
}