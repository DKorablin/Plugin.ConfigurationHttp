using System;

namespace AlphaOmega.NamedPipes
{
	public sealed class ServerSideWorker
	{
		public String WorkerId { get; }

		public String WorkerPipeName { get; }

		public Guid ConnectionId { get; }

		public ServerSideWorker(String workerId, String workerPipeName, Guid connectionId)
		{
			this.WorkerId = workerId;
			this.WorkerPipeName = workerPipeName;
			this.ConnectionId = connectionId;
		}
	}
}