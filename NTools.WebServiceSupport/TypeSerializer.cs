using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Web.Services;
using System.Web.Services.Protocols;
using log4net.Core;
using NTools.Core.Reflection;
using NTools.Logging.Log4Net;

namespace NTools.WebServiceSupport {

    //- Logging Namespaces ---------------------------------------------------

	using TypeSerializerDictByType      = Dictionary<Type, TypeSerializer>;
    using TypeSerializerDictByObject    = Dictionary<object, TypeSerializer>;
    using FieldSerializerDict           = Dictionary<string, FieldSerializer>;
    using ConstructorReflectorDict      = Dictionary<string, ConstructorReflector>;


	/// <summary>
	/// Klasse für die Serialisierung von Objekten, die eigentlich nicht serialisierbar sind. 
	/// Die Serialisierung erfolgt über Reflection.
	/// </summary>
	[Serializable]
	public class TypeSerializer {
		#region nicht serialisierte Member
		[NonSerialized]
		private static readonly ITraceLog s_log = TraceLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[NonSerialized]
        private static TypeSerializerDictByType s_typeSerializers = new TypeSerializerDictByType( );

		[NonSerialized]
        private static readonly TypeSerializerDictByObject s_instances = new TypeSerializerDictByObject( );

		[NonSerialized]
		private readonly object m_instance;
		#endregion

		private readonly Type m_type;
        private readonly FieldSerializerDict m_fields;

		//[NonSerialized]
		private readonly ConstructorReflectorDict m_constructors;

		//[NonSerialized]
		private readonly ConstructorReflector m_defaultConstructor;


		/// <summary>
		/// Initialisiert eine neue Instanz der class <see cref="TypeSerializer"/> für das 
		/// nicht serialisierbare Objekt <paramref name="instance"/> für eine Serialisierung mittles
		/// Reflection.
		/// </summary>
		/// <param name="instance">The instance.</param>	
		private TypeSerializer(object instance) {
			using (var log = new EnterExitLogger(s_log, "instance = {0}", instance)) {
				m_instance = instance;
				m_type = m_instance.GetType();
				m_fields = new FieldSerializerDict();
				m_constructors = new ConstructorReflectorDict();

				foreach (var ci in m_type.GetConstructors(MemberReflector.AllInstanceDeclared)) {
					var ctor = new ConstructorReflector(ci);
					m_constructors.Add(TypeReflector.BuildMethodSignature(ci), ctor);
					if (ctor.IsDefaultConstructor) {
						m_defaultConstructor = ctor;
					}
				}

				var fields = m_type.GetFields(MemberReflector.AllInstanceDeclared);

				foreach (var fi in fields) {
					var fieldValue = fi.GetValue(m_instance);
					fieldValue = CreateSerializerWrapper(fieldValue);

					m_fields.Add(fi.Name, new FieldSerializer(fi, fieldValue));
				}
			}
		}

		/// <summary>
		/// Liefert den Default Konstruktor oder <c>null</c>.
		/// </summary>
		/// <value>The default constructor.</value>
		public ConstructorReflector DefaultConstructor {
			get { return m_defaultConstructor; }
		}


		/// <summary>
		/// Liefert einen <see cref="ConstructorReflector"/> mit der Signatur <paramref name="signature"/>.
		/// </summary>
		/// <param name="signature">The signature.</param>
		/// <returns></returns>
		public ConstructorReflector GetConstructor(string signature) {
			if (!m_constructors.ContainsKey(signature)) {
				throw new ArgumentException(string.Format("Type {0} has no such constructor.", m_type), "name");
			}

			return m_constructors[signature];
		}



