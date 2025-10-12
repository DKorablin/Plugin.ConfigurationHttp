# Plugin.ConfigurationHttp
[![Auto build](https://github.com/DKorablin/Plugin.ConfigurationHttp/actions/workflows/release.yml/badge.svg)](https://github.com/DKorablin/Plugin.ConfigurationHttp/releases/latest)

A .NET Framework 4.8 configuration plugin that hosts a lightweight HTTP/HTTPS server (default port 8180) to inspect and change runtime settings of a SAL-based host without restart.
Multiple local process instances coordinate via a named pipe for discovery and port conflict resolution.

By default HTTP(s) server is starting at 8180 port and using IPC named pipe to find another instances of the host application (if any).
If existing port is in use the plugin will try to find existing plugin instance in the different process using named pipe.