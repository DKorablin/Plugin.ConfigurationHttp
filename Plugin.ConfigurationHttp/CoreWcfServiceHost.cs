#if !NETFRAMEWORK
using System;
using System.Threading;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommunicationState = System.ServiceModel.CommunicationState;

namespace Plugin.ConfigurationHttp
{
	/// <summary>
	/// Wrapper for CoreWCF service hosting in .NET 8
	/// Provides a similar interface to System.ServiceModel.ServiceHost
	/// </summary>
	internal class CoreWcfServiceHost : IDisposable
	{
		private IHost _host;
		private readonly Type _serviceType;
		private readonly Uri _baseAddress;
		private CommunicationState _state = CommunicationState.Created;
		private readonly Object _syncLock = new Object();

		public event EventHandler Faulted;

		public CommunicationState State
		{
			get { lock(_syncLock) return _state; }
			private set { lock(_syncLock) _state = value; }
		}

		public ServiceDescription Description { get; private set; }

		public CoreWcfServiceHost(Type serviceType, params Uri[] baseAddresses)
		{
			this._serviceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
			this._baseAddress = baseAddresses?.Length > 0 ? baseAddresses[0] : null;

			// Create a minimal ServiceDescription for compatibility
			this.Description = new ServiceDescription();
		}

		public CoreWcfServiceHost(Object singletonInstance, params Uri[] baseAddresses)
		{
			_ = singletonInstance ?? throw new ArgumentNullException(nameof(singletonInstance));

			this._serviceType = singletonInstance.GetType();
			this._baseAddress = baseAddresses?.Length > 0 ? baseAddresses[0] : null;

			this.Description = new ServiceDescription();
		}

		public ServiceEndpoint AddServiceEndpoint(Type implementedContract, CoreWCF.Channels.Binding binding, String address)
		{
			// Store endpoint information for later use
			var endpoint = new ServiceEndpoint(new ContractDescription(implementedContract.Name));
			endpoint.Address = new EndpointAddress(new Uri(_baseAddress, address));
			endpoint.Binding = binding;

			this.Description.Endpoints.Add(endpoint);
			return endpoint;
		}

		public void Open()
		{
			if(this.State != CommunicationState.Created && this.State != CommunicationState.Closed)
				throw new InvalidOperationException($"Cannot open service in state: {this.State}");

			this.State = CommunicationState.Opening;

			try
			{
				// For named pipe hosting, we use a minimal host
				// CoreWCF requires an IHost but we'll create a lightweight one
				var hostBuilder = Host.CreateDefaultBuilder()
					.ConfigureServices((context, services) =>
					{
						services.AddServiceModelServices();

						// Add the service
						if(_serviceType != null)
						{
							services.AddSingleton(_serviceType);
						}
					})
					.ConfigureServices(services =>
					{
						// Configure CoreWCF
						services.AddServiceModelServices();
					});

				_host = hostBuilder.Build();
				_host.StartAsync().Wait();

				this.State = CommunicationState.Opened;
			} catch(Exception)
			{
				this.State = CommunicationState.Faulted;
				Faulted?.Invoke(this, EventArgs.Empty);
				throw;
			}
		}

		public void Close()
			=> this.Close(TimeSpan.FromSeconds(10));

		public void Close(TimeSpan timeout)
		{
			if(this.State == CommunicationState.Closed || this.State == CommunicationState.Closing)
				return;

			this.State = CommunicationState.Closing;

			try
			{
				if(_host != null)
				{
					using(var cts = new CancellationTokenSource(timeout))
						_host.StopAsync(cts.Token).Wait();
				}
				this.State = CommunicationState.Closed;
			} catch
			{
				this.State = CommunicationState.Faulted;
				throw;
			}
		}

		public void Abort()
		{
			this.State = CommunicationState.Closing;
			try
			{
				this._host?.StopAsync(TimeSpan.FromSeconds(1)).Wait();
			} catch
			{
				// Ignore exceptions during abort
			} finally
			{
				this.State = CommunicationState.Closed;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(Boolean disposing)
		{
			if(disposing)
			{
				if(this.State == CommunicationState.Opened)
				{
					try
					{
						this.Close();
					} catch
					{
						this.Abort();
					}
				}

				this._host?.Dispose();
				this._host = null;
			}
		}
	}
}
#endif