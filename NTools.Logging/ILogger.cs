using System;
using System.Collections.Generic;
using System.Text;

namespace NTools.Logging {

	public interface ILogging {
		void Log(int level, string format, params object[] args);
	}
}
