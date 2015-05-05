using System;
using System.Reflection;
using NTools.Logging.Log4Net;

namespace NTools.Core.Reflection {

	[Serializable]
	public class MethodReflector : MethodBaseReflector {
        private static readonly ITraceLog s_log = TraceLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public MethodReflector(Type type, string methodName, BindingFlags bindingFlags)
			: base(type, methodName, bindingFlags) {
                using (var log = new EnterExitLogger(s_log, "type = {0}, methodName = {1}, bindingFlags = {2}", type, methodName, bindingFlags)) {
                    Info = type.GetMethod(methodName, bindingFlags);
                }
		}

		public MethodReflector(MethodInfo methodInfo)
			: base(methodInfo) {
		    using (var log = new EnterExitLogger(s_log, "methodInfo = {0}", methodInfo)) {		      
		    }
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
