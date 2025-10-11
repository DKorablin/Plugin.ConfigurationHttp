# Plugin.ConfigurationHttp
Configuration plugin using built in HTTP(s) server to configure service host applications without stopping it.
To interact between different instances of the host application on the same machine plugin uses named pipes.

By default HTTP(s) server is starting at 8180 port and using IPC named pipe to find another instances of the host application (if any).
If existing port is in use the plugin will try to find existing plugin instance in the different process using named pipe.