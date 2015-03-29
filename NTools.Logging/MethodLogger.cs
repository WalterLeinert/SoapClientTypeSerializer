using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

//- Zusätzliche Namespaces -----------------------------------------------
using log4net.Repository;
using log4net.Core;
using log4net.Util;

namespace NTools.Logging {

	public sealed class MethodLogger : IDisposable {

		/// <summary>
		/// Der String, der bei Methodeneintritt ausgegeben wird.
		/// </summary>
		private const string EnterString = ">> ";

		/// <summary>
		/// Der String, der bei Methodenaustritt ausgegeben wird.
		/// </summary>
		private const string ExitString = "<< ";

		/// <summary>
		/// der aktuelle Logger-Wrapper
		/// </summary>
		private readonly ILogging m_logger;

		/// <summary>
		/// der Log-Level
		/// </summary>
		private Level m_level;

		/// <summary>
		/// Methodeninformation aus dem Stackframe
		/// </summary>
		private MethodBase m_methodBase;

		private Type m_callerStackDeclaringType;

		/// <summary>
		/// der Methodenname
		/// </summary>
		private string m_method;

		private bool m_logIsEnabled;

		/// <summary>
		/// Zeitpunkt des Methodeneintritts; wird im Dispose() für die Berechnung der elapsed Zeit verwendet.
		/// </summary>
		private QueryPerfCounter m_timeEntered;

		static MethodLogger() {
		}


		public MethodLogger(ILogging logger)
			: this(logger, Level.Debug) {
		}



		public MethodLogger(ILogging logger, Level level) {
			m_logger = logger;
			m_level = level;

			m_timeEntered = new QueryPerfCounter(true);
			//m_logIsEnabled = m_logIsEnabled.Logger.IsEnabledFor(m_level);
			m_callerStackDeclaringType = null;
			m_method = "";
			m_methodBase = null;

			if (m_logIsEnabled) {
				Log(EnterString + ' ' + Method);
			}
		}

		public void Log(string format, params object[] args) {
			Log(m_level, format, args);
		}

		public void Log(Level level, string format, params object[] args) {
			//LoggingEvent ev = new LoggingEvent(null, m_traceLog.Logger.Repository, "name", level, string.Format(format, args), null);
		}

		#region IDisposable Member

		public void Dispose() {
			if (m_logIsEnabled) {
				m_timeEntered.Stop();

				StringBuilder sb = new StringBuilder(ExitString);

				if (m_methodBase != null) {
					sb.Append(m_methodBase.Name);
				} else {
					sb.Append(Method);
				}

				sb.AppendFormat(" (elapsed = {0,1:F2} msec)", m_timeEntered.ElapsedMilliseconds);

				//LogProperty(sb.ToString(), m_level, String.Empty);
				Log(m_level, sb.ToString());
			}
		}

		#endregion


		#region private Properties

		/// <summary>
		/// Der auszugebende Methodenname
		/// </summary>
		/// <value>Liefert den Methodennamen</value>
		private string Method {
			get {
				if (string.IsNullOrEmpty(m_method)) {
					return "-unknown-";
				}
				return m_method;
			}
		}

		#endregion
	}
}
