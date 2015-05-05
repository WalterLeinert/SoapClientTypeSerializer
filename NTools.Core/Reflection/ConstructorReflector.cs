using System;
using System.Reflection;

namespace NTools.Core.Reflection {

	[Serializable]
	public class ConstructorReflector : MethodBaseReflector {
		private bool m_isDefaultConstructor;


		public ConstructorReflector(Type type)
			: this(type, AllInstanceDeclared) {
		}

		public ConstructorReflector(Type type, BindingFlags bindingFlags)
			: this(type, new Type[] { }, bindingFlags) {
		}


		public ConstructorReflector(Type type, Type[] types, BindingFlags bindingFlags)
			:
			base(type, ".ctor", bindingFlags) {
			Info = type.GetConstructor(bindingFlags, null, types, null);

			SetupConstructorInfo();
		}

		public ConstructorReflector(ConstructorInfo info)
			: base(info) {
			SetupConstructorInfo();
		}


		public bool IsDefaultConstructor {
			get { return m_isDefaultConstructor; }
		}

		private void SetupConstructorInfo() {
			var parameters = Info.GetParameters();
			if (parameters == null || parameters.Length <= 0) {
				m_isDefaultConstructor = true;
			}
		}

		public new ConstructorInfo Info {
			get { return (ConstructorInfo)base.Info; }
			set { base.Info = value; }
		}

		public object Invoke(object[] parameters) {
            if (IsDefaultConstructor) {
                return Info.Invoke(null);
            } else {
                return Info.Invoke(parameters);
            }
			
		}
	}


}
