using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Services;
using System.Web.Services.Protocols;
using NTools.Core.Reflection;

namespace NTools.WebServiceSupport {
    using TypeList      = List<Type>;
    using NTools.Logging.Log4Net;


	public sealed class SoapClientTypeSerializer {
        private static readonly ITraceLog s_log = TraceLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		
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
			var filePath = Path.GetDirectoryName(webServiceType.Assembly.Location);
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
			var exportedTypes = assembly.GetExportedTypes();
			var webServiceTypes = new TypeList();

			foreach (var type in exportedTypes) {
				if (type.IsAbstract) {
					continue;
				}

				var serviceBindingAttributes = (WebServiceBindingAttribute[])type.GetCustomAttributes(typeof(WebServiceBindingAttribute), false);
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
			var webServiceTypes = GetWebserviceTypes(assembly);

			foreach (var webServiceType in webServiceTypes) {
				SerializeClientType(webServiceType);
			}
		}


		/// <summary>
		/// Serializes the type of the client.
		/// </summary>
		/// <param name="webServiceType">Type of the web service.</param>
		public static void SerializeClientType(Type webServiceType) {
			IFormatter formatter = new BinaryFormatter();

			using (var stream = new FileStream(GetSerializerPath(webServiceType), FileMode.OpenOrCreate)) {

				//
				// WebService-Proxy instantiieren -> SoapClientType wird über Reflection erzeugt
				//
				var ce = new ConstructorEventArgs(webServiceType);
				if (Constructing != null) {
					Constructing(null, ce);
				}
				var ctor = new ConstructorReflector(webServiceType, ce.Types, MemberReflector.AllInstanceDeclared);
				var webServiceInstance = ctor.Invoke(ce.Args);

				//
				// SoapClientType-Field ermitteln und serialisieren
				//
				var reflectedClientType = s_clientTypeReflector.GetValue(webServiceInstance);

				var serializer = (TypeSerializer)TypeSerializer.CreateSerializerWrapper(reflectedClientType);

				TypeSerializer.Serialize(formatter, stream, serializer);
			}
		}


		/// <summary>
		/// Deserializes alle WebService-Proxytypen des Assemblies <paramref name="assembly"/>.
		/// </summary>
		/// <param name="assembly">Die Assembly mit den WebService-Proxyklassen.</param>
		public static void DeserializeClientTypes(Assembly assembly) {
			var webServiceTypes = GetWebserviceTypes(assembly);

			foreach (var webServiceType in webServiceTypes) {
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
				var filename = GetSerializerPath(webServiceType);

				if (File.Exists(filename)) {
					var formatter = new BinaryFormatter();

					var webClientProtocolReflector = new TypeReflector(typeof(WebClientProtocol));
					var cache = webClientProtocolReflector.GetField("cache");
					var cacheReflector = new TypeReflector(cache.GetType());
					var cacheHashtable = (Hashtable)cacheReflector.GetField(cache, "cache");

					object deserializedClientType = null;
					using (var stream = new FileStream(filename, FileMode.Open)) {
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
			var cachedClientType = s_webClientProtocolReflector.Invoke("GetFromCache(System.Type)", new object[] { webServiceType });
			return (cachedClientType != null);
		}

		public static event EventHandler<ConstructorEventArgs> Constructing;
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
