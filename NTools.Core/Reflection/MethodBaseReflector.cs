using System;
using System.Reflection;
using NTools.Logging.Log4Net;

namespace NTools.Core.Reflection {

	[Serializable]
	public abstract class MethodBaseReflector : MemberReflector {
        private static readonly ITraceLog s_log = TraceLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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