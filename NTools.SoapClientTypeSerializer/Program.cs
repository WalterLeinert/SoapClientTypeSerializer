using NTools.Logging.Log4Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace NTools.WebServiceSupport.Tools {

	class Program {
        private static readonly ITraceLog s_log = TraceLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static readonly List<Type> s_constructorTypes = new List<Type>();
		private static string s_modelDirectory;

		/// <summary>
		/// Entry point of the application.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <remarks>
		/// args[0]: assembly containing the web service proxies.
		/// args[1]: web service proxy constructor types (type names)
		/// </remarks>
		static void Main(string[] args) {
            using (var log = new EnterExitLogger(s_log)) {
#if (false)
			Type[] exportedTypes = typeof(AkteWebService).Assembly.GetExportedTypes();

			List<Type> webServiceTypes = new List<Type>();

			foreach (Type type in exportedTypes) {
				if (type.IsAbstract) {
					continue;
				}

				WebServiceBindingAttribute[] serviceBindingAttributes = (WebServiceBindingAttribute[])type.GetCustomAttributes(typeof(WebServiceBindingAttribute), false);
				if (serviceBindingAttributes != null && serviceBindingAttributes.Length == 1) {
					webServiceTypes.Add(type);
				}
			}

			foreach (Type webServiceType in webServiceTypes) {
				SoapClientTypeSerializer.SerializeClientType(webServiceType);
			}
#endif

                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;

                var assembly = Assembly.LoadFrom(args[0]);

                s_modelDirectory = Path.GetDirectoryName(assembly.Location);

                var serializerFileName = assembly.Location;
                serializerFileName = Path.ChangeExtension(serializerFileName, ".XmlSerializers.dll");

                if (File.Exists(serializerFileName)) {
                    var serializerAssembly = Assembly.LoadFrom(serializerFileName);
                }


                if (args.Length > 1) {
                    var constructorTypes = args[1].Split(new char[] { ';' });

                    foreach (var constructorType in constructorTypes) {
                        var typeParts = constructorType.Split(new char[] { ',' });

                        Type type;
                        if (typeParts.Length > 1) {
                            var asmName = typeParts[1].Trim();
                            var asmPath = Path.Combine(s_modelDirectory, asmName);
                            asmPath = Path.ChangeExtension(asmPath, ".dll");

                            //Assembly asm = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(assembly.Location), "ObjectManagement.dll"));
                            var asm = Assembly.LoadFrom(asmPath);
                            type = asm.GetType(typeParts[0]);
                        } else {
                            type = Type.GetType(constructorType);
                        }

                        if (type != null) {
                            s_constructorTypes.Add(type);
                        }
                    }
                }

                var currentDirectory = Environment.CurrentDirectory;

                try {
                    Environment.CurrentDirectory = Path.GetDirectoryName(assembly.Location);
                    SoapClientTypeSerializer.Constructing += new EventHandler<ConstructorEventArgs>(SoapClientTypeSerializerConstructing);
                    SoapClientTypeSerializer.SerializeClientTypes(assembly);
                } finally {
                    Environment.CurrentDirectory = currentDirectory;
                }
            }
		}

		static void SoapClientTypeSerializerConstructing(object sender, ConstructorEventArgs e) {
			//if (e.Type == typeof(object)) {
			e.Args = new object[] { null };
			if (s_constructorTypes.Count > 0) {
				e.Types = s_constructorTypes.ToArray();
			}
			//}
		}

		private static Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args) {
			Assembly assembly = null;

			var parts = args.Name.Split(',');
			var assemblyPath = Path.ChangeExtension(Path.Combine(s_modelDirectory, parts[0]), ".dll");

			if (File.Exists(assemblyPath)) {
				assembly = Assembly.LoadFrom(assemblyPath);
			}
			return assembly;
		}

	}


	class Tester {

		private static void Main(string[] args) {
			var assembly = Assembly.LoadFrom(args[0]);
			SoapClientTypeSerializer.DeserializeClientTypes(assembly);
		}
	}
}
