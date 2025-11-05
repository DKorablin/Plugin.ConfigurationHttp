using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;

namespace Plugin.ConfigurationHttp
{
	/// <summary>The HTTP(s) server facade.</summary>
	internal class HttpServerFacade : IDisposable
	{
		private readonly Plugin _plugin;
		private readonly HttpListenerWrapper _wrapper;
		private readonly ControllersWrapper _controllers;
		private static readonly StaticFilesWrapper Static = new StaticFilesWrapper();

		/// <summary>The service is started as the listener.</summary>
		public Boolean IsListening => this._wrapper.IsListening;

		public IEnumerable<String> Endpoints => this._wrapper.Endpoints;

		/// <summary>Create instance of <see cref="HttpServerFacade"/> with host plugin instance and the list of known public methods.</summary>
		/// <param name="plugin">The host plugin information.</param>
		/// <param name="controllers">The list of controllers, which can process users requests.</param>
		public HttpServerFacade(Plugin plugin, Object[] controllers)
		{
			this._plugin = plugin;
			this._plugin.Settings.PropertyChanged += this.Settings_PropertyChanged;
			this._controllers = new ControllersWrapper(controllers);

			this._wrapper = new HttpListenerWrapper();
			this._wrapper.ProcessRequest += this.Wrapper_ProcessRequest;
		}

		/// <summary>Start HTTP(s) server</summary>
		public void Start()
			=> this._wrapper.Start(this._plugin.Settings.GetHostUrl(),
				this._plugin.Settings.ListenersCount,
				this._plugin.Settings.IgnoreWriteExceptions,
				this._plugin.Settings.AuthenticationSchemes);

		/// <summary>Stop HTTP(s) server</summary>
		public void Stop()
			=> this._wrapper.Stop();

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(Boolean disposing)
		{
			if(disposing)
			{
				this._wrapper.Dispose();
				this._plugin.Settings.PropertyChanged -= this.Settings_PropertyChanged;
			}
		}

		private void Settings_PropertyChanged(Object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch(e.PropertyName)
			{
			case nameof(PluginSettings.AuthenticationSchemes):
				this._wrapper.AuthenticationSchemes = this._plugin.Settings.AuthenticationSchemes;
				break;
			case nameof(PluginSettings.IgnoreWriteExceptions):
				this._wrapper.IgnoreWriteExceptions = this._plugin.Settings.IgnoreWriteExceptions;
				break;
			}
		}

		private void Wrapper_ProcessRequest(HttpListenerContext context)
		{
			if(!this._plugin.Settings.Authenticate(context.User))
			{
				using(HttpListenerResponse response = context.Response)
					response.StatusCode = (Int32)HttpStatusCode.Unauthorized;
				return;
			}

			try
			{
				HttpListenerRequest request = context.Request;
				using(HttpListenerResponse response = context.Response)
					this.SendResponse(request, response);

			} catch(Exception exc)
			{
				if(Utils.IsFatal(exc))
					throw;
				else
					Plugin.Trace.TraceData(System.Diagnostics.TraceEventType.Error, 10, exc);
			}
		}

		/// <summary>Search for an answer in controllers or resources and send response.</summary>
		/// <param name="request">The user's request</param>
		private void SendResponse(HttpListenerRequest request, HttpListenerResponse response)
		{
			String localPath = StaticFilesWrapper.FormatResourceName(request.Url.LocalPath);
			ResponseBase result = this.GetControllerResult(localPath, request);
			if(!result.IsMethodFound)
			{
				ResponseBase st = HttpServerFacade.Static.Get(localPath);
				if(st != null)
				{
					result.Payload = st.Payload;
					result.ContentType = st.ContentType;
				}
			}

			if(result.Payload == null)
				response.StatusCode = (Int32)HttpStatusCode.NotFound;
			else if(result.Payload.Length == 0)
				response.StatusCode = (Int32)HttpStatusCode.NoContent;
			else
			{
				if(result.ContentType != null)
					response.ContentType = result.ContentType;

				//HACK: MSIE11 Headers Because its ignores repeatable GET requests
				response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");
				response.AddHeader("Pragma", "no-cache");
				response.AddHeader("Expires", "0");

				response.ContentLength64 = result.Payload.Length;
				using(Stream output = response.OutputStream)
					output.Write(result.Payload, 0, result.Payload.Length);
			}
		}

		/// <summary>Get the result from loaded controllers</summary>
		/// <param name="localPath">Path to the method in the controller</param>
		/// <param name="request">Incoming HTTP(s) request</param>
		/// <returns>The result is a byte array or null.</returns>
		private ResponseBase GetControllerResult(String localPath, HttpListenerRequest request)
		{
			NameValueCollection keyValue = HttpServerFacade.ParseMethodParams(request);
			MethodWrapper method = this._controllers.Get(request.HttpMethod, localPath, keyValue.AllKeys);

			if(method == null
				|| (method.HttpMethod != null && method.HttpMethod != request.HttpMethod))
				return new ResponseBase();

			return method.Invoke(keyValue);
		}

		/// <summary>Get input parameters as Key/Value</summary>
		/// <param name="request">External HTTP(s) request</param>
		/// <returns>Key/Value of incoming parameters from the request</returns>
		private static NameValueCollection ParseMethodParams(HttpListenerRequest request)
		{
			NameValueCollection result = HttpUtility.ParseQueryString(request.Url.Query);

			if(request.HasEntityBody)
			{
				String body;
				using(StreamReader reader = new StreamReader(request.InputStream))
					body = reader.ReadToEnd();
				result.Add(HttpUtility.ParseQueryString(body));
			}

			return result;
		}
	}
}