		/// <summary>
		/// Erzeugt einen <see cref="TypeSerializer"/> für die Instanz <paramref name="instance"/>, falls
		/// diese nicht serialisierbar ist. Sonst wird einfach <paramref name="instance"/> zurückgeliefert.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <returns></returns>
		public static object CreateSerializerWrapper(object instance) {
			if (instance != null) {
				var type = instance.GetType();
				if (type.IsArray) {
					//
					// Arrays
					//
					if (!IsSerializable(type.GetElementType())) {
						var elements = (object[])instance;
						var array = new TypeSerializer[elements.Length];
						if (elements.Length > 0) {
							for (var i = 0; i < elements.Length; i++) {
								array[i] = (TypeSerializer)CreateSerializerWrapper(elements[i]);
							}
						}

						instance = array;
					}
				} else if (type == typeof(Hashtable)) {
					//
					// Behandlung von Hashtables
					//

					var ht = new Hashtable();
					foreach (DictionaryEntry de in ((Hashtable)instance)) {
						var key = de.Key;
						var value = de.Value;

						key = CreateSerializerWrapper(key);
						value = CreateSerializerWrapper(value);

						ht.Add(key, value);
					}
					instance = ht;
				} else if (!IsSerializable(type)) {
					instance = CreateSerializer(instance);
				}
			}

			return instance;
		}

		/// <summary>
		/// Erzeugt einen <see cref="TypeSerializer"/> für die angegebene Instanz.
		/// Die Serializer werden gecached.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <returns>Den zugehörigen <see cref="TypeSerializer"/>.</returns>
		private static TypeSerializer CreateSerializer(object instance) {
			TypeSerializer serializer = null;

			if (!s_instances.ContainsKey(instance)) {
				serializer = new TypeSerializer(instance);
				s_instances.Add(instance, serializer);
			} else {
				serializer = s_instances[instance];
			}

			return serializer;
		}


		/// <summary>
		/// Prüft, ob der angegebene Typ <paramref name="type"/> serializable ist.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>
		/// 	<c>true</c> falls die angegebenen type is serializable; sonst <c>false</c>.
		/// </returns>
		public static bool IsSerializable(Type type) {
			if (type.IsArray) {
				return IsSerializable(type.GetElementType());
			}

			if (type.IsSerializable) {
				return true;
			}

			var serializableAttributes = (SerializableAttribute[])type.GetCustomAttributes(typeof(SerializableAttribute), false);
			return (serializableAttributes.Length > 0);
		}

		/// <summary>
		/// Prüft, ob der angegebene Wert <paramref name="value"/> serialisierbar ist.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>
		/// 	<c>true</c> falls die angegebenen value is serializable; sonst <c>false</c>.
		/// </returns>
		public static bool IsSerializable(object value) {
			if (value != null) {
				return IsSerializable(value.GetType());
			}
			return true;
		}



		/// <summary>
		/// Serialisiert den Objektgraph <param name="objectGraph"/> mit Hilfe des
		/// angegebenen Formatters <paramref name="formatter"/> auf den Ausgabe-Stream <paramref name="stream"/>.
		/// </summary>
		/// <param name="formatter">The formatter.</param>
		/// <param name="stream">The stream.</param>
		/// <param name="objectGraph">The object graph.</param>
		public static void Serialize(IFormatter formatter, Stream stream, object objectGraph) {
			using (var log = new EnterExitLogger(s_log, Level.Info, "objectGraph = {0}, type = {1}", objectGraph,
				objectGraph as TypeSerializer != null ? ((TypeSerializer) objectGraph).m_type.FullName : "-")) {
				formatter.Serialize(stream, objectGraph);
			}
		}


		/// <summary>
		/// Deserialisiert einen WebService-Proxytyp (SoapHttpClientProtocol) mit Hilfe des angegebenen 
		/// Formatters <paramref name="formatter"/> vom Eingabe-Stream <paramref name="stream"/>.
		/// </summary>
		/// <param name="formatter">The formatter.</param>
		/// <param name="stream">The stream.</param>
		/// <param name="webServiceType">Type of the web service.</param>
		/// <returns></returns>
		public static object Deserialize(IFormatter formatter, Stream stream) {
			using (var log = new EnterExitLogger(s_log, Level.Info)) {

				var typeSerializer = (TypeSerializer)formatter.Deserialize(stream);
				var constructors = typeSerializer.m_type.GetConstructors(MemberReflector.AllInstanceDeclared);

				var ctor = new ConstructorReflector(typeSerializer.m_type, new Type[] { typeof(Type) }, MemberReflector.AllInstanceDeclared);
				var rval = ctor.Invoke(new object[] { typeof(EmptyWebService) });

				typeSerializer.Deserialize(rval);

				return rval;
			}
		}

		/// <summary>
		/// Deserializes die aktuelle Instanz.
		/// </summary>
		/// <returns></returns>
		private object Deserialize() {
			return Deserialize(null);
		}


