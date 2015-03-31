using System;
using System.Reflection;

namespace NTools.Core.Reflection {

	[Serializable]
	public abstract class MethodBaseReflector : MemberReflector {
		protected MethodBaseReflector(Type type, string methodName, BindingFlags bindingFlags)
			: base(type, methodName, bindingFlags) {
		}

		protected MethodBaseReflector(MethodBase methodBase)
			: base(methodBase) {
		}

		protected new MethodBase Info {
			get { return (MethodBase)base.Info; }
			set { base.Info = value; }
		}
	}

}