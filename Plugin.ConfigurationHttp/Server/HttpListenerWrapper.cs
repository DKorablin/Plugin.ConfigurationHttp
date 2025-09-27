using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace Plugin.ConfigurationHttp
{
	/// <summary>HttpListener wrapper with added listener array.</summary>
	internal class HttpListenerWrapper : IDisposable
	{
		private HttpListener _listener;
		private Thread[] _workers;
		private readonly Thread _listenerThread;
		private readonly ManualResetEvent _stop, _ready;
		private readonly Queue<HttpListenerContext> _queue;

		/// <summary>Client request processing event</summary>
		public event Action<HttpListenerContext> ProcessRequest;

		/// <summary>The state of the service.</summary>
		public Boolean IsListening => this._listener != null && this._listener.IsListening;

		public Boolean IgnoreWriteExceptions
		{
			get => this._listener != null && this._listener.IgnoreWriteExceptions;
			set
			{
				if(this._listener != null)
					this._listener.IgnoreWriteExceptions = value;
			}
		}

		public AuthenticationSchemes AuthenticationSchemes
		{
			get => this._listener == null
					? AuthenticationSchemes.None
					: this._listener.AuthenticationSchemes;
			set
			{
				if(this._listener != null)
					this._listener.AuthenticationSchemes = value;
			}
		}

		public IEnumerable<String> Endpoints
			=> this._listener == null
				? Enumerable.Empty<String>()
				: this._listener.Prefixes;

		public HttpListenerWrapper()
		{
			this._queue = new Queue<HttpListenerContext>();
			this._stop = new ManualResetEvent(false);
			this._ready = new ManualResetEvent(false);
			this._listenerThread = new Thread(HandleRequests);
		}

		/// <summary>Start the HTTP(s) server</summary>
		/// <param name="hostUrl">The host server url</param>
		/// <param name="listenersCount">Number of client request handlers</param>
		/// <param name="ignoreWriteExceptions">Ignoring exceptions when sending responses to the client</param>
		/// <param name="authenticationscheme">The client authentication scheme used</param>
		public void Start(String hostUrl, Int32 listenersCount, Boolean ignoreWriteExceptions, AuthenticationSchemes authenticationScheme)
		{
			if(this.ProcessRequest == null)
				throw new InvalidOperationException("Callback event not defined");

			HttpListener listener = new HttpListener();
			listener.Prefixes.Add(hostUrl);

			listener.IgnoreWriteExceptions = ignoreWriteExceptions;
			listener.AuthenticationSchemes = authenticationScheme;

			this._listener = listener;
			this._listener.Start();

			Thread[] workers = new Thread[listenersCount];
			for(Int32 loop = 0; loop < workers.Length; loop++)
			{
				workers[loop] = new Thread(this.Worker);
				workers[loop].Start();
			}
			this._workers = workers;

			this._listenerThread.Start();
		}

		/// <summary>Start the HTTP(s) server.</summary>
		public void Stop()
		{
			this._stop.Set();

			if(this._listenerThread.IsAlive)
				this._listenerThread.Join();

			if(this._workers != null)
				foreach(Thread worker in this._workers)
					worker.Join();

			if(this._listener != null && this._listener.IsListening)
				this._listener.Stop();
		}

		private void HandleRequests()
		{
			while(this._listener.IsListening)
			{
				IAsyncResult context = this._listener.BeginGetContext(this.ContextReady, null);

				if(WaitHandle.WaitAny(new WaitHandle[] { this._stop, context.AsyncWaitHandle }) == 0)
					return;
			}
		}

		private void ContextReady(IAsyncResult ar)
		{
			try
			{
				lock(this._queue)
				{
					this._queue.Enqueue(this._listener.EndGetContext(ar));
					this._ready.Set();
				}
			} catch(HttpListenerException exc)
			{
				switch(exc.ErrorCode)
				{
				case 995://The I/O operation has been aborted because of either a thread exit or an application request
					break;
				default:
					throw;
				}
			}
			catch(Exception exc)
			{
				if(Utils.IsFatal(exc))
					throw;
				else
					Plugin.Trace.TraceData(System.Diagnostics.TraceEventType.Error, 10, exc);
			}
		}

		private void Worker()
		{
			WaitHandle[] wait = new WaitHandle[] { this._ready, this._stop };
			while(WaitHandle.WaitAny(wait) == 0)
			{
				HttpListenerContext context;
				lock(this._queue)
				{
					if(this._queue.Count > 0)
						context = this._queue.Dequeue();
					else
					{
						this._ready.Reset();
						continue;
					}
				}

				try
				{
					this.ProcessRequest(context);
				}
				catch(Exception exc)
				{
					if(Utils.IsFatal(exc))
						throw;
					else
						Plugin.Trace.TraceData(System.Diagnostics.TraceEventType.Error, 10, exc);
				}
			}
		}

		public void Dispose()
			=> this.Stop();
	}
}