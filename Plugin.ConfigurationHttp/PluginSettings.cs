using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using AlphaOmega.MQ.Publisher;
using Plugin.ConfigurationHttp.UI;
using SAL.Flatbed;

namespace Plugin.ConfigurationHttp
{
	public class PluginSettings : INotifyPropertyChanged
	{
		private class PushMessage
		{
			public String Title { get; set; }
			public String Description { get; set; }
		}

		/// <summary>Данные для отправки WebPush сообщений</summary>
		[Serializable]
		public class PushSettngs
		{
			private Uri _endpoint;
			private Byte[] _p256dh;
			private Byte[] _auth;

			[Category("Web Push")]
			[DisplayName("endpoint")]
			[Description("The endpoint takes the form of a custom URL pointing to a push server, which can be used to send a push message to the particular service worker instance that subscribed to the push service")]
			public String Endpoint
			{
				get => this._endpoint?.ToString();
				set
				{
					if(String.IsNullOrEmpty(value))
						this._endpoint = null;
					else if(value.Trim().Length == 0)
						this._endpoint = null;
					else if(!Uri.TryCreate(value, UriKind.Absolute, out this._endpoint))
						this._endpoint = null;
				}
			}

			[Category("Web Push")]
			[DisplayName("p256dh")]
			[Description("An Elliptic curve Diffie–Hellman public key on the P-256 curve (that is, the NIST secp256r1 elliptic curve).\r\nThe resulting key is an uncompressed point in ANSI X9.62 format.")]
			public String P256dh
			{
				get => this._p256dh == null ? null : Convert.ToBase64String(this._p256dh);
				set
				{
					if(String.IsNullOrEmpty(value))
						value = null;
					else if(value.Trim().Length == 0)
						value = null;

					this._p256dh = value == null
						? null
						: Convert.FromBase64String(value.Replace(' ', '+'));
				}
			}

			[Category("Web Push")]
			[DisplayName("auth")]
			[Description("An authentication secret, as described in Message Encryption for Web Push")]
			public String Auth
			{
				get => this._auth == null ? null : Convert.ToBase64String(this._auth);
				set
				{
					if(String.IsNullOrEmpty(value))
						value = null;
					else if(value.Trim().Length == 0)
						value = null;

					this._auth = value == null
						? null
						: Convert.FromBase64String(value.Replace(' ', '+'));
				}
			}

			internal Uri EndpointI => this._endpoint;

			internal Byte[] P256dhI => this._p256dh;

			internal Byte[] AuthI => this._auth;

			public PushSettngs()
			{ }

			public PushSettngs(String endpoint,String p256dh, String auth)
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
				PushSettngs push = obj as PushSettngs;
				if(push == null)
					return false;
				if(push.IsEmpty() == this.IsEmpty())
					return true;

				return this.EndpointI == push.EndpointI && this.Auth == push.Auth && this.P256dh == push.P256dh;
			}

