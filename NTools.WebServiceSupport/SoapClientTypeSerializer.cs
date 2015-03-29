using System;
using System.Collections;
#if !NET_1
using System.Collections.Generic;
#endif
using System.Web.Services.Protocols;
using System.Reflection;
using System.IO;
using System.Web.Services;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using NTools.Core.Reflection;

namespace NTools.WebServiceSupport {
#if !NET_1
    using TypeList      = List<Type>;
#else
    using TypeList      = ArrayList;
#endif


	public sealed class SoapClientTypeSerializer {
		
		/// <summary>
		/// Die Fileextension für die serialisierten SoapClientTypes.
		/// </summary>
		public const string SerializerExtension = "SoapClientType";

		public static FieldReflector s_clientTypeReflector = new FieldReflector(typeof(SoapHttpClientProtocol), "clientType", MemberReflector.PrivateInstanceDeclared);
		public static TypeReflector s_webClientProtocolReflector = new TypeReflector(typeof(WebClientProtocol));


		/// <summary>
		/// Liefert für den WebService-Proxytyp <paramref name="webServiceType"/> den Pfad auf den serialisierten SoapClientType.
		/// </summary>
		/// <param name="webServiceType">WebService-Proxytyp.</param>
		/// <returns>Pfad auf den serialisierten SoapClientType.</returns>
		/// <remarks>
		/// Die serialisierten SoapClientTyps liegen in demselben Verzeichnis wie die Assembly.
		/// Der Name enthält die Assembly-Versionsnummer des zugehörigen Assemblies: Service-2.0.0.0.SoapClientType
		/// </remarks>
		private static string GetSerializerPath(Type webServiceType) {
			string filePath = Path.GetDirectoryName(webServiceType.Assembly.Location);
			return Path.Combine(filePath, Path.ChangeExtension(webServiceType.Name,
				"-" + webServiceType.Assembly.GetName().Version.ToString() + 
				"." + SerializerExtension));
		}


		/// <summary>
		/// Liefert die WebService-Proxytypen des Assemblies <paramref name="assembly"/>.
		/// </summary>
		/// <param name="assembly">Die Assembly mit den WebService-Proxyklassen.</param>
		/// <returns> WebService-Proxytypen.</returns>
		private static TypeList GetWebserviceTypes(Assembly assembly) {
			Type[] exportedTypes = assembly.GetExportedTypes();
			TypeList webServiceTypes = new TypeList();

			foreach (Type type in exportedTypes) {
				if (type.IsAbstract) {
					continue;
				}

				WebServiceBindingAttribute[] serviceBindingAttributes = (WebServiceBindingAttribute[])type.GetCustomAttributes(typeof(WebServiceBindingAttribute), false);
				if (serviceBindingAttributes != null && serviceBindingAttributes.Length == 1) {
					webServiceTypes.Add(type);
				}
			}

			return webServiceTypes;
		}


		/// <summary>
		/// Serialisiert für alle WebService-Proxyklassen die internen SoapClientType-Instanzen in binäre Files.
		/// </summary>
		/// <param name="assembly">Die Assembly mit den WebService-Proxyklassen.</param>
		public static void SerializeClientTypes(Assembly assembly) {
			TypeList webServiceTypes = GetWebserviceTypes(assembly);

			foreach (Type webServiceType in webServiceTypes) {
				SerializeClientType(webServiceType);
			}
		}


