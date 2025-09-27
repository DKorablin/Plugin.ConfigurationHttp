using System;
using System.Runtime.Serialization;
using SAL.Flatbed;

namespace Plugin.ConfigurationHttp.Controllers.Message
{
	[DataContract]
	public class PluginResponse
	{
		/// <summary>The plugin unique identifier.</summary>
		/// <remarks>For the time being the Name is a unique identifier for the plugin</remarks>
		[DataMember]
		public String ID { get; private set; }

		/// <summary>The plugin name.</summary>
		[DataMember]
		public String Name { get; private set; }

		/// <summary>The source of the plugin</summary>
		[DataMember]
		public String Source { get; private set; }

		/// <summary>The plugin version.</summary>
		[DataMember]
		public Version Version { get; private set; }

		/// <summary>The plugin (assembly) description.</summary>
		[DataMember]
		public String Description { get; private set; }

		/// <summary>Assembly company</summary>
		[DataMember]
		public String Company { get; private set; }

		/// <summary>Plugin copyright.</summary>
		[DataMember]
		public String Copyright { get; private set; }

		internal PluginResponse(IPluginDescription plugin)
		{
			this.Company = plugin.Company;
			this.Copyright = plugin.Copyright;
			this.Description = plugin.Description;
			this.ID = plugin.ID;
			this.Name = plugin.Name;
			this.Source = plugin.Source;
			this.Version = plugin.Version;
		}
	}
}