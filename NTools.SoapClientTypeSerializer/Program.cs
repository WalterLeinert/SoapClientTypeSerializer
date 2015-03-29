using System;
using System.Collections;
using System.IO;
using System.Reflection;

using NTools.WebServiceSupport;

namespace NTools.WebServiceSupport.Tools {

	class Program {
		private static ArrayList s_constructorTypes = new ArrayList();
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

			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

			Assembly assembly = Assembly.LoadFrom(args[0]);

			s_modelDirectory = Path.GetDirectoryName(assembly.Location);

			string serializerFileName = assembly.Location;
			serializerFileName = Path.ChangeExtension(serializerFileName, ".XmlSerializers.dll");

			if (File.Exists(serializerFileName)) {
				Assembly serializerAssembly = Assembly.LoadFrom(serializerFileName);
			}			

			ArrayList types = new ArrayList();
			string[] constructorTypes = args[1].Split(new char[] {';'});

			foreach (string constructorType in constructorTypes) {
				string[] typeParts = constructorType.Split(new char[] {','});

				Type type;
				if (typeParts.Length > 1) {
					string asmName = typeParts[1].Trim();
					string asmPath = Path.Combine(s_modelDirectory, asmName);
					asmPath = Path.ChangeExtension(asmPath, ".dll");

					//Assembly asm = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(assembly.Location), "ObjectManagement.dll"));
					Assembly asm = Assembly.LoadFrom(asmPath);
					type = asm.GetType(typeParts[0]);
				} else {
					type = Type.GetType(constructorType);
				}

				if (type != null) {
					s_constructorTypes.Add(type);					
				}
			}

			string currentDirectory = Environment.CurrentDirectory;

			try {
				Environment.CurrentDirectory = Path.GetDirectoryName(assembly.Location);
#if !NET_1
				SoapClientTypeSerializer.Constructing += new EventHandler<ConstructorEventArgs>(SoapClientTypeSerializer_Constructing);
#else
				SoapClientTypeSerializer.Constructing += new ConstructorEventHandler(SoapClientTypeSerializer_Constructing);
#endif
				SoapClientTypeSerializer.SerializeClientTypes(assembly);
			} finally {
				Environment.CurrentDirectory = currentDirectory;
			}
		}

		static void SoapClientTypeSerializer_Constructing(object sender, ConstructorEventArgs e) {
			//if (e.Type == typeof(object)) {
			e.Args = new object[] { null };
			if (s_constructorTypes.Count > 0) {
				e.Types = (Type[])s_constructorTypes.ToArray(typeof(Type));
			}
			//}
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
			Assembly assembly = null;

			string[] parts = args.Name.Split(new char[] {','});
			string assemblyPath = Path.ChangeExtension(Path.Combine(s_modelDirectory, parts[0]), ".dll");

			if (File.Exists(assemblyPath)) {
				assembly = Assembly.LoadFrom(assemblyPath);
			}
			return assembly;
		}
	}

}
