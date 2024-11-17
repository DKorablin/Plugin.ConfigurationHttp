using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;

namespace Plugin.ConfigurationHttp
{
	/// <summary>Фасад HTTP сервера</summary>
	internal class HttpServerFacade : IDisposable
	{
		private readonly Plugin _plugin;
		private readonly HttpListenerWrapper _wrapper;
		private readonly ControllersWrapper _controllers;
		private static readonly StaticFilesWrapper Static = new StaticFilesWrapper();

		/// <summary>Сервис в режиме прослушки</summary>
		public Boolean IsListening => this._wrapper.IsListening;

		public IEnumerable<String> Endpoints => this._wrapper.Endpoints;

		/// <summary>Конструктор фасада</summary>
		/// <param name="plugin">Плагин</param>
		/// <param name="controllers">Массив контроллеров, которые обрабатывают запросы клиентов</param>
		public HttpServerFacade(Plugin plugin, Object[] controllers)
		{
			this._plugin = plugin;
			this._plugin.Settings.PropertyChanged += Settings_PropertyChanged;
			this._controllers = new ControllersWrapper(controllers);

			this._wrapper = new HttpListenerWrapper();
			this._wrapper.ProcessRequest += wrapper_ProcessRequest;
		}

		/// <summary>Запустить сервер</summary>
		public void Start()
			=> this._wrapper.Start(this._plugin.Settings.GetHostUrl(),
				this._plugin.Settings.ListenersCount,
				this._plugin.Settings.IgnoreWriteExceptions,
				(AuthenticationSchemes)this._plugin.Settings.AuthenticationSchemes);

		/// <summary>Остановить сервер</summary>
		public void Stop()
			=> this._wrapper.Stop();

		public void Dispose()
		{
			this._wrapper.Dispose();
			this._plugin.Settings.PropertyChanged -= Settings_PropertyChanged;
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

		private void wrapper_ProcessRequest(HttpListenerContext context)
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

		/// <summary>Поискать ответ из контроллерах или ресурсах</summary>
		/// <param name="request">Запрос клиента</param>
		/// <returns>Результат</returns>
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
				} /*else if(localPath == "/favicon.ico")
				{
					System.Drawing.Icon.ExtractAssociatedIcon()
				}*/
			}

			if(result.Payload == null)
				response.StatusCode = (Int32)HttpStatusCode.NotFound;
			else if(result.Payload.Length == 0)
				response.StatusCode = (Int32)HttpStatusCode.NoContent;
			else
			{
				//String strResponse = String.Format("<HTML><BODY>Method: {0} Url.PathAndQuery: {1}</BODY></HTML>", request.HttpMethod,request.Url.PathAndQuery);
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

		/// <summary>Получить результат из подгруженных контроллеров</summary>
		/// <param name="localPath">Путь до метода в контроллере</param>
		/// <param name="request">Входящий HTTP(S) запрос</param>
		/// <returns>Результат в массиве байт или null</returns>
		private ResponseBase GetControllerResult(String localPath, HttpListenerRequest request)
		{
			NameValueCollection keyValue = HttpServerFacade.ParseMethodParams(request);
			MethodWrapper method = this._controllers.Get(request.HttpMethod, localPath, keyValue.AllKeys);

			if(method == null
				|| (method.HttpMethod != null && method.HttpMethod != request.HttpMethod))
				return new ResponseBase();

			return method.Invoke(keyValue);
		}

		/// <summary>Получить входящие параметры в ввиде Ключ/Значение</summary>
		/// <param name="request">Внешний HTTP(S) запрос</param>
		/// <returns>Ключ/Значение входящих параметров, из запроса</returns>
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