			public override Int32 GetHashCode()
				=> this.EndpointI == null ? 0 : this.EndpointI.GetHashCode();
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
		private Int32 _webPushTtl = Constants.WebPushTtl;
		private AuthenticationSchemes _authenticationSchemes = System.Net.AuthenticationSchemes.Anonymous;
		private TraceEventType _webPushEventTypes = TraceEventType.Error;
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
				this.SetField(ref this._hostUrl, _value, nameof(HostUrl));
			}
		}

		[Category("Server")]
		[DefaultValue(1)]
		[Description("HTTP server listeners")]
		public Int32 ListenersCount
		{
			get => this._listenersCount;
			set => this.SetField(ref this._listenersCount,
					value > 0 ? value : this._listenersCount,
					nameof(ListenersCount));
		}

		[Category("Server")]
		[DefaultValue(true)]
		[Description("Gets or sets a Boolean value that specifies whether your application receives exceptions that occur when an HttpListener sends the response to the client.")]
		public Boolean IgnoreWriteExceptions
		{
			get => this._ignoreWriteExceptions;
			set => this.SetField(ref this._ignoreWriteExceptions, value, nameof(IgnoreWriteExceptions));
		}

		[Category("Authentication")]
		[DefaultValue(false)]
		[Description("Gets or sets a Boolean value that controls whether, when NTLM is used, additional requests using the same Transmission Control Protocol (TCP) connection are required to authenticate.")]
		public Boolean UnsafeConnectionNtlmAuthentication
		{
			get => this._unsafeConnectionNtlmAuthentication;
			set => this.SetField(ref this._unsafeConnectionNtlmAuthentication, value, nameof(UnsafeConnectionNtlmAuthentication));
		}

		[Category("Authentication")]
		[Description("Servers use realms to partition protected resources; each partition can have its own authentication scheme and/or authorization database. Realms are used only for basic and digest authentication. After a client successfully authenticates, the authentication is valid for all resources in a given realm. For a detailed description of realms, see RFC 2617.")]
		public String Realm
		{
			get => this._realm;
			set => this.SetField(ref this._realm, value, nameof(Realm));
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
					nameof(AuthenticationSchemes));
		}

		[Category("Authentication")]
		[Description("A list of user names that are allowed access to the resource")]
		public String[] Users
		{
			get => this._users;
			set => this.SetField(ref this._users,
					value == null || value.Length == 0 ? null : value,
					nameof(Users));
		}

		[Category("Notifications")]
		[DisplayName("Subscriber")]
		[Description("Data for sending HTTP PUSH notifications (RFC-8030)")]
		[TypeConverter(typeof(ExpandableObjectConverter))]
		public PushSettngs WebPush { get; private set; }

		[Category("Notifications")]
		[DisplayName("Publish Events")]
		[Description("What evens to send")]
		[DefaultValue(TraceEventType.Error)]
		[Editor(typeof(ColumnEditor<TraceEventType>), typeof(UITypeEditor))]
		public TraceEventType WebPushEventTypes
		{
			get => this._webPushEventTypes;
			set => this.SetField(ref this._webPushEventTypes,
					value == 0 ? TraceEventType.Error : value,
					nameof(WebPushEventTypes));
		}

		[Browsable(false)]
		public String WebPushJson
		{
			get => this.WebPush.IsEmpty() ? null : Serializer.JavaScriptSerialize(this.WebPush);
			set
			{
				PushSettngs newValue = value == null
					? new PushSettngs()
					: Serializer.JavaScriptDeserialize<PushSettngs>(value);

				if(newValue == this.WebPush)
					return;

				this.WebPush = newValue;
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WebPushJson)));
			}
		}

		[Category("Notifications")]
		[DisplayName("Time to Live")]
		[Description("Time to live recieved message in browser before it will be destroyed")]
		[DefaultValue(Constants.WebPushTtl)]
		public Int32 WebPushTtl
		{
			get => this._webPushTtl;
			set => this.SetField(ref this._webPushTtl, value < 0 || value > 10000 ? WebPushTtl : value, nameof(WebPushTtl));
		}

		/// <summary>Хост адрес текущей машины</summary>
		private static IPAddress HostAddress
		{
			get
			{
				if(_HostAddress == null)
				{
					IPHostEntry ip = Dns.GetHostEntry(Dns.GetHostName());
					_HostAddress = Array.Find(ip.AddressList, delegate(IPAddress addr) { return addr.AddressFamily == AddressFamily.InterNetwork; });
				}
				return _HostAddress;
			}
		}

		internal PluginSettings(IHost host)
		{
			this._host = host;
			this.WebPush = new PushSettngs();
		}

		/// <summary>Получить ност с кастомным форматированием</summary>
		/// <returns>Хост с дополнительным форматированием</returns>
		internal String GetHostUrl()
		{
			String result = this.HostUrl;

			return result.Contains(Constants.TemplateIpAddr)
				? result.Replace(Constants.TemplateIpAddr, PluginSettings.HostAddress.ToString())
				: result;
		}

		/// <summary>Проверить подлинность пользователя по внутреннему списку</summary>
		/// <param name="principal">Проверяемый пользователь</param>
		/// <returns>Проверка подлинности прошла успешно</returns>
		internal Boolean Authenticate(IPrincipal principal)
		{
			if(this.Users == null)
				return true;//Игнорируем пользователей, ибо они не заданы

			if((this.AuthenticationSchemes | System.Net.AuthenticationSchemes.Anonymous) == System.Net.AuthenticationSchemes.Anonymous
				|| (this.AuthenticationSchemes | System.Net.AuthenticationSchemes.None) == System.Net.AuthenticationSchemes.None)
				return true;//Анонимная схема авторизации, игнорируем пользователей

			if(principal == null || !principal.Identity.IsAuthenticated)
				return false;//Не передан пользователь, однако по схеме одн должен быть

			return Array.Exists(this.Users, p => p == principal.Identity.Name);//Проверяем пользователей по внутреннему списку
		}

		/// <summary>Получить наименование приложения для которого прописывается функция автозапуска</summary>
		internal String GetApplicationName()
		{
			StringBuilder result = new StringBuilder();
			foreach(IPluginDescription kernel in this._host.Plugins.FindPluginType<IPluginKernel>())
				result.Append(kernel.ID);

			return result.ToString();
		}

		/// <summary>Отправить HTTP PUSH сообщение пользователю</summary>
		/// <param name="title">Заголовок отправляемого сообщения</param>
		/// <param name="description">Описание отправляемого сообщения</param>
		internal void SendPushMessage(String title, String description)
		{
			if(this.WebPush.IsEmpty())
				return;

			String json = Serializer.JavaScriptSerialize(new PushMessage() { Title = title, Description = description, });

			PushWebRequest publisher = new PushWebRequest(this.WebPush.EndpointI, this.WebPushTtl, null);
			publisher.SendMessage(this.WebPush.P256dhI, this.WebPush.AuthI, json);
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