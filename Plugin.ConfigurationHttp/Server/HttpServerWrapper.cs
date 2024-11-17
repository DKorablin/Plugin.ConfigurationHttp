using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;

namespace Plugin.ConfigurationHttp
{
	internal class HttpServerWrapper
	{
		private readonly Plugin _plugin;
		private HttpListener _listener;
		private ControllersWrapper _controllers;
		private static StaticFilesWrapper Static = new StaticFilesWrapper();

		public Boolean IsListening
		{
			get { return this._listener != null && this._listener.IsListening; }
		}

		public HttpServerWrapper(Plugin plugin, params Object[] controllers)
		{
			this._plugin = plugin;
			this._controllers = new ControllersWrapper(controllers);

			HttpListener listener = new HttpListener();
			listener.Prefixes.Add(this._plugin.Settings.GetHostUrl());
			this._listener = listener;
		}

		public void Start()
		{
			this._listener.Start();
			for(Int32 loop = 0; loop < this._plugin.Settings.ListenersCount; loop++)
				this._listener.BeginGetContext(new AsyncCallback(ListenerCallback), this);
		}

		public void Stop()
		{
			this._listener.Abort();
		}

		private static void ListenerCallback(IAsyncResult result)
		{
			HttpServerWrapper wrapper = (HttpServerWrapper)result.AsyncState;
			if(!wrapper.IsListening)
				return;//Terminate

			HttpListenerContext context = wrapper._listener.EndGetContext(result);

			try
			{
				HttpListenerRequest request = context.Request;
				using(HttpListenerResponse response = context.Response)
					wrapper.SendResponse(request, response);

			} catch(Exception exc)
			{
				if(Utils.IsFatal(exc))
					throw;
				else
					wrapper._plugin.Trace.TraceData(System.Diagnostics.TraceEventType.Error, 10, exc);
			}

			wrapper._listener.BeginGetContext(new AsyncCallback(ListenerCallback), wrapper);
		}

		/// <summary>Поискать ответ из контроллерах или ресурсах</summary>
		/// <param name="request">Запрос клиента</param>
		/// <returns>Результат</returns>
		private void SendResponse(HttpListenerRequest request, HttpListenerResponse response)
		{
			String localPath = StaticFilesWrapper.FormatResourceName(request.Url.LocalPath);
			Byte[] buffer = this.GetControllerResult(localPath, request);
			if(buffer == null)
			{
				StaticFilesWrapper.StaticDescription st = HttpServerWrapper.Static.Get(localPath);
				if(st != null)
				{
					buffer = st.Payload;
					response.ContentType = st.ContentType;
				}
			}

			if(buffer == null)
				response.StatusCode = (Int32)HttpStatusCode.NotFound;
			else
			{
				//String strResponse = String.Format("<HTML><BODY>Method: {0} Url.PathAndQuery: {1}</BODY></HTML>", request.HttpMethod,request.Url.PathAndQuery);
				response.ContentLength64 = buffer.Length;
				using(Stream output = response.OutputStream)
					output.Write(buffer, 0, buffer.Length);
			}
		}

		/// <summary>Получить результат из подгруженных контроллеров</summary>
		/// <param name="localPath">Путь до метода в контроллере</param>
		/// <param name="request">Входящий HTTP(S) запрос</param>
		/// <returns>Результат в массиве байт или null</returns>
		private Byte[] GetControllerResult(String localPath, HttpListenerRequest request)
		{
			MethodWrapper method = this._controllers.Get(request.HttpMethod,localPath);
			if(method == null)
				return null;
			if(method.HttpMethod != null && method.HttpMethod != request.HttpMethod)
				return null;

			NameValueCollection keyValue = HttpServerWrapper.ParseMethodParams(request);
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