		/// <summary>
		/// Deserializes die angegebenen instance.
		/// </summary>
		/// <param name="instance">The instance.</param>
		private object Deserialize(object instance) {
			using (var log = new EnterExitLogger(s_log, "instance = {0}, type = {1}", instance, m_type)) {

				if (instance == null) {
					if (DefaultConstructor != null) {
						instance = DefaultConstructor.Invoke(null);
					} else {
						if (m_type.IsArray) {
							var elementType = m_type.GetElementType();

							var arrayCtor = GetConstructor(".ctor(System.Int32)");

							//
							// Dummy: Array mit einem Element anlegen
							//
							var array = (object[])arrayCtor.Invoke(new object[] { 1 });

							if (!elementType.IsAbstract) {
								//ConstructorReflector elCtor = new ConstructorReflector(elementType, MemberReflector.AllInstanceDeclared);
								//object element = elCtor.Invoke(null);
								//array[0] = element;
							}

							instance = array;

						} else if (m_type == typeof(LogicalMethodInfo)) {

							//
							// Achtung: Spezialbehandlung, da die Klasse LogicalMethodInfo nur einen Konstruktor
							// mit einem MethodInfo-Parameter hat! -> wir erzeugen einen Dummy-Konstruktorparameter;
							// die konkreten Member werden bei der Deserialisierung gesetzt.
							//
							var tr = new TypeReflector(typeof(DummyInfoWebService));
							var mi = typeof(DummyInfoWebService).GetMethod("getString");

							var rtmiCtor = new ConstructorReflector(Type.GetType("System.Reflection.RuntimeMethodInfo"));
							var rtmi = rtmiCtor.Invoke(null);

							var ctorSignature = TypeReflector.BuildMethodSignature(".ctor", new Type[] { Type.GetType("System.Reflection.MethodInfo") });
							var ctor = GetConstructor(ctorSignature);
							instance = ctor.Invoke(new object[] { mi });
						} else {
							throw new InvalidOperationException(string.Format("Type {0} has no default constructor", m_type));
						}
					}
				}

				//
				// Deseralisierung der Member
				//
				foreach (var field in m_fields.Values) {
					var fieldValue = field.Value;

					var fieldSerializer = field.Value as TypeSerializer;

					if (fieldSerializer != null) {
						var instanceFieldValue = fieldSerializer.Deserialize();
						field.SetValue(instance, instanceFieldValue);

					} else if (field.Value != null) {
						if (field.Value is Hashtable) {
							var ht = new Hashtable();
							foreach (DictionaryEntry de in ((Hashtable)field.Value)) {
								var key = de.Key;
								var value = de.Value;

								if (key is TypeSerializer) {
									key = ((TypeSerializer)key).Deserialize();
								}

								if (value is TypeSerializer) {
									value = ((TypeSerializer)value).Deserialize();
								}

								ht.Add(key, value);
							}

							field.SetValue(instance, ht);

						} else if (field.Info.FieldType.IsArray) {
							var fieldValues = (object[])field.Value;

							var ctor = new ConstructorReflector(field.Info.FieldType, new Type[] { typeof(int) }, MemberReflector.AllInstanceDeclared);

							var array = (object[])ctor.Invoke(new object[] { fieldValues.Length });
							for (var i = 0; i < fieldValues.Length; i++) {
								if (fieldValues[i] is TypeSerializer) {
									array[i] = ((TypeSerializer)fieldValues[i]).Deserialize();
								} else {
									array[i] = fieldValues[i];
								}
							}

							field.SetValue(instance, array);
						} else {
							// regular field
							field.SetValue(instance, field.Value);
						}
					}
				}

				return instance;
			}
		}



		/// <summary>
		/// Dummy WebService-Proxy, der keine Methoden enthält und sehr schnell im Konstruktor von 
		/// SoapHttpClientProtocol bzw. WebClientProtocol intern analysiert werden kann.
		/// </summary>
		[WebServiceBinding(
			Name = "DummyWebServiceType",
		   Namespace = "http://www.themindelectric.com/wsdl/DummyWebServiceType/")]
		class EmptyWebService : SoapHttpClientProtocol {
		}
	}
}
