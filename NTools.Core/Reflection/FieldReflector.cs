using System;
using System.Reflection;
using NTools.Logging.Log4Net;

namespace NTools.Core.Reflection {

	/// <summary>
	/// Klasse zur Serialisierung von Membern, die eigentlich nicht serialisierbar sind.
	/// Die Serialisierung erfolgt über Reflection.
	/// </summary>
	[Serializable]
	public class FieldReflector : MemberReflector {
        private static readonly ITraceLog s_log = TraceLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


		public FieldReflector(Type type, string fieldName)
			: this(type, fieldName, AllDeclared) {
		}

        public FieldReflector(Type type, string fieldName, BindingFlags bindingFlags)
            : base(type, fieldName, bindingFlags) {
            using (var log = new EnterExitLogger(s_log, "type = {0}, fieldName = {1}, bindingFlags = {2}", type, fieldName, bindingFlags)) {
                Info = type.GetField(fieldName, bindingFlags);
            }
        }

        public FieldReflector(MemberInfo fieldInfo)
            : base(fieldInfo) {
            using (var log = new EnterExitLogger(s_log, "fieldInfo = {0}", fieldInfo)) {
            }
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
