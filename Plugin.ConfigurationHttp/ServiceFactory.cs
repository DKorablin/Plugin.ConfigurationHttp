using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using System.Net;
#if NETFRAMEWORK
using System.ServiceModel;
using System.ServiceModel.Description;
#else
using CoreWCF;
using CoreWCF.Description;
using Microsoft.Extensions.DependencyInjection;
using CommunicationState = CoreWCF.CommunicationState;
using ServiceHost = Plugin.ConfigurationHttp.CoreWcfServiceHost;
#endif
using Plugin.ConfigurationHttp.Controllers.Message;
using Plugin.ConfigurationHttp.Ipc;
using Plugin.ConfigurationHttp.Ipc.Control;

namespace Plugin.ConfigurationHttp
{
	internal class ServiceFactory : IDisposable
	{
		//TODO: Upgrade to ConcurrentDictionary
		public static readonly Dictionary<Int32, PluginsServiceProxy> Proxies = new Dictionary<Int32, PluginsServiceProxy>();

		private readonly Plugin _plugin;
		private IpcSingleton _ipc;

		private ServiceHost _controlHost;

		private HttpServerFacade _controlWebHost;
		private ControlServiceProxy _controlProxy;
		private String _hostUrl;
		private Timer _ping;
		private readonly Object ObjLock = new Object();

		private String BaseAddress => "net.pipe://" + Environment.MachineName + "/Plugin.ConfigurationHttp" + this._hostUrl.GetHashCode();
		private String BaseControlAddress => this.BaseAddress + "/Control";

		public Boolean IsHost => this._controlWebHost != null; // CoreWCF hosted inside generic host
		public event EventHandler<EventArgs> Connected;

		public CommunicationState State => this._controlWebHost != null && this._controlWebHost.IsListening ? CommunicationState.Opened : CommunicationState.Closed;

