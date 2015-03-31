using System.Collections.Generic;

namespace NTools.Logging.Log4Net {
	using Queue = Queue<LoggingEventWrapper>;

	public class LoggingEventQueue : Queue {
		private static volatile int[] s_locker = new int[0];
		private bool m_bufferingOn;
		private int m_flushThreshold;

		public LoggingEventQueue(int capacity, int flushThreshold)
			: base(capacity) {
			m_flushThreshold = flushThreshold;
		}

		public bool BufferingOn {
			get { return m_bufferingOn; }
			set { m_bufferingOn = value; }
		}


		public void Enqueue(ITraceLog traceLog, LoggingEventWrapper loggingEvent) {
			lock (s_locker) {
				base.Enqueue(loggingEvent);
				if (Count >= m_flushThreshold) {
					Flush(traceLog);
				}
			}
		}


		public void Flush(ITraceLog traceLog) {
			LoggingEventWrapper[] loggingEvents = null;
			lock (s_locker) {
				loggingEvents = ToArray();
				Clear();
			}

			foreach (LoggingEventWrapper loggingEvent in loggingEvents) {
				traceLog.Logger.Log(loggingEvent);
			}
		}
	}
}
