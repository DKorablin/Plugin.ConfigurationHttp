using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using Org.BouncyCastle.Asn1.Ocsp;
using Plugin.ConfigurationHttp.UI;
using SAL.Flatbed;
using WebPush;

namespace Plugin.ConfigurationHttp
{
	public class PluginSettings : INotifyPropertyChanged
	{
		private class PushMessage
		{
			public String Title { get; set; }
			public String Description { get; set; }
		}

		/// <summary>Data for sending WebPush messages</summary>
		[Serializable]
		public class PushSettings
		{
			private String _endpoint;
			private String _p256dh;
			private String _auth;

			[Category("Web Push")]
			[DisplayName("endpoint")]
			[Description("The endpoint takes the form of a custom URL pointing to a push server, which can be used to send a push message to the particular service worker instance that subscribed to the push service")]
			public String Endpoint
			{
				get => this._endpoint;
				set
				{
					if(String.IsNullOrWhiteSpace(value))
						this._endpoint = null;
					else if(Uri.IsWellFormedUriString(value, UriKind.Absolute))
						this._endpoint = value;
				}
			}

			[Category("Web Push")]
			[DisplayName("p256dh")]
			[Description("An Elliptic curve Diffie–Hellman public key on the P-256 curve (that is, the NIST secp256r1 elliptic curve).\r\nThe resulting key is an uncompressed point in ANSI X9.62 format.")]
			public String P256dh
			{
				get => this._p256dh;
				set => this._p256dh = String.IsNullOrWhiteSpace(value) ? null : value.Trim();
			}

			[Category("Web Push")]
			[DisplayName("auth")]
			[Description("An authentication secret, as described in Message Encryption for Web Push")]
			public String Auth
			{
				get => this._auth;
				set => this._auth = String.IsNullOrWhiteSpace(value) ? null : value.Trim();
			}

			public PushSettings()
			{ }

			public PushSettings(String endpoint,String p256dh, String auth)
			{
				this.Endpoint = endpoint;
				this.P256dh = p256dh;
				this.Auth = auth;
			}

			public Boolean IsEmpty()
				=> this._endpoint == null && this._p256dh == null && this._auth == null;

			public override String ToString()
				=> this.Endpoint != null && this.P256dh != null && this.Auth != null
					? "{Enabled}"
					: "{Disabled}";

			public override Boolean Equals(Object obj)
			{
				if(!(obj is PushSettings push))
					return false;

				return push.IsEmpty() == this.IsEmpty() || this.Endpoint == push.Endpoint && this.Auth == push.Auth && this.P256dh == push.P256dh;
			}

			public override Int32 GetHashCode()
				=> this.Endpoint == null ? 0 : this.Endpoint.GetHashCode();
		}

		private static class Constants
		{
			public const String TemplateIpAddr = "{ipAddress}";
			public const String HostUrl = "http://" + TemplateIpAddr + ":8180/";
			public const Int32 WebPushTtl = 0;
		}

		private readonly IHost _host;
		private String _hostUrl = Constants.HostUrl;

		private Int32 _listenersCount = 1;
		private Boolean _ignoreWriteExceptions = true;
		private Boolean _unsafeConnectionNtlmAuthentication = false;
		private String _realm;
		private String[] _users;
		private AuthenticationSchemes _authenticationSchemes = System.Net.AuthenticationSchemes.Anonymous;

		private TraceEventType _webPushEventTypes = TraceEventType.Error;
		private Int32 _webPushTtl = Constants.WebPushTtl;
		private String _vapidSubject;
		private String _vapidPublicKey;
		private String _vapidPrivateKey;

		private static IPAddress _HostAddress;

		[Category("Server")]
		[DefaultValue(Constants.HostUrl)]
		[Description("Deployment host for Configuration service. Use " + Constants.TemplateIpAddr + " template for Dns.GetHostName().\r\nFor SSL don't forget to specify certificate at:\r\nnetsh http add sslcert ipport=[ipAddress]:8180 certhash=[thumbprint] appid={d10da6bc-77fd-4ada-8b3f-b850023e59ae}")]
		public String HostUrl
		{
			get => this._hostUrl;
			set
			{
				String _value = String.IsNullOrEmpty(value) ? Constants.HostUrl : value;
				this.SetField(ref this._hostUrl, _value, nameof(this.HostUrl));
			}
		}

		[Category("Server")]
		[DefaultValue(1)]
		[Description("HTTP server listeners")]
		public Int32 ListenersCount
		{
			get => this._listenersCount;
			set => this.SetField(ref this._listenersCount, value > 0 ? value : this._listenersCount, nameof(this.ListenersCount));
		}

