using System;
using System.Text;
using log4net.Core;
using log4net.Repository;
//- Zusätzliche Namespaces -----------------------------------------------


namespace NTools.Logging.Log4Net {

	/// <summary>
	/// Wrapper um die Klasse <see cref="LoggingEvent"/>.
	/// </summary>
	public sealed class LoggingEventWrapper  {
		private readonly string m_format;
		private readonly object[] m_args;

		private readonly EnterExit m_enterExit;
		private readonly string m_methodName;
		private readonly Type m_callerStackBoundaryDeclaringType;
		private readonly ILoggerRepository m_repository;
		private readonly LoggingEventData m_data;

		private readonly bool m_eventDataSet;

		private readonly string m_loggerName;
		private readonly Level m_level;
		private readonly object m_message;
		private readonly Exception m_exception;
		private readonly TimeSpan m_elapsed;
		private readonly bool m_elapsedSet;


		public LoggingEventWrapper(EnterExit enterExit, string methodName, Type callerStackBoundaryDeclaringType,
			ILoggerRepository repository, LoggingEventData data) {
			
			m_enterExit = enterExit;
			m_methodName = methodName;
			m_callerStackBoundaryDeclaringType = callerStackBoundaryDeclaringType;
			m_repository = repository;
			m_data = data;
			m_eventDataSet = true;
		}

		public LoggingEventWrapper(EnterExit enterExit, string methodName, TimeSpan elapsed, Type callerStackBoundaryDeclaringType,
		 ILoggerRepository repository, string loggerName, Level level) {

			m_enterExit = enterExit;
			m_methodName = methodName;
			m_callerStackBoundaryDeclaringType = callerStackBoundaryDeclaringType;
			m_repository = repository;
			m_loggerName = loggerName;
			m_level = level;
			m_elapsed = elapsed;
			m_elapsedSet = true;
		}

		public LoggingEventWrapper(EnterExit enterExit, string methodName, Type callerStackBoundaryDeclaringType,
			 ILoggerRepository repository, string loggerName, Level level,
				object message, Exception exception) {

			m_enterExit = enterExit;
			m_methodName = methodName;
			m_callerStackBoundaryDeclaringType = callerStackBoundaryDeclaringType;
			m_repository = repository;
			m_loggerName = loggerName;
			m_level = level;
			m_message = message;
			m_exception = exception;
		}

		public LoggingEventWrapper(EnterExit enterExit, string methodName, Type callerStackBoundaryDeclaringType,
			ILoggerRepository repository, string loggerName, Level level, Exception exception,
			string format, params object[] args) {
		
			#region Checks
			if (callerStackBoundaryDeclaringType == null) {
				throw new ArgumentNullException("callerStackBoundaryDeclaringType");
			}
			if (repository == null) {
				throw new ArgumentNullException("repository");
			}
			#endregion

			m_enterExit = enterExit;
			m_methodName = methodName;
			m_callerStackBoundaryDeclaringType = callerStackBoundaryDeclaringType;
			m_repository = repository;
			m_loggerName = loggerName;
			m_level = level;
			m_exception = exception;

			m_format = format;
			m_args = args;
		}
		 

		public static implicit operator LoggingEvent(LoggingEventWrapper eventWrapper) {
			LoggingEvent loggingEvent;

			if (eventWrapper.m_eventDataSet) {
				loggingEvent = new LoggingEvent(eventWrapper.m_callerStackBoundaryDeclaringType, eventWrapper.m_repository, eventWrapper.m_data);
			} else if (eventWrapper.m_format != null) {
				loggingEvent = new LoggingEvent(eventWrapper.m_callerStackBoundaryDeclaringType, eventWrapper.m_repository,
				                        eventWrapper.m_loggerName, eventWrapper.m_level,
				                        string.Format(eventWrapper.m_format, eventWrapper.m_args), eventWrapper.m_exception);

			} else {
				loggingEvent = new LoggingEvent(eventWrapper.m_callerStackBoundaryDeclaringType, eventWrapper.m_repository,
						eventWrapper.m_loggerName, eventWrapper.m_level,
						eventWrapper.m_message, eventWrapper.m_exception);
			}

			var elapsedInfo = string.Empty;
			if (eventWrapper.m_elapsedSet) {
				elapsedInfo = string.Format(" (elapsed = {0,1:F2} msec)", (double) eventWrapper.m_elapsed.Milliseconds);
			}

			var sb = new StringBuilder(EnterExitLogger.EnterExitStrings[(int)eventWrapper.m_enterExit]);
			sb.Append(eventWrapper.m_methodName);
			if (elapsedInfo.Length > 0) {
                sb.Append(elapsedInfo);
			}
			loggingEvent.Properties[EnterExitLogger.MethodPropertyName] = sb.ToString();
			return loggingEvent;
		}
	}
}
