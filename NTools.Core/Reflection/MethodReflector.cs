using System;
using System.Reflection;

namespace NTools.Core.Reflection {

	[Serializable]
	public class MethodReflector : MethodBaseReflector {

		public MethodReflector(Type type, string methodName, BindingFlags bindingFlags)
			: base(type, methodName, bindingFlags) {
			Info = type.GetMethod(methodName, bindingFlags);
		}

		public MethodReflector(MethodInfo methodInfo)
			: base(methodInfo) {
		}

		public new MethodInfo Info {
			get { return (MethodInfo)base.Info; }
			set { base.Info = value; }
		}

		public object Invoke(object instance, object[] parameters) {
			return Info.Invoke(instance, parameters);
		}
	}

}
