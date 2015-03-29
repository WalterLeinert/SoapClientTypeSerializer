using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NTools.Core.DynamicCode {
	
	public class TypeProxy<TClass> {
		private static readonly Dictionary<string, FieldProxy<TClass>> s_fieldProxies;
		private static readonly Dictionary<string, PropertyProxy<TClass>> s_propertyProxies;

		static TypeProxy() {
			s_fieldProxies = new Dictionary<string, FieldProxy<TClass>>();
			s_propertyProxies = new Dictionary<string, PropertyProxy<TClass>>();

			foreach (FieldInfo info in typeof(TClass).GetFields(MemberProxy.PrivateInstanceDeclared) ) {
				s_fieldProxies.Add(info.Name, new FieldProxy<TClass>(info));
			}
		}

		#region Filed Access

		private static FieldProxy<TClass> GetFieldProxy(string name) {
			FieldProxy<TClass> proxy;
			if (!s_fieldProxies.TryGetValue(name, out proxy)) {
				FieldInfo info = typeof(TClass).GetField(name);
				proxy = new FieldProxy<TClass>(info);
				s_fieldProxies.Add(name, proxy);
			}
			return proxy;
		}

		public TField GetFieldValue<TField>(TClass instance, string name) {		
			return GetFieldProxy(name).GetValue<TField>(instance);
		}

		public void SetFieldValue<TField>(TClass instance, string name, TField value) {	
			GetFieldProxy(name).SetValue<TField>(instance, value);
		}

		public FieldProxy<TClass> this[string name] {
			get { return s_fieldProxies[name]; }
		}

		#endregion

		private static PropertyProxy<TClass> GetPropertyProxy(string name) {
			PropertyProxy<TClass> proxy;
			if (!s_propertyProxies.TryGetValue(name, out proxy)) {
				PropertyInfo info = typeof(TClass).GetProperty(name);
				proxy = new PropertyProxy<TClass>(info);
				s_propertyProxies.Add(name, proxy);
			}
			return proxy;
		}


		public T GetPropertyValue<T>(TClass instance, string name) {	
			return (T) GetPropertyProxy(name).GetValueByDelegate<T>(instance);
		}

		public void SetPropertyValue<T>(TClass instance, string name, T value) {	
			GetPropertyProxy(name).SetValueByDelegate<T>(instance, value);
		}

		#region Zugriff auf Properties von primitiven Typen
		public int GetIntPropertyValue(TClass instance, string name) {
			return GetPropertyProxy(name).GetIntValue(instance);
		}

		public void SetIntPropertyValue(TClass instance, string name, int value) {
			GetPropertyProxy(name).SetIntValue(instance, value);
		}
		#endregion
	}

	public class TypeProxy {
		private readonly Type m_type;
		private readonly Dictionary<string, FieldProxy> m_fieldProxies;

		public TypeProxy(Type type) {
			m_type = type;

			m_fieldProxies = new Dictionary<string, FieldProxy>();

			foreach (FieldInfo info in m_type.GetFields(MemberProxy.PrivateInstanceDeclared)) {
				m_fieldProxies.Add(info.Name, new FieldProxy(info));
			}
		}

		public TField GetValue<TField>(object instance, string name) {
			return m_fieldProxies[name].GetValue<TField>(instance);
		}

		public void SetValue<TField>(object instance, string name, TField value) {
			m_fieldProxies[name].SetValue<TField>(instance, value);
		}

		public FieldProxy this[string name] {
			get { return m_fieldProxies[name]; }	
		}
	}
}
