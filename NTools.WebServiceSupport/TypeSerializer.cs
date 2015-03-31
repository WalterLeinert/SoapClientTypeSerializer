using System;
using System.Collections;
#if !NET_1
using System.Collections.Generic;
#endif
using System.IO;
using System.Web.Services.Protocols;
using System.Reflection;
using System.Runtime.Serialization;

using NTools.Core.Reflection;

namespace NTools.WebServiceSupport {

#if !NET_1

    //- Logging Namespaces ---------------------------------------------------

    using log4net.Core;
    using NTools.Logging.Log4Net;

    using TypeSerializerDictByType      = Dictionary<Type, TypeSerializer>;
    using TypeSerializerDictByObject    = Dictionary<object, TypeSerializer>;
    using FieldSerializerDict           = Dictionary<string, FieldSerializer>;
    using ConstructorReflectorDict      = Dictionary<string, ConstructorReflector>;
#else
    using TypeSerializerDictByType      = Hashtable;
    using TypeSerializerDictByObject    = Hashtable;
    using FieldSerializerDict           = Hashtable;
    using ConstructorReflectorDict      = Hashtable;

	interface ITraceLog {
	}

	[Serializable]
	sealed class TraceLogManager {
		public static ITraceLog GetLogger(Type type) {
			return null;
		}
	}

	[Serializable]
	public class EnterExitLogger : IDisposable {
		public EnterExitLogger(params object[] args) {
		}
		#region IDisposable Member

		public void Dispose() {
			// TODO:  Implementierung von EnterExitLogger.Dispose hinzuf�gen
		}

		#endregion
	}

	[Serializable]
	enum Level {
		Info
	}

	#endif