		[Category("Server")]
		[DefaultValue(true)]
		[Description("Gets or sets a Boolean value that specifies whether your application receives exceptions that occur when an HttpListener sends the response to the client.")]
		public Boolean IgnoreWriteExceptions
		{
			get => this._ignoreWriteExceptions;
			set => this.SetField(ref this._ignoreWriteExceptions, value, nameof(this.IgnoreWriteExceptions));
		}

		[Category("Authentication")]
		[DefaultValue(false)]
		[Description("Gets or sets a Boolean value that controls whether, when NTLM is used, additional requests using the same Transmission Control Protocol (TCP) connection are required to authenticate.")]
		public Boolean UnsafeConnectionNtlmAuthentication
		{
			get => this._unsafeConnectionNtlmAuthentication;
			set => this.SetField(ref this._unsafeConnectionNtlmAuthentication, value, nameof(this.UnsafeConnectionNtlmAuthentication));
		}

		[Category("Authentication")]
		[Description("Servers use realms to partition protected resources; each partition can have its own authentication scheme and/or authorization database. Realms are used only for basic and digest authentication. After a client successfully authenticates, the authentication is valid for all resources in a given realm. For a detailed description of realms, see RFC 2617.")]
		public String Realm
		{
			get => this._realm;
			set => this.SetField(ref this._realm, value, nameof(this.Realm));
		}

		[Category("Authentication")]
		[DefaultValue(System.Net.AuthenticationSchemes.Anonymous)]
		[Description("A bitwise combination of AuthenticationSchemes enumeration values that indicates how clients are to be authenticated. The default value is Anonymous.")]
		[Editor(typeof(ColumnEditorTyped<AuthenticationSchemes>), typeof(UITypeEditor))]
		public AuthenticationSchemes AuthenticationSchemes
		{
			get => this._authenticationSchemes;
			set => this.SetField(ref this._authenticationSchemes,
					value < 0 ? System.Net.AuthenticationSchemes.Anonymous : value,
					nameof(this.AuthenticationSchemes));
		}

		[Category("Authentication")]
		[Description("A list of user names that are allowed access to the resource")]
		public String[] Users
		{
			get => this._users;
			set => this.SetField(ref this._users, value == null || value.Length == 0 ? null : value, nameof(this.Users));
		}

		[Category("Notifications")]
		[DisplayName("Subscriber")]
		[Description("Data for sending HTTP PUSH notifications (RFC-8030)")]
		[TypeConverter(typeof(ExpandableObjectConverter))]
		public PushSettings WebPush { get; private set; }

		[Category("Notifications")]
		[DisplayName("Publish Events")]
		[Description("Which type of events to send")]
		[DefaultValue(TraceEventType.Error)]
		[Editor(typeof(ColumnEditor<TraceEventType>), typeof(UITypeEditor))]
		public TraceEventType WebPushEventTypes
		{
			get => this._webPushEventTypes;
			set => this.SetField(ref this._webPushEventTypes, value == 0 ? TraceEventType.Error : value, nameof(this.WebPushEventTypes));
		}

		[Category("Notifications")]
		[DisplayName("Time to Live")]
		[Description("Specifies the time (in seconds) the push message is retained by the push service and attempts to deliver it to the user agent. This value must be an integer between 0 and 10,000, inclusive.\r\nA value of 0 indicates that the push message is not retained at all. The default value is 0.")]
		[DefaultValue(Constants.WebPushTtl)]
		public Int32 WebPushTtl
		{
			get => this._webPushTtl;
			set => this.SetField(ref this._webPushTtl, value < 0 || value > 10000 ? this.WebPushTtl : value, nameof(this.WebPushTtl));
		}

		[Category("Notifications")]
		[DisplayName("VAPID Subject")]
		[Description("A contact URI for the application server.\r\nThis string is either a mailto: or a URL.\r\nIt is used by push services to contact the application server operator if necessary.")]
		public String VapidSubject
		{
			get => this._vapidSubject;
			set => this.SetField(ref this._vapidSubject, value?.Trim(), nameof(this.VapidSubject));
		}

		[Category("Notifications")]
		[DisplayName("VAPID Public Key")]
		[Description("VAPID (Voluntary Application Server Identification) keys are an Elliptic Curve key pair will be generated automatically on first request")]
		public String VapidPublicKey
		{
			get
			{
				if(this._vapidPublicKey == null)
				{
					Utils.GenerateVapidKeys(out String publicKey, out String privateKey);
					this.VapidPublicKey = publicKey;
					this.VapidPrivateKey = privateKey;
				}
				return this._vapidPublicKey;
			}
			set => this.SetField(ref this._vapidPublicKey, value?.Trim(), nameof(this.VapidPublicKey));
		}

