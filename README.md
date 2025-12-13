# Plugin.ConfigurationHttp
[![Auto build](https://github.com/DKorablin/Plugin.ConfigurationHttp/actions/workflows/release.yml/badge.svg)](https://github.com/DKorablin/Plugin.ConfigurationHttp/releases/latest)

Lightweight configuration Web UI and IPC coordination plugin for SAL host applications (.NET Framework 4.8).

## Overview
Provides a self-hosted HTTP/HTTPS endpoint (default: 8180) exposing:
* Live inspection of loaded plugins (name, version, metadata)
* Grouped editable settings (with DisplayName, Description, DefaultValue, ReadOnly flags)
* Persistence of changed settings back to the host (saved per assembly)
* Push (web‑push) message dispatch to subscribed users

If multiple host processes run on the same machine the first process becomes the control host. Subsequent processes discover it and coordinate via a named pipe IPC channel, avoiding port collisions and enabling cross‑process plugin enumeration.

## Installation
To install the Configuration HTTP Plugin, follow these steps:
1. Download the latest release from the [Releases](https://github.com/DKorablin/Plugin.ReflectionSearch/releases)
2. Extract the downloaded ZIP file to a desired location.
3. Use the provided [Flatbed.Dialog (Lite)](https://dkorablin.github.io/Flatbed-Dialog-Lite) executable or download one of the supported host applications:
	- [Flatbed.Dialog](https://dkorablin.github.io/Flatbed-Dialog)
	- [Flatbed.MDI](https://dkorablin.github.io/Flatbed-MDI)
	- [Flatbed.MDI (WPF)](https://dkorablin.github.io/Flatbed-MDI-Avalon)
	- [Flatbed.WorkerService](https://dkorablin.github.io/Flatbed-WorkerService)
5. Grant permission for the application to use the specified HTTP/HTTPS port (or run the application in the Administrative mode):
	- Open Command Prompt as Administrator
	- Run the following command, replacing `{hostUrl}` with your configured host URL (e.g., `https://+:8180/`):
	  ```
	  netsh http add urlacl url={hostUrl} user={Environment.UserDomainName}\\{Environment.UserName}
	  ```
6. To use HTTPS, ensure a valid SSL certificate is bound to the specified port. You can use the following command to bind a certificate:
	```
	netsh http add sslcert ipport=[ipAddress]:8180 certhash=[thumbprint] appid={d10da6bc-77fd-4ada-8b3f-b850023e59ae}
	```
7. Launch the host application and optionally set Users[] and AuthenticationSchemes.

## Architecture
* Plugin.cs – Entry point implementing IPlugin & IPluginSettings. Creates and connects ServiceFactory (HTTP + IPC surfaces) using a resolved host URL (supports token substitution for local IP).
* PluginSettings – Strongly typed configuration (authentication schemes, users list, host URL template, push keys, etc.). Performs runtime formatting (e.g. replacing {ip} template) and authentication checks.
* IPC (ControlServiceProxy) – Named pipe WCF client facilitating: process discovery, connection, periodic Ping, graceful disconnect, and hosting of a Plugins IPC service for remote enumeration.
* Controllers – Build serialized DTOs (e.g. SettingsResponse) for the browser UI. Reflection extracts property metadata/attributes; TimeSpan values normalized to invariant strings.
* Static HTML/JS – Single‑page UI (Index.html) renders categories and settings; posts updates; collapsible groups; basic error reporting.
* Tracing – Custom TraceSource and WebPushTraceListener unify plugin log output with host listeners; Start/Warning events for lifecycle and faults.

## Settings Model
Each plugin supplying `IPluginSettings` is inspected. Property metadata used:
* [DisplayName], [Description], [DefaultValue], [ReadOnly]
* Type conversion via TypeDescriptor (supports primitives, TimeSpan, nullable TimeSpan)
Edited values are validated through type converters then persisted: `host.Plugins.Settings(plugin).SaveAssemblyParameter(name,value)`.

## Authentication
PluginSettings.Authenticate(principal) enforces schemes and optional internal allow‑list (Users[]). Anonymous or None schemes bypass checks. Caller identity must be authenticated when a scheme requires it.

## IPC Coordination
ControlServiceProxy manages a per‑process PluginsHost (named pipe). Sequence:
1. CreateClientHost() opens local service (PluginsIpcService) and connects to control host.
2. Ping() keeps the channel alive; handles pipe fault codes (e.g. 232 – broken pipe) gracefully.
3. DisconnectControlHost() closes channels; aborts local host on faults.

## Release & Packaging
GitHub Actions workflows:
* test.yml – Restore, test, build (unsigned) for PR validation.
* release.yml – Auto versioning, signed build, NuGet packaging, artifact signing, dependency injection, final per‑framework zip creation.
Packaging script (Build-ReleasePackage.ps1):
1. Fetch latest Flatbed.Dialog.Lite release.
2. Organize build output into /{framework}/Plugin.
3. Download matching dependency asset (redirect netstandard to net8.0‑windows asset when required).
4. Merge dependency files, produce versioned zip: {Solution}_v{Version}_{Framework}.zip.

## Developer Notes
* Host URL may contain a template constant (see `PluginSettings.Constants.TemplateIpAddr`) substituted with local IPv4.
* Add new settings by extending `PluginSettings`; mark with attributes for richer UI metadata.
* Trace: use Plugin.Trace.TraceEvent(type,id,message,...) for consistent output.
* Exceptions during property set are caught; inner exception message returned to UI.
* TimeSpan values: serialized invariant ("c" format) – ensure client supplies same format.

## Extensibility
* Add new controllers: follow existing pattern returning serializable POCO/DTO objects.
* Inject custom authentication: extend Authenticate or front the HTTP host with a reverse proxy terminating TLS + auth.
* Replace UI: serve alternative static assets or integrate Razor/SPA; keep contract of existing JSON endpoints.

## Security Considerations
* HTTPS recommended (register certificate; see AssemblyInfo comment for netsh binding sample).
* Limit Users[] and avoid Anonymous where sensitive configuration exists.
* Protect web‑push VAPID keys; stored in settings (trimmed on assignment).
* Named pipes are local only; still validate process IDs if adding privileged operations.

---
For additional frameworks or custom build targets extend the release script and workflows accordingly.
