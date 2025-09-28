using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using SAL.Flatbed;

namespace Plugin.ConfigurationHttp
{
	internal static class Utils
	{
		/// <summary>Checking for a fatal exception after which further code execution is impossible</summary>
		/// <param name="exception">The exception for verification</param>
		/// <returns>The exception is fatal</returns>
		public static Boolean IsFatal(Exception exception)
		{
			while(exception != null)
			{
				if((exception is OutOfMemoryException && !(exception is InsufficientMemoryException)) || exception is ThreadAbortException || exception is AccessViolationException || exception is SEHException)
					return true;
				if(!(exception is TypeInitializationException) && !(exception is TargetInvocationException))
					break;
				exception = exception.InnerException;
			}
			return false;
		}

		public static UInt32[] BitToInt(params Boolean[] bits)
		{
			UInt32[] result = new UInt32[] { };
			Int32 counter = 0;
			for(Int32 loop = 0; loop < bits.Length; loop++)
			{
				if(result.Length <= loop)//Increase the array by one if the value does not fit.
					Array.Resize<UInt32>(ref result, result.Length + 1);

				for(Int32 innerLoop = 0; innerLoop < 32; innerLoop++)
				{
					result[loop] |= Convert.ToUInt32(bits[counter++]) << innerLoop;
					if(counter >= bits.Length)
						break;
				}
				if(counter >= bits.Length)
					break;
			}
			return result;
		}

		public static void GenerateVapidKeys(out String publicKey, out String privateKey)
		{
			// Get the P-256 elliptic curve
			X9ECParameters ecParameters = ECNamedCurveTable.GetByName("prime256v1");
			ECDomainParameters domainParameters = new ECDomainParameters(
				ecParameters.Curve,
				ecParameters.G,
				ecParameters.N,
				ecParameters.H,
				ecParameters.GetSeed());

			// Create an EC key pair generator
			ECKeyPairGenerator keyPairGenerator = new ECKeyPairGenerator();
			keyPairGenerator.Init(new ECKeyGenerationParameters(domainParameters, new SecureRandom()));

			// Generate the key pair
			var keyPair = keyPairGenerator.GenerateKeyPair();

			// Extract public and private keys
			ECPublicKeyParameters ecPublicKey = (ECPublicKeyParameters)keyPair.Public;
			ECPrivateKeyParameters ecPrivateKey = (ECPrivateKeyParameters)keyPair.Private;

			// Encode keys to URL-safe Base64
			publicKey = UrlSafeBase64Encode(ecPublicKey.Q.GetEncoded(false));
			privateKey = UrlSafeBase64Encode(ecPrivateKey.D.ToByteArrayUnsigned());

			String UrlSafeBase64Encode(Byte[] bytes)
				=> Convert.ToBase64String(bytes)
					.TrimEnd('=')
					.Replace('+', '-')
					.Replace('/', '_');
		}

		#region Search
		/// <summary>Get a list of search parameters in the plugin</summary>
		/// <param name="plugin">A plugin instance for returning search strings</param>
		/// <returns>Search strings found in the plugin</returns>
		public static IEnumerable<String> GetPluginSearchMembers(IPluginDescription plugin)
		{
			foreach(String value in SearchProperties(plugin, false))
				yield return value;

			if(plugin.Instance is IPluginSettings settings)
				foreach(String value in SearchProperties(settings.Settings, true))
					yield return value;
		}

		/// <summary>Search by properties of an object instance</summary>
		/// <param name="instance">The object whose properties to search by</param>
		/// <param name="searchAttributes">Search by attributes of each property</param>
		/// <returns>Search strings found in object instance properties</returns>
		private static IEnumerable<String> SearchProperties(Object instance, Boolean searchAttributes)
		{
			PropertyInfo[] properties = instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy);
			foreach(PropertyInfo property in properties)
			{
				yield return property.Name;

				if(searchAttributes)
				{//We search by all attributes so as not to hardcode specific attributes
					Object[] attributes = property.GetCustomAttributes(false);
					if(attributes != null)
						foreach(Object attribute in attributes)
							foreach(String value in Utils.SearchProperties(attribute, false))
								yield return value;
				}

				if(property.CanRead
					&& property.GetIndexParameters().Length == 0
					&& Array.Exists<Type>(property.PropertyType.GetInterfaces(), p => p == typeof(IComparable)))
				{
					Object value = property.GetValue(instance, null);
					if(value != null)
						yield return value.ToString();
				}
			}
		}
		#endregion Search
	}
}