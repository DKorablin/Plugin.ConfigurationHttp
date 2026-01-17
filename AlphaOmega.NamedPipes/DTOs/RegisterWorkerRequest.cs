using System;

namespace AlphaOmega.NamedPipes.DTOs
{
	public sealed class RegisterWorkerRequest
	{
		public String WorkerId { get; set; }

		public String PipeName { get; set; }

		public RegisterWorkerRequest(String workerId, String pipeName)
		{
			this.WorkerId = workerId;
			this.PipeName = pipeName;
		}
	}
}