using System.Reflection;
using System.Runtime.InteropServices;

[assembly: Guid("d10da6bc-77fd-4ada-8b3f-b850023e59ae")]
[assembly: System.CLSCompliant(true)]

#if NETCOREAPP
[assembly: AssemblyMetadata("ProjectUrl", "https://github.com/DKorablin/Plugin.ConfigurationHttp")]
#else

[assembly: AssemblyDescription("Web UI for Plugins configuration")]
[assembly: AssemblyCopyright("Copyright © Danila Korablin 2019-2025")]

#endif
/*
Register SSL Certificate:
certhash -> thumbprint
appid -> Assembly:GuidAttribute
	netsh http add sslcert ipport=192.168.58.46:8180 certhash=8ede92fb1415e7a975ea20ec26f9466fe7875198 appid={d10da6bc-77fd-4ada-8b3f-b850023e59ae}

*/