using System;
using System.Threading;
using System.Threading.Tasks;
using AlphaOmega.NamedPipes.DTOs;
using AlphaOmega.NamedPipes.Interfaces;

namespace AlphaOmega.NamedPipes.Reflection
{
	public class RemoteProcessingWorkerInvoker : RemoteProcessingLogicInvoker
	{
		private String _workerId;

		public RemoteProcessingWorkerInvoker() { }

		public RemoteProcessingWorkerInvoker(Type interfaceType)
			: base(interfaceType)
		{
		}

		public void Initialize<T>(IRegisterServer registerServer, String workerId, CancellationToken cancellationToken) where T : class
		{
			if(String.IsNullOrWhiteSpace(workerId))
				throw new ArgumentNullException(nameof(workerId));

			this._workerId = workerId;
			this.Initialize<T>(registerServer, cancellationToken);
		}

		protected override async Task<Object> SendRequestAndGetResponseAsync(PipeMessage request, Type responseType)
		{
			if(String.IsNullOrWhiteSpace(this._workerId))
				throw new InvalidOperationException("Proxy not properly initialized");

			PipeMessage response = await this.RegisterServer.SendRequestToWorker(this._workerId, request, this.CancellationToken);
			if(response.Type == PipeMessageType.Error.ToString())
			{
				var error = response.Deserialize<ErrorResponse>();
				throw new InvalidOperationException(error.Message);
			}

			Object result = response.Deserialize(responseType);
			return result;
		}
	}
}