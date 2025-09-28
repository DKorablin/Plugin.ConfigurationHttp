using System;
using System.Diagnostics;
using WebPush;

namespace Plugin.ConfigurationHttp
{
	internal class WebPushTraceListener : TraceListener
	{
		private const String UnknownCategory = "Unknown";

		public override void Write(String message)
			=> this.TraceEvent(null, WebPushTraceListener.UnknownCategory, TraceEventType.Verbose, 0, message);

		public override void WriteLine(String message)
			=> this.TraceEvent(null, WebPushTraceListener.UnknownCategory, TraceEventType.Verbose, 0, message);

		public override void Write(Object o)
			=> this.AnalyzeTraceData(null, WebPushTraceListener.UnknownCategory, TraceEventType.Verbose, 0, o == null ? String.Empty : o);

		public override void WriteLine(Object o)
			=> this.AnalyzeTraceData(null, WebPushTraceListener.UnknownCategory, TraceEventType.Verbose, 0, o == null ? String.Empty : o);

		public override void Write(Object o, String category)
			=> this.AnalyzeTraceData(null, category, TraceEventType.Verbose, 1, o);

		public override void WriteLine(Object o, String category)
			=> this.AnalyzeTraceData(null, category, TraceEventType.Verbose, 1, o);

		public override void Write(String message, String category)
			=> this.TraceEvent(null, category, TraceEventType.Verbose, 0, message);

		public override void WriteLine(String message, String category)
			=> this.TraceEvent(null, category, TraceEventType.Verbose, 0, message);

		public override void TraceData(TraceEventCache eventCache, String source, TraceEventType eventType, Int32 id, params Object[] data)
		{
			if(data == null)
				return;

			for(Int32 loop = 0; loop < data.Length; loop++)
				if(data[loop] != null)
					this.AnalyzeTraceData(eventCache, source, eventType, id, data[loop]);
			//base.TraceData(eventCache, source, eventType, id, data);
		}

		public override void TraceData(TraceEventCache eventCache, String source, TraceEventType eventType, Int32 id, Object data)
			=> this.AnalyzeTraceData(eventCache, source, eventType, id, data);

		public override void TraceEvent(TraceEventCache eventCache, String source, TraceEventType eventType, Int32 id, String format, params Object[] args)
		{
			String message = args == null ? format : String.Format(format, args);

			this.TraceEvent(eventCache, source, eventType, id, message);
			//base.TraceEvent(eventCache, source, eventType, id, format, args);
		}

		public override void TraceEvent(TraceEventCache eventCache, String source, TraceEventType eventType, Int32 id, String message)
		{
			if(base.Filter != null && !base.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
				return;

			if(!this.IsPushEnabled())
				return;

			if(((Int32)Plugin._settings.WebPushEventTypes >> (Int32)eventType & 0x01) == 1)
				this.SendPushMessage(source, message);
		}

		private void AnalyzeTraceData(TraceEventCache eventCache, String source, TraceEventType eventType, Int32 id, Object data)
		{
			if(!this.IsPushEnabled())
				return;

			Exception exc = data as Exception;
			if(exc != null && !Utils.IsFatal(exc))
				this.SendPushMessage(exc.GetType().Name, exc.ToString());
			else
			{
				String message;
				if(data == null)
					message = String.Empty;
				else
				{
					Type type = data.GetType();
					switch(type.FullName)
					{
					case "System.Xml.DocumentXPathNavigator"://WCF exceptions come as DocumentXPathNavigator, but instead of XML, it returns the field value...
						message = (String)type.InvokeMember("OuterXml", System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public, null, data, null);
						break;
					default:
						message = data.ToString();
						break;
					}
				}
				this.TraceEvent(eventCache, source, eventType, id, message);
			}
		}

		private Boolean IsPushEnabled()
			=> Plugin._settings != null && Plugin._settings.WebPush != null;

		private void SendPushMessage(String title, String message)
			=> Plugin._settings?.SendPushMessage(title, message);
	}
}