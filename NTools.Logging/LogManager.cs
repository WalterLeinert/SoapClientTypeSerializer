using System;

namespace NTools.Logging {

	public class LogManager {
		public ILogging GetLogger(Type type) {
			return new Logger();
		}
	}
}
