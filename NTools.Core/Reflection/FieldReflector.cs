using System;
using System.Reflection;

namespace NTools.Core.Reflection {

	/// <summary>
	/// Klasse zur Serialisierung von Membern, die eigentlich nicht serialisierbar sind.
	/// Die Serialisierung erfolgt über Reflection.
	/// </summary>
	[Serializable]
	public class FieldReflector : MemberReflector {

		public FieldReflector(Type type, string fieldName)
			: this(type, fieldName, AllDeclared) {
		}

		public FieldReflector(Type type, string fieldName, BindingFlags bindingFlags)
			: base(type, fieldName, bindingFlags) {
			Info = type.GetField(fieldName, bindingFlags);
		}

		public FieldReflector(MemberInfo fieldInfo)
			: base(fieldInfo) {
		}

		public new FieldInfo Info {
			get { return (FieldInfo)base.Info; }
			set { base.Info = value; }
		}

		public object GetValue(object instance) {
			return Info.GetValue(instance);
		}


		public void SetValue(object instance, object value) {
			Info.SetValue(instance, value);
		}

	}

}