		[Category("Notifications")]
		[DisplayName("VAPID Private Key")]
		[Description("VAPID (Voluntary Application Server Identification) keys are an Elliptic Curve key pair will be generated automatically on first request")]
		public String VapidPrivateKey
		{
			get
			{
				if(this._vapidPrivateKey == null)
				{
					Utils.GenerateVapidKeys(out String publicKey, out String privateKey);
					this.VapidPublicKey = publicKey;
					this.VapidPrivateKey = privateKey;
				}
				return this._vapidPrivateKey;
			}
			set => this.SetField(ref this._vapidPrivateKey, value?.Trim(), nameof(this.VapidPrivateKey));
		}

		[Browsable(false)]
		public String WebPushJson
		{
			get => this.WebPush.IsEmpty() ? null : Serializer.JavaScriptSerialize(this.WebPush);
			set
			{
				PushSettings newValue = value == null
					? new PushSettings()
					: Serializer.JavaScriptDeserialize<PushSettings>(value);

				if(newValue == this.WebPush)
					return;

				this.WebPush = newValue;
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WebPushJson)));
			}
		}

		/// <summary>Host address of the current machine</summary>
		private static IPAddress HostAddress
		{
			get
			{
				if(_HostAddress == null)
				{
					IPHostEntry ip = Dns.GetHostEntry(Dns.GetHostName());
					_HostAddress = Array.Find(ip.AddressList, addr => addr.AddressFamily == AddressFamily.InterNetwork);
				}
				return _HostAddress;
			}
		}

		internal PluginSettings(IHost host)
		{
			this._host = host;
			this.WebPush = new PushSettings();
		}

		/// <summary>Get a note with custom formatting</summary>
		/// <returns>Host with additional formatting</returns>
		internal String GetHostUrl()
		{
			String result = this.HostUrl;

			return result.Contains(Constants.TemplateIpAddr)
				? result.Replace(Constants.TemplateIpAddr, PluginSettings.HostAddress.ToString())
				: result;
		}

		/// <summary>Verify user authenticity against an internal list</summary>
		/// <param name="principal">User being verified</param>
		/// <returns>Authentication successful</returns>
		internal Boolean Authenticate(IPrincipal principal)
		{
			if(this.Users == null)
				return true;//We ignore users because they are not specified

			if((this.AuthenticationSchemes | System.Net.AuthenticationSchemes.Anonymous) == System.Net.AuthenticationSchemes.Anonymous
				|| (this.AuthenticationSchemes | System.Net.AuthenticationSchemes.None) == System.Net.AuthenticationSchemes.None)
				return true;//Anonymous authentication scheme, ignoring users

			if(principal == null || !principal.Identity.IsAuthenticated)
				return false;//The user is not transferred, but according to the scheme one should be

			return Array.Exists(this.Users, p => p == principal.Identity.Name);//We check users against an internal list
		}

		/// <summary>Get the name of the application for which the AutoStart function is registered</summary>
		internal String GetApplicationName()
		{
			StringBuilder result = new StringBuilder();
			foreach(IPluginDescription kernel in this._host.Plugins.FindPluginType<IPluginKernel>())
				result.Append(kernel.ID);

			return result.ToString();
		}

		/// <summary>Send an HTTP PUSH message to a user</summary>
		/// <param name="title">The title of the message being sent</param>
		/// <param name="description">Description of the message being sent</param>
		internal void SendPushMessage(String title, String description)
		{
			if(this.WebPush.IsEmpty())
				return;
			if(this.VapidSubject == null)
				return;

			try
			{

				String json = Serializer.JavaScriptSerialize(new PushMessage() { Title = title, Description = description, });

				PushSubscription subscription = new PushSubscription(this.WebPush.Endpoint, this.WebPush.P256dh, this.WebPush.Auth);

				using(WebPushClient client = new WebPushClient())
				{
					client.SetVapidDetails(this.VapidSubject, this.VapidPublicKey, this.VapidPrivateKey);

					Dictionary<String, Object> options = new Dictionary<String, Object>()
					{
						{ "TTL", this.WebPushTtl },
					}
				;
					client.SendNotification(subscription, payload: json, options: options);
				}
			} catch(WebPushException exc)
			{
				switch(exc.HttpResponseMessage.StatusCode)
				{
				case System.Net.HttpStatusCode.Gone:
					this.WebPushJson = null;
					break;
				}
			}
		}

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;
		private Boolean SetField<T>(ref T field, T value, String propertyName)
		{
			if(EqualityComparer<T>.Default.Equals(field, value))
				return false;

			field = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			return true;
		}
		#endregion INotifyPropertyChanged
	}
}