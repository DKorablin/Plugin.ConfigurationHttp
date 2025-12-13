using System;
using System.Diagnostics;
using System.Linq;
using SAL.Flatbed;
#if NET8_0_OR_GREATER
using CoreWCF;
#else
using System.ServiceModel;
#endif

namespace Plugin.ConfigurationHttp
{
	public class Plugin : IPlugin, IPluginSettings<PluginSettings>
	{
		private static TraceSource _trace;
		private ServiceFactory _server;
		internal static PluginSettings _settings;

		internal IHost Host { get; }
		internal static IHost SHost { get; private set; }

		internal static TraceSource Trace => Plugin._trace ?? (Plugin._trace = Plugin.CreateTraceSource<Plugin>());

		/// <summary>Settings for interaction from the host</summary>
		Object IPluginSettings.Settings => this.Settings;

		/// <summary>Settings for interaction from the plugin</summary>
		public PluginSettings Settings
		{
			get
			{
				if(Plugin._settings == null)
				{
					Plugin._settings = new PluginSettings(this.Host);
					this.Host.Plugins.Settings(this).LoadAssemblyParameters(Plugin._settings);
				}
				return Plugin._settings;
			}
		}

		public Boolean IsStarted => this._server.State == CommunicationState.Opening;

		public Plugin(IHost host)
			=> Plugin.SHost = this.Host = host ?? throw new ArgumentNullException(nameof(host));

		Boolean IPlugin.OnConnection(ConnectMode mode)
		{
			this._server = new ServiceFactory(this);
			this._server.Connected += this.Server_Connected;
			this._server.Connect(this.Settings.GetHostUrl());
			return true;
		}

		private void Server_Connected(Object sender, EventArgs e)
			=> Plugin.Trace.TraceEvent(TraceEventType.Start, 1, "Started at Url:\r\n\t{0}", String.Join("\r\n\t", this._server.GetHostEndpoints().ToArray()));

		Boolean IPlugin.OnDisconnection(DisconnectMode mode)
		{
			if(this._server != null)
			{
				this._server.Dispose();
				this._server = null;
			}
			return true;
		}

		private static TraceSource CreateTraceSource<T>(String name = null) where T : IPlugin
		{
			TraceSource result = new TraceSource(typeof(T).Assembly.GetName().Name + name);
			result.Switch.Level = SourceLevels.All;
			result.Listeners.Remove("Default");
			result.Listeners.AddRange(System.Diagnostics.Trace.Listeners);
			return result;
		}
	}
}