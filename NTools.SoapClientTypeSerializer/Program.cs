using System.Linq.Expressions;
using log4net.Core;
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
		    using (var log = new EnterExitLogger(s_log, Level.Info)) {
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

		        try {

		            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;

		            var assembly = Assembly.LoadFile(args[0]);

		            s_modelDirectory = Path.GetDirectoryName(assembly.Location);

		            var serializerFileName = assembly.Location;
		            serializerFileName = Path.ChangeExtension(serializerFileName, ".XmlSerializers.dll");

		            if (File.Exists(serializerFileName)) {
		                var serializerAssembly = Assembly.LoadFile(serializerFileName);
		            }


		            if (args.Length > 1) {
		                var constructorTypes = args[1].Split(new char[] {';'});

		                foreach (var constructorType in constructorTypes) {
		                    var typeParts = constructorType.Split(new char[] {','});

		                    Type type;
		                    if (typeParts.Length > 1) {
		                        var asmName = typeParts[1].Trim();
		                        var asmPath = Path.Combine(s_modelDirectory, asmName);
		                        asmPath = Path.ChangeExtension(asmPath, ".dll");

		                        //Assembly asm = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(assembly.Location), "ObjectManagement.dll"));
		                        var asm = Assembly.LoadFile(asmPath);
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
		                SoapClientTypeSerializer.Constructing +=
		                    new EventHandler<ConstructorEventArgs>(SoapClientTypeSerializerConstructing);
		                SoapClientTypeSerializer.SerializeClientTypes(assembly);
		            } finally {
		                Environment.CurrentDirectory = currentDirectory;
		            }
		        } catch (Exception exc) {
		            log.Error(exc);
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
				assembly = Assembly.LoadFile(assemblyPath);
			}
			return assembly;
		}

	}


	class Tester {
        private static readonly ITraceLog s_log = TraceLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static void Main(string[] args) {
		    using (var log = new EnterExitLogger(s_log, Level.Info)) {
		        try {
		            var assembly = Assembly.LoadFile(args[0]);

		            if (args.Length > 1) {
		                for (var i = 1; i < args.Length; i++) {
		                    if (Directory.Exists(args[i])) {
		                        var assemblies = Directory.GetFiles(args[i], "*.dll");
		                        foreach (var asm in assemblies) {
		                            Assembly.LoadFile(asm);
		                        }
		                    } else {
		                        Assembly.LoadFile(args[i]);
		                    }

		                }
		            }
		            SoapClientTypeSerializer.DeserializeClientTypes(assembly);
		        } catch (Exception exc) {
		            log.Error(exc);
		        }
		    }
		}
	}
}
