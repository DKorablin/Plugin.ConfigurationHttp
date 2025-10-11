using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.Web.Configuration;
using System.Web.Hosting;

namespace Plugin.ConfigurationHttp.Ipc
{
	public sealed class ServiceConfiguration
	{
		private readonly ServiceModelSectionGroup _serviceModelGroup;

		public static readonly ServiceConfiguration Instance = new ServiceConfiguration();

		private ServiceConfiguration()
		{
			Configuration configuration = HostingEnvironment.IsHosted
				? WebConfigurationManager.OpenWebConfiguration("~")
				: ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

			this._serviceModelGroup = configuration == null ? null : ServiceModelSectionGroup.GetSectionGroup(configuration);
		}

		public ServiceHost Create<TService, TEndpoint>(String baseAddress, String address)
		{
			if(this.CheckServiceConfiguration<TEndpoint>())
				return new ServiceHost(typeof(TService));
			else
			{
				ServiceHost result = new ServiceHost(typeof(TService), new Uri(baseAddress));

				NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
				{//https://stackoverflow.com/questions/2911221/what-is-the-purpose-of-wcf-reliable-session
					ReceiveTimeout = TimeSpan.MaxValue,
				};
				ServiceEndpoint endpoint = result.AddServiceEndpoint(typeof(TEndpoint), binding, address);

				return result;
			}
		}

		private Boolean CheckClientConfiguration<TEndpoint>()
		{
			if(this._serviceModelGroup == null)
				return false;

			foreach(ChannelEndpointElement endpoint in this._serviceModelGroup.Client.Endpoints)
				if(endpoint.Contract == typeof(TEndpoint).FullName)
					return true;

			return false;
		}

		private Boolean CheckServiceConfiguration<TService>()
		{
			/*
			TODO: To ensure that reading from the .config file works correctly, you need to create two different contracts.
			The first one is for the web, and the second one is for IPC. Separating the interfaces doesn't work.
			*/
			if(this._serviceModelGroup == null)
				return false;

			foreach(ServiceElement service in this._serviceModelGroup.Services.Services)
				if(service.Name == typeof(TService).FullName)
					return true;

			return false;
		}
	}
}