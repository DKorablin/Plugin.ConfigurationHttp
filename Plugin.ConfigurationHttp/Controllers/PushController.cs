using System;

namespace Plugin.ConfigurationHttp.Push
{
	internal class PushController
	{
		private readonly Plugin _plugin;

		public PushController(Plugin plugin)
			=> this._plugin = plugin;

		/// <summary>Save data for sending WebPUSH messages to the user</summary>
		/// <param name="endpoint">The endpoint takes the form of a custom URL pointing to a push server, which can be used to send a push message to the particular service worker instance that subscribed to the push service</param>
		/// <param name="p256dh">
		/// An Elliptic curve Diffie–Hellman public key on the P-256 curve (that is, the NIST secp256r1 elliptic curve).
		/// The resulting key is an uncompressed point in ANSI X9.62 format.
		/// </param>
		/// <param name="auth">An authentication secret, as described in Message Encryption for Web Push</param>
		public void Subscribe(String endpoint, String p256dh, String auth)
		{
			this._plugin.Settings.WebPushJson = Serializer.JavaScriptSerialize(new PluginSettings.PushSettings(endpoint, p256dh, auth));

			String title = "Subscribed";
			String description = String.Format("You successfully subscribed for HTTP PUSH messages on server: {0}", Environment.MachineName);

			this._plugin.Settings.SendPushMessage(title, description);
		}

		/// <summary>Delete data for sending WebPUSH messages to a user</summary>
		public void Unsubscribe()
			=> this._plugin.Settings.WebPushJson = null;
	}
}