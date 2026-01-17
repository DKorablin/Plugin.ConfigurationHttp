using System;
using SAL.Flatbed;

namespace Plugin.ConfigurationHttp.Controllers.Message
{
	public class PluginResponse
	{
		/// <summary>The plugin unique identifier.</summary>
		/// <remarks>For the time being the Name is a unique identifier for the plugin</remarks>
		public String ID { get; private set; }

		/// <summary>The plugin name.</summary>
		public String Name { get; private set; }

		/// <summary>The source of the plugin.</summary>
		public String Source { get; private set; }

		/// <summary>The plugin version.</summary>
		public Version Version { get; private set; }

		/// <summary>The plugin (assembly) description.</summary>
		public String Description { get; private set; }

		/// <summary>Assembly company.</summary>
		public String Company { get; private set; }

		/// <summary>Plugin copyright.</summary>
		public String Copyright { get; private set; }

		/// <summary>Initializes a new instance of the PluginResponse class from an IPluginDescription.</summary>
		/// <param name="plugin">The plugin description instance.</param>
		internal PluginResponse(IPluginDescription plugin)
			: this(plugin.ID, plugin.Name, plugin.Source, plugin.Version, plugin.Description, plugin.Company, plugin.Copyright)
		{
		}

		/// <summary>Initializes a new instance of the PluginResponse class with specified values.</summary>
		/// <param name="ID">The plugin unique identifier.</param>
		/// <param name="name">The plugin name.</param>
		/// <param name="source">The source of the plugin.</param>
		/// <param name="version">The plugin version.</param>
		/// <param name="description">The plugin description.</param>
		/// <param name="company">The company of the plugin.</param>
		/// <param name="copyright">The copyright of the plugin.</param>
		public PluginResponse(String ID, String name, String source, Version version, String description, String company, String copyright)
		{
			this.Company = company;
			this.Copyright = copyright;
			this.Description = description;
			this.ID = ID;
			this.Name = name;
			this.Source = source;
			this.Version = version;
		}
	}
}