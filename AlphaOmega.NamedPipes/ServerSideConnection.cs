using System;
using System.IO.Pipes;
using System.Threading;

namespace AlphaOmega.NamedPipes
{
	public sealed class ServerSideConnection : IDisposable
	{
		public Guid ConnectionId { get; }

		public PipeStream Pipe { get; }

		internal SemaphoreSlim ReadWriteLock { get; } = new SemaphoreSlim(1, 1);

		public ServerSideConnection(PipeStream pipe)
			: this(Guid.NewGuid(), pipe)
		{
		}

		public ServerSideConnection(Guid connectionId, PipeStream pipe)
		{
			this.ConnectionId = connectionId;
			this.Pipe = pipe;
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);

			this.Pipe?.Dispose();
			this.ReadWriteLock.Dispose();
		}
	}
}