		/// <summary>
		/// Serializes the type of the client.
		/// </summary>
		/// <param name="webServiceType">Type of the web service.</param>
		public static void SerializeClientType(Type webServiceType) {
			IFormatter formatter = new BinaryFormatter();

			using (FileStream stream = new FileStream(GetSerializerPath(webServiceType), FileMode.OpenOrCreate)) {

				//
				// WebService-Proxy instantiieren -> SoapClientType wird über Reflection erzeugt
				//
				ConstructorEventArgs ce = new ConstructorEventArgs(webServiceType);
				if (Constructing != null) {
					Constructing(null, ce);
				}
				ConstructorReflector ctor = new ConstructorReflector(webServiceType, ce.Types, MemberReflector.AllInstanceDeclared);
				object webServiceInstance = ctor.Invoke(ce.Args);

				//
				// SoapClientType-Field ermitteln und serialisieren
				//
				object reflectedClientType = s_clientTypeReflector.GetValue(webServiceInstance);

				TypeSerializer serializer = (TypeSerializer)TypeSerializer.CreateSerializerWrapper(reflectedClientType);

				TypeSerializer.Serialize(formatter, stream, serializer);
			}
		}


		/// <summary>
		/// Deserializes alle WebService-Proxytypen des Assemblies <paramref name="assembly"/>.
		/// </summary>
		/// <param name="assembly">Die Assembly mit den WebService-Proxyklassen.</param>
		public static void DeserializeClientTypes(Assembly assembly) {
			TypeList webServiceTypes = GetWebserviceTypes(assembly);

			foreach (Type webServiceType in webServiceTypes) {
				DeserializeClientType(webServiceType);
			}			
		}


		/// <summary>
		/// Deserialisiert für den WebService-Proxytyp <paramref name="webServiceType"/> den zugehörigen SoapClientType und
		/// registriert diesen im statischen Cache von <see cref="WebClientProtocol"/>.
		/// </summary>
		/// <param name="webServiceType">Der WebService-Proxytyp.</param>
		public static void DeserializeClientType(Type webServiceType) {
			if (!IsCached(webServiceType)) {
				string filename = GetSerializerPath(webServiceType);

				if (File.Exists(filename)) {
					BinaryFormatter formatter = new BinaryFormatter();

					TypeReflector webClientProtocolReflector = new TypeReflector(typeof(WebClientProtocol));
					object cache = webClientProtocolReflector.GetField("cache");
					TypeReflector cacheReflector = new TypeReflector(cache.GetType());
					Hashtable cacheHashtable = (Hashtable)cacheReflector.GetField(cache, "cache");

					object deserializedClientType = null;
					using (FileStream stream = new FileStream(filename, FileMode.Open)) {
						deserializedClientType = TypeSerializer.Deserialize(formatter, stream);
						webClientProtocolReflector.Invoke("AddToCache(System.Type, System.Object)", new object[] { webServiceType, deserializedClientType });
					}
				}
			}
		}


		/// <summary>
		/// Liefert <c>true</c>, falls der angegebene Webservice Typ bereits im Cache von <see cref="WebClientProtocol"/> vorliegt.
		/// </summary>
		/// <param name="webServiceType">Der WebService-Proxytyp.</param>
		/// <returns>
		/// <c>true</c> falls der angegebene Webservice Typ bereits im Cache von <see cref="WebClientProtocol"/> vorliegt;
		/// sonst <c>false</c>.
		/// </returns>
		public static bool IsCached(Type webServiceType) {
			object cachedClientType = s_webClientProtocolReflector.Invoke("GetFromCache(System.Type)", new object[] { webServiceType });
			return (cachedClientType != null);
		}

#if !NET_1
		public static event EventHandler<ConstructorEventArgs> Constructing;
#else
		public static event ConstructorEventHandler Constructing;
#endif
	}

	public delegate void ConstructorEventHandler(object sender, ConstructorEventArgs e);


	public class ConstructorEventArgs : EventArgs {
		private readonly Type m_type;
		private Type[] m_types;
		private object[] m_args;

		public ConstructorEventArgs(Type type) {
			m_type = type;
			m_types = new Type[0];
		}

		public Type Type {
			get { return m_type; }
		}

		public object[] Args {
			get { return m_args; }
			set { m_args = value; }
		}

		public Type[] Types {
			get { return m_types; }
			set { m_types = value; }
		}
	}
}
