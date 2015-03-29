using System;
using System.Text;
using System.Collections;
using System.Globalization;

using NTools.Logging;

namespace NTools.Logging.Log4Net {

	/// <summary>
	/// Implementiert eine Klasse für Zeitmessungen.
	/// </summary>
	/// <remarks>
	/// Die Log-Konfiguration sieht z.B. wie folgt aus:
	/// <code>
	///		<log4net>
	///		...
	///  		<appender name="ElapsedTimeAppender" type="log4net.Appender.ConsoleAppender" >
	///  			<layout type="log4net.Layout.PatternLayout">
	///					<param name="ConversionPattern" value="%m%n" />
	///				</layout>
	///			</appender>	
	///		...
	///		
	///			<logger name="HP.Dpma.Sys.Diagnostics.ElapsedTimeLogger">
	///				<level value="INFO" />
	///				<appender-ref ref="ElapsedTimeAppender" />
	///			</logger>	
	///			
	///		</log4net>		
	///	</code>	
	/// </remarks>
	public class ElapsedTimeLogger : IDisposable {
		private static ITraceLog	s_log = TraceLogManager.GetLogger(typeof(ElapsedTimeLogger));

		private static string			s_logSeparator = " ";

		private const string s_ERROR_MARKER = ".ERROR";

		/// <summary>
		/// Hilfsklasse für genaue Zeitmessung (100ns Auflösung)
		/// </summary>
		private QueryPerfCounter		m_timer;

		/// <summary>
		/// Der Name des Messpunkts
		/// </summary>
		private string					m_key;

		/// <summary>
		/// Falls <c>true</c> wird die Messung im Konstruktor begonnen und in <see cref="Dispose"/>
		/// beendet und gelogged
		/// </summary>
		private bool					m_logOnDispose;

		/// <summary>
		/// Falls <c>true</c> wird der Name <c>m_key</c> des Messpunktes mit einer Fehlerkennung 
		/// versehen, so dass Fehlersituationen getrennt ausgewertet werden können.
		/// </summary>
		private bool					m_operationInError;

		/// <summary>
		/// Zusätzliche optionale Liste von weiteren Werten für die Messung.
		/// </summary>
		private ArrayList				m_values;


		#region Konstruktor / Cleanup

		/// <summary>
		/// Initialisiert eine neue Instanz.
		/// </summary>
		/// <param name="key">Der Log-Key für die Messung.</param>
		/// <param name="logOnDispose"><c>true</c>, falls über <see cref="Dispose"/> gelogged werden soll.</param>
		/// <remarks>
		/// Falls <paramref name="logOnDispose"/> <c>true</c> ist, wird die Messung im Konstruktor begonnen und in <see cref="Dispose"/>
		/// beendet und gelogged.
		/// </remarks>
		public ElapsedTimeLogger(string key, bool logOnDispose) {
			m_key			= key;
			m_logOnDispose	= logOnDispose;

			m_values		= new ArrayList();			
			m_timer			= new QueryPerfCounter();

			if (m_logOnDispose) {
				Start();
			}
		}


		/// <summary>
		/// Initialisiert eine neue Instanz für automatisches Logging in <see cref="Dispose"/> Dispose.
		/// </summary>
		/// <param name="key">Der Log-Key für die Messung.</param>
		public ElapsedTimeLogger(string key) : this(key, true) {
		}


		/// <summary>
		/// Automatisches Logging, falls m_logOnDispose <c>true</c> ist.
		/// </summary>		
		public void Dispose() {
			if (m_logOnDispose) {
				Stop();
			}
		}

		#endregion
	

		/// <summary>
		/// Schreibt einen Log-Eintrag in den konfigurierten log4net-Appender.
		/// </summary>
		private void Log() {
			StringBuilder sb = new StringBuilder();

			sb.Append(m_key);
			if ( OperationInError ) {
				sb.Append(s_ERROR_MARKER);
			}
			sb.Append(s_logSeparator);
			sb.Append(m_timer.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture));

			foreach (object val in m_values) {
				sb.Append(s_logSeparator);

				IConvertible convertible = val as IConvertible;

				if (convertible != null) {
					sb.Append(convertible.ToString(CultureInfo.InvariantCulture));
				} else {
					sb.Append(val.ToString());
				}
			}

			m_values.Clear();

			s_log.Info(sb.ToString());
		}


		/// <summary>
		/// Liefert oder setzt den Log-Separator.
		/// </summary>
		public static string LogSeparator {
			get { return s_logSeparator; }
			set { s_logSeparator = value; }
		}


		/// <summary>
		/// Startet eine neue Messung
		/// </summary>
		public void Start() {
			m_timer.Start();
		}

		/// <summary>
		/// Beendet eine Messung ohne einen Log-Eintrag zu schreiben.
		/// </summary>
		public void StopWithoutLog() {
			m_timer.Stop();
		}

		/// <summary>
		/// Markiere für die Messung eine Fehlersituation. Der Log wird mit
		/// einem key mit .ERROR Anhang ausgegeben.
		/// </summary>
		public void MarkOperationInError() {
			m_operationInError = true;
		}

		/// <summary>
		/// Handelt es sich um eine Messung mit Fehlersituation.
		/// </summary>
		public bool OperationInError {
			get { return m_operationInError; }
		}

		/// <summary>
		/// Beendet eine Messung und schreibt einen Log-Eintrag.
		/// </summary>
		public void Stop() {
			m_timer.Stop();
			Log();
		}


		/// <summary>
		/// Fügt für die aktuelle Messung eine Liste von Werten hinzu, die dann über <see cref="Log"/> ausgegeben werden.
		/// </summary>
		/// <param name="values"></param>
		public void AddValues(params object[] values) {
			m_values.AddRange(values);
		}

		public TimeSpan Delta {
			get { return new TimeSpan((long) m_timer.Ticks); }
		}
		
	}
}
