using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using AlphaOmega.IO;
using AlphaOmega.IO.Interfaces;
using Plugin.ConfigurationHttp.Ipc;

namespace Plugin.ConfigurationHttp
{
	internal class ServiceFactory : IDisposable
	{
		private readonly Plugin _plugin;
		private String _hostUrl;
		private HttpServerFacade _controlWebHost;
		private IRegistryServer _registerServer;
		private IWorkerServer _workerServer;
		private String IpcRegistryPipeName => "Plugin.ConfigurationHttp.Registry." + Utils.GetDeterministicHashCode(this._hostUrl);
		private static String IpcWorkerPipeName => "Plugin.ConfigurationHttp.Worker.";
		private static String IpcWorkerPipeId => Process.GetCurrentProcess().Id.ToString();
		private String MutexName => "Global\\Plugin.ConfigurationHttp." + Utils.GetDeterministicHashCode(this._hostUrl);

		public Boolean IsHost => this._registerServer != null;
		public event EventHandler<EventArgs> Connected;

		public IEnumerable<String> Proxies
		{
			get => this._registerServer.ConnectedWorkerIDs;
		}

		public ServiceFactory(Plugin plugin)
			=> this._plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));

		/// <summary>Get a list of addresses under which hosts are running</summary>
		/// <returns></returns>
		public IEnumerable<String> GetHostEndpoints()
		{
			if(this._controlWebHost != null)
				foreach(String addr in this._controlWebHost.Endpoints)
					yield return addr;
			if(this._registerServer != null)
			{
				yield return this._registerServer.PipeName;

				foreach(String workerId in this._registerServer.ConnectedWorkerIDs)
					yield return workerId;
			}
			if(this._workerServer != null)
				yield return this._workerServer.PipeName;
		}

		public void Connect(String hostUrl, CancellationToken token)
		{
			if(String.IsNullOrEmpty(hostUrl))
				throw new ArgumentNullException(nameof(hostUrl));

			this._hostUrl = hostUrl;
			IpcSingleton ipc = new IpcSingleton(this.MutexName, new TimeSpan(0, 0, 10));

			// Fire and forget to avoid blocking the caller
			_ = Task.Run(async () =>
			{
				await ipc.MutexAsync<Object>(null, async p =>
				{
					try
					{
						if(this.TryCreateWebHost())
						{
							this._registerServer = new RegistryServer(this.IpcRegistryPipeName);
							this._registerServer.WorkerConnected += (workerId) =>
							{
								Plugin.Trace.TraceEvent(TraceEventType.Information, 0, "Worker server {0} connected to registry.", workerId);
								return System.Threading.Tasks.Task.CompletedTask;
							};
							this._registerServer.WorkerDisconnected += (workerId) =>
							{
								Plugin.Trace.TraceEvent(System.Diagnostics.TraceEventType.Warning, 0, "Worker server {0} disconnected from registry.", workerId);
								return System.Threading.Tasks.Task.CompletedTask;
							};

							this._registerServer.StartAsync(token);
						} else
						{
							var ipcPlugins = new PluginsIpcService(this._plugin.Host, this);
							this._workerServer = new WorkerServer(this.IpcRegistryPipeName, IpcWorkerPipeName, IpcWorkerPipeId, ipcPlugins);

							// Subscribe to connection loss before starting
							this._workerServer.ConnectionLost += () => this.OnWorkerServerConnectionLostAsync(token);

							await this._workerServer.StartAsync(token);
						}

						this.Connected?.Invoke(this, EventArgs.Empty);
					} catch
					{
						this.Dispose();
						throw;
					}
				});
			}, token);
		}

		private async Task OnWorkerServerConnectionLostAsync(CancellationToken token)
		{
			if(token.IsCancellationRequested)
				return;

			Plugin.Trace.TraceEvent(TraceEventType.Warning, 0, "Worker server lost connection to registry. Attempting to restart IPC connection...");

			try
			{
				if(this._workerServer != null)
				{
					await this._workerServer?.StopAsync();
					this._workerServer?.Dispose();
					this._workerServer = null;
				}

				// Wait briefly before attempting reconnection to avoid tight loops
				await Task.Delay(TimeSpan.FromSeconds(2), token);

				this.Connect(this._hostUrl, token);

				Plugin.Trace.TraceEvent(TraceEventType.Information, 0, "IPC server reconnected succesfully.");
			} catch(OperationCanceledException)
			{
				Plugin.Trace.TraceEvent(TraceEventType.Information, 0, "Worker server reconnection cancelled.");
			}
		}

		public IPluginsIpcService GetWorkerInstance(String workerId)
		{
			if(String.IsNullOrEmpty(workerId))
				throw new ArgumentNullException(nameof(workerId));

			return this._registerServer.CreateProcessingLogic<IPluginsIpcService>(workerId);
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
				this._controlWebHost?.Stop();
				this._controlWebHost?.Dispose();
				this._controlWebHost = null;

				this._registerServer?.Dispose();
				this._registerServer = null;

				this._workerServer?.Dispose();
				this._workerServer = null;
			}
		}

		~ServiceFactory()
			=> this.Dispose(false);

		private Boolean TryCreateWebHost()
		{
			try
			{
				this._controlWebHost = new HttpServerFacade(this._plugin, new Object[] { new PluginsController(this._plugin.Host, this), new Push.PushController(this._plugin), });
				this._controlWebHost.Start();
				return true;
			} catch(HttpListenerException exc)
			{
				this._controlWebHost = null;

				switch(exc.ErrorCode)
				{
				case 5://Access is denied
					CheckAdministratorAccess(this._hostUrl, exc);
					throw;
				case 183://Address already in use exception
					return false;
				default:
					throw;
				}
			}
		}

		/// <summary>Check for administrator rights for current user and modify exception for fixing details</summary>
		private static void CheckAdministratorAccess(String serverUrl, Exception exc)
		{
			Boolean isAdministrator;
			using(WindowsIdentity identity = WindowsIdentity.GetCurrent())
				isAdministrator = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);

			if(!isAdministrator)
			{
				String hostUrl = serverUrl.EndsWith("/") ? serverUrl : (serverUrl + "/");
				exc.Data.Add("netsh", $"netsh http add urlacl url={hostUrl} user={Environment.UserDomainName}\\{Environment.UserName}");
				Plugin.Trace.TraceEvent(TraceEventType.Warning, 6, "You have to reserve host with netsh (see exception details for example) command or run application in [Administrator] mode.");
			}
		}
	}
}