		public ServiceFactory(Plugin plugin)
			=> this._plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));

		/// <summary>Get a list of addresses under which hosts are running</summary>
		/// <returns></returns>
		public IEnumerable<String> GetHostEndpoints()
		{
			if(this._controlWebHost != null)
				foreach(String addr in this._controlWebHost.Endpoints)
					yield return addr;
				// Original WCF control host endpoints omitted in CoreWCF multi-target simplification
			if(this._controlProxy != null)
				foreach(ServiceEndpoint addr in this._controlProxy.PluginsHost.Description.Endpoints)
					yield return addr.Address.ToString();
		}

		private Boolean TryCreateWebHost()
		{
			try
			{
				this._controlWebHost = new HttpServerFacade(this._plugin, new Object[] { new PluginsController(this._plugin.Host), new Push.PushController(this._plugin), });
				this._controlWebHost.Start();
				//this._controlWebHost.Faulted += ControlHost_Faulted;
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
			} catch(AddressAccessDeniedException exc)
			{
				this._controlWebHost = null;

				CheckAdministratorAccess(this._hostUrl, exc);
				throw;
			}
		}

		private void TryCreateControlProxy()
		{
			try
			{
				/*TODO: There is a floating exception here when _controlWebHost is already open, but _controlHost has not yet been created.
				In this case, _controlProxy cannot connect to _controlHost that has not yet been created.*/
				this._controlProxy = new ControlServiceProxy(this.BaseControlAddress, "Host");
				this._controlProxy.CreateClientHost();
			} catch(EndpointNotFoundException exc)
			{
				exc.Data.Add(nameof(this._hostUrl), this._hostUrl);
				exc.Data.Add(nameof(this.BaseControlAddress), this.BaseControlAddress + "/Host");
				Plugin.Trace.TraceEvent(TraceEventType.Warning, 6, $"IPC control host not found. Probably address {this._hostUrl} already in use in different application");
				throw;
			}
		}

		public void Connect(String hostUrl)
		{
			if(String.IsNullOrEmpty(hostUrl))
				throw new ArgumentNullException(nameof(hostUrl));

			this._hostUrl = hostUrl;
			this._ipc = new IpcSingleton("Global\\Plugin.ConfigurationHttp." + this._hostUrl.GetHashCode(), new TimeSpan(0, 0, 10));
			this._ipc.Mutex<Object>(null, p =>
			{
				try
				{
					if(this.TryCreateWebHost())
					{
						this._controlHost = Ipc.ServiceConfiguration.Instance.Create<ControlService, IControlService>(this.BaseControlAddress, "Host");
						this._controlHost.Open();
						this._controlHost.Faulted += (sender, e) => Plugin.Trace.TraceEvent(TraceEventType.Error, 10, "ControlHost is in faulted state");

						// Initialize CoreWCF host (simplified: control service host creation placeholder)
						//Ipc.ServiceConfiguration.Instance.EnsureCoreWcfHost<ControlService, IControlService>(this.BaseControlAddress);
					} else
					{
						this.TryCreateControlProxy();
					}
					this._ping = new Timer(this.TimerCallback, this, 5000, 5000);

					this.Connected?.Invoke(this, EventArgs.Empty);
				} catch
				{
					this.Dispose();
					throw;
				}
			});
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

		public void Dispose()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();

			CommunicationState state = this.State;
			lock(ObjLock)
			{
				if(this._ping != null)
				{
					this._ping.Dispose();
					this._ping = null;
				}

				AbortServiceHost(nameof(this._controlWebHost), this._controlWebHost, p => p.Stop());

				AbortServiceHost(nameof(this._controlProxy), this._controlProxy, p => p.DisconnectControlHost());

				AbortServiceHost(nameof(this._controlHost), this._controlHost, p => p.Abort());
			}

			sw.Stop();
			Plugin.Trace.TraceEvent(TraceEventType.Verbose, 7, "Destroyed. State: {0} Elapsed: {1} ", state, sw.Elapsed);
		}

		private static void AbortServiceHost<T>(String name, T service, Action<T> method) where T : class
		{
			if(service != null)
				try
				{
					method(service);
				} catch(System.ServiceModel.CommunicationObjectFaultedException exc)
				{
					Plugin.Trace.TraceEvent(TraceEventType.Warning, 5, name + " Dispose exception: " + exc.Message);
				}
		}

		private void TimerCallback(Object state)
		{
			this._ping.Change(Timeout.Infinite, Timeout.Infinite);

			ServiceFactory communication = (ServiceFactory)state;
			try
			{
				if(communication.IsHost)
				{
					PluginResponse[] data;
					List<Int32> failedProxies = new List<Int32>();
					foreach(KeyValuePair<Int32, PluginsServiceProxy> proxy in ServiceFactory.Proxies)//TODO: Streams can remove or add objects from the dictionary.
						try
						{
							data = proxy.Value.Plugins.GetPlugins(null);
						} catch(CommunicationObjectFaultedException exc)
						{
							Exception ei = exc.InnerException ?? exc;
							ei.Data.Add("ProxyId", proxy.Key);
							Plugin.Trace.TraceData(TraceEventType.Error, 9, ei);

							failedProxies.Add(proxy.Key);
						}

					foreach(Int32 proxy in failedProxies)//Removing proxies that couldn't be contacted (TODO: It might be worth giving it a few more attempts to connect)
						ServiceFactory.Proxies.Remove(proxy);

				} else if(!this._controlProxy.Ping())
				{
					Plugin.Trace.TraceEvent(TraceEventType.Verbose, 7, "Control Proxy PING failed. Reconnecting...");
					this.Dispose();
					this.Connect(this._hostUrl);
				}
			} catch(Exception exc)
			{
				Exception ei = exc.InnerException ?? exc;
				Plugin.Trace.TraceData(TraceEventType.Error, 10, ei);
			} finally
			{
				this._ping?.Change(5000, 5000);
			}
		}
	}
}