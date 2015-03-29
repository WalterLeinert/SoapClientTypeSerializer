using System;
using System.Collections;
#if !NET_1
using System.Collections.Generic;
#endif
using System.Text;

namespace NTools.Logging.Log4Net {

#if !NET_1
		using Queue = Queue<LoggingEventWrapper>;
#else
	using Queue = ArrayList;
#endif

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
#if !NET_1
				base.Enqueue(loggingEvent);
#else
				base.Add(loggingEvent);
#endif
				if (Count >= m_flushThreshold) {
					Flush(traceLog);
				}
			}
		}


		public void Flush(ITraceLog traceLog) {
			LoggingEventWrapper[] loggingEvents = null;
			lock (s_locker) {
#if !NET_1
				loggingEvents = ToArray();
#else
				loggingEvents = new LoggingEventWrapper[Count];
				CopyTo(loggingEvents);
#endif
				Clear();
			}

			foreach (LoggingEventWrapper loggingEvent in loggingEvents) {
				traceLog.Logger.Log(loggingEvent);
			}
		}
	}
}
