using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NTools.Core.Reflection {    
    using FieldDict     = Dictionary<string, FieldReflector>;
    using MethodDict    = Dictionary<string, MethodReflector>;
    using TypeList      = List<Type>;

    public class TypeReflector {		
		private readonly Type m_type;
		private readonly FieldDict  m_fields;
		private readonly MethodDict m_methods;
        private readonly MethodDict m_staticMethods;

        public TypeReflector(Type type) {
			m_type = type;
            m_fields = new FieldDict();
			m_methods = new MethodDict();
			m_staticMethods = new MethodDict();

			var fields = m_type.GetFields(MemberReflector.AllDeclared);
			foreach (var fi in fields) {
				m_fields.Add(fi.Name, new FieldReflector(fi));
			}

			var methods = m_type.GetMethods(MemberReflector.PrivateInstanceDeclared);

			foreach (var mi in methods) {
				m_methods.Add(BuildMethodSignature(mi), new MethodReflector(mi));
			}

			methods = m_type.GetMethods(MemberReflector.AllStaticDeclared);

			foreach (var mi in methods) {
				m_staticMethods.Add(BuildMethodSignature(mi), new MethodReflector(mi));
			}
		}


		public static string BuildMethodSignature(MethodBase methodBase) {
			var parameterTypes = new TypeList();
			foreach (var pi in methodBase.GetParameters()) {
				parameterTypes.Add(pi.ParameterType);
			}

		    var types = parameterTypes.ToArray(); 
            return BuildMethodSignature(methodBase.Name, types);
		}


		public static string BuildMethodSignature(string name, Type[] parameterTypes) {
			var sb = new StringBuilder(name);

			sb.Append("(");
			var first = true;
			foreach (var type in parameterTypes) {
				if (!first) {
					sb.Append(", ");
				}
				sb.Append(type.FullName);
				first = false;
			}
			sb.Append(")");
			return sb.ToString();
		}


		public Type Type {
			get { return m_type; }
		}

		public object GetField(object instance, string name) {
			if (instance == null) {
				throw new ArgumentNullException("instance");
			}
			if (Utility.IsNullOrEmpty(name)) {
				throw new ArgumentNullException("name");
			}

			if (!m_fields.ContainsKey(name)) {
				throw new ArgumentException(string.Format("Type {0} has no such field.", "name"));
			}

			return m_fields[name].GetValue(instance);
		}

		public object GetField(string name) {
			if (Utility.IsNullOrEmpty(name)) {
				throw new ArgumentNullException("name");
			}

			if (!m_fields.ContainsKey(name)) {
				throw new ArgumentException(string.Format("Type {0} has no such field.", "name"));
			}

			return m_fields[name].GetValue(null);
		}

		public object Invoke(object instance, string methodName, object[] parameters) {
			if (instance == null) {
				throw new ArgumentNullException("instance");
			}
			if (Utility.IsNullOrEmpty(methodName)) {
				throw new ArgumentNullException("methodName");
			}

			if (!m_methods.ContainsKey(methodName)) {
				throw new ArgumentException(string.Format("Type {0} has no such method.", m_type), "methodName");
			}

			return m_methods[methodName].Invoke(instance, parameters);
		}

		public object Invoke(string methodName, object[] parameters) {
			if (Utility.IsNullOrEmpty(methodName)) {
				throw new ArgumentNullException("methodName");
			}

			if (!m_staticMethods.ContainsKey(methodName)) {
				throw new ArgumentException(string.Format("Type {0} has no such method.", m_type), "methodName");
			}

			return m_staticMethods[methodName].Invoke(null, parameters);
		}
	}

}