	/// <summary>
	/// Klasse f�r die Serialisierung von Objekten, die eigentlich nicht serialisierbar sind. 
	/// Die Serialisierung erfolgt �ber Reflection.
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
		/// Initialisiert eine neue Instanz der class <see cref="TypeSerializer"/> f�r das 
		/// nicht serialisierbare Objekt <paramref name="instance"/> f�r eine Serialisierung mittles
		/// Reflection.
		/// </summary>
		/// <param name="instance">The instance.</param>	
		private TypeSerializer(object instance) {
			using (EnterExitLogger log = new EnterExitLogger(s_log, "instance = {0}", instance)) {
				m_instance = instance;
				m_type = m_instance.GetType();
				m_fields = new FieldSerializerDict();
				m_constructors = new ConstructorReflectorDict();

				foreach (ConstructorInfo ci in m_type.GetConstructors(MemberReflector.AllInstanceDeclared)) {
					ConstructorReflector ctor = new ConstructorReflector(ci);
					m_constructors.Add(TypeReflector.BuildMethodSignature(ci), ctor);
					if (ctor.IsDefaultConstructor) {
						m_defaultConstructor = ctor;
					}
				}

				FieldInfo[] fields = m_type.GetFields(MemberReflector.AllInstanceDeclared);

				foreach (FieldInfo fi in fields) {
					object fieldValue = fi.GetValue(m_instance);
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

			return (ConstructorReflector) m_constructors[signature];
		}



		/// <summary>
		/// Erzeugt einen <see cref="TypeSerializer"/> f�r die Instanz <paramref name="instance"/>, falls
		/// diese nicht serialisierbar ist. Sonst wird einfach <paramref name="instance"/> zur�ckgeliefert.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <returns></returns>
		public static object CreateSerializerWrapper(object instance) {
			if (instance != null) {
				Type type = instance.GetType();
				if (type.IsArray) {
					//
					// Arrays
					//
					if (!IsSerializable(type.GetElementType())) {
						object[] elements = (object[])instance;
						TypeSerializer[] array = new TypeSerializer[elements.Length];
						if (elements.Length > 0) {
							for (int i = 0; i < elements.Length; i++) {
								array[i] = (TypeSerializer)TypeSerializer.CreateSerializerWrapper(elements[i]);
							}
						}

						instance = array;
					}
				} else if (type == typeof(Hashtable)) {
					//
					// Behandlung von Hashtables
					//

					Hashtable ht = new Hashtable();
					foreach (DictionaryEntry de in ((Hashtable)instance)) {
						object key = de.Key;
						object value = de.Value;

						key = TypeSerializer.CreateSerializerWrapper(key);
						value = TypeSerializer.CreateSerializerWrapper(value);

						ht.Add(key, value);
					}
					instance = ht;
				} else if (!IsSerializable(type)) {
					instance = TypeSerializer.CreateSerializer(instance);
				}
			}

			return instance;
		}

		/// <summary>
		/// Erzeugt einen <see cref="TypeSerializer"/> f�r die angegebene Instanz.
		/// Die Serializer werden gecached.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <returns>Den zugeh�rigen <see cref="TypeSerializer"/>.</returns>
		private static TypeSerializer CreateSerializer(object instance) {
			TypeSerializer serializer = null;

			if (!s_instances.ContainsKey(instance)) {
				serializer = new TypeSerializer(instance);
				s_instances.Add(instance, serializer);
			} else {
				serializer = (TypeSerializer) s_instances[instance];
			}

			return serializer;
		}


		/// <summary>
		/// Pr�ft, ob der angegebene Typ <paramref name="type"/> serializable ist.
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

			SerializableAttribute[] serializableAttributes = (SerializableAttribute[])type.GetCustomAttributes(typeof(SerializableAttribute), false);
			return (serializableAttributes.Length > 0);
		}

		/// <summary>
		/// Pr�ft, ob der angegebene Wert <paramref name="value"/> serialisierbar ist.
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
			using (EnterExitLogger log = new EnterExitLogger(s_log, Level.Info, "objectGraph = {0}, type = {1}", objectGraph,
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
			using (EnterExitLogger log = new EnterExitLogger(s_log, Level.Info)) {

				TypeSerializer typeSerializer = (TypeSerializer)formatter.Deserialize(stream);
				ConstructorInfo[] constructors = typeSerializer.m_type.GetConstructors(MemberReflector.AllInstanceDeclared);

				ConstructorReflector ctor = new ConstructorReflector(typeSerializer.m_type, new Type[] { typeof(Type) }, MemberReflector.AllInstanceDeclared);
				object rval = ctor.Invoke(new object[] { typeof(EmptyWebService) });

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
			using (EnterExitLogger log = new EnterExitLogger(s_log, "instance = {0}, type = {1}", instance, m_type)) {

				if (instance == null) {
					if (DefaultConstructor != null) {
						instance = DefaultConstructor.Invoke(null);
					} else {
						if (m_type.IsArray) {
							Type elementType = m_type.GetElementType();

							ConstructorReflector arrayCtor = GetConstructor(".ctor(System.Int32)");

							//
							// Dummy: Array mit einem Element anlegen
							//
							object[] array = (object[])arrayCtor.Invoke(new object[] { 1 });

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
							TypeReflector tr = new TypeReflector(typeof(DummyInfoWebService));
							MethodInfo mi = typeof(DummyInfoWebService).GetMethod("getString");

							ConstructorReflector rtmiCtor = new ConstructorReflector(Type.GetType("System.Reflection.RuntimeMethodInfo"));
							object rtmi = rtmiCtor.Invoke(null);

							string ctorSignature = TypeReflector.BuildMethodSignature(".ctor", new Type[] { Type.GetType("System.Reflection.MethodInfo") });
							ConstructorReflector ctor = GetConstructor(ctorSignature);
							instance = ctor.Invoke(new object[] { mi });
						} else {
							throw new InvalidOperationException(string.Format("Type {0} has no default constructor", m_type));
						}
					}
				}

				//
				// Deseralisierung der Member
				//
				foreach (FieldSerializer field in m_fields.Values) {
					object fieldValue = field.Value;

					TypeSerializer fieldSerializer = field.Value as TypeSerializer;

					if (fieldSerializer != null) {
						object instanceFieldValue = fieldSerializer.Deserialize();
						field.SetValue(instance, instanceFieldValue);

					} else if (field.Value != null) {
						if (field.Value is Hashtable) {
							Hashtable ht = new Hashtable();
							foreach (DictionaryEntry de in ((Hashtable)field.Value)) {
								object key = de.Key;
								object value = de.Value;

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
							object[] fieldValues = (object[])field.Value;

							ConstructorReflector ctor = new ConstructorReflector(field.Info.FieldType, new Type[] { typeof(int) }, MemberReflector.AllInstanceDeclared);

							object[] array = (object[])ctor.Invoke(new object[] { fieldValues.Length });
							for (int i = 0; i < fieldValues.Length; i++) {
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
		/// Dummy WebService-Proxy, der keine Methoden enth�lt und sehr schnell im Konstruktor von 
		/// SoapHttpClientProtocol bzw. WebClientProtocol intern analysiert werden kann.
		/// </summary>
		[System.Web.Services.WebServiceBindingAttribute(
			Name = "DummyWebServiceType",
		   Namespace = "http://www.themindelectric.com/wsdl/DummyWebServiceType/")]
		class EmptyWebService : SoapHttpClientProtocol {
		}
	}
}