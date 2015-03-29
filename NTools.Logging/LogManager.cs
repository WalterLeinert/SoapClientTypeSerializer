using System;
using System.Collections.Generic;
using System.Text;

namespace NTools.Logging {

	public class LogManager {
		public ILogging GetLogger(Type type) {
			return new Logger();
		}
	}
}
