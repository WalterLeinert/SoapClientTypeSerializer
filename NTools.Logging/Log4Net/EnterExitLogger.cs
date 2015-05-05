//- Standard Namespaces --------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using log4net.Core;
using log4net.Repository;
using log4net.Util;
using NTools.Core;
//- Zusätzliche Namespaces -----------------------------------------------

namespace NTools.Logging.Log4Net {

	/// <summary>
	/// Die Klasse EnterExitLogger implementiert eine Hilfsklasse, um Methodenein- bzw.
	/// austritte sicher und einfach tracen zu können. Dabei wird der Methodeneintritt im 
	/// Konstruktor ausgegeben. Zum sicheren Trace des Methodenaustritts implementiert 
	/// EnterExitLogger das Interface <see cref="IDisposable"/>, damit der Trace des 
	/// Methodenaustritts bei Aufruf von <see cref="Dispose"/> generiert wird. 
	/// </summary>
	/// <remarks>
	/// Verwendet wird ExitEnterLogger mit Hilfe des <c>using</c> Statements; Konstruktorargumente
	/// sind der Logger, der Log-Level und ggf. weitere Ausgabeparmeter:
	/// 
	/// Einfachste Form (Default-LogLevel = Level.Debug):
	/// <code>
	///		void Method1 (string name) {
	///			using (new EnterExitLogger(s_log) {
	///				// >> hier wird der Enter-Trace ausgegeben
	///				...
	///			}	// &lt;&lt; hier wird der Exit-Trace ausgegeben
	///		}
	/// </code>
	/// 
	/// oder mit Angabe weiterer Ausgabeparameter:
	/// <code>
	///		void Method1 (string name) {
	///			using (new EnterExitLogger(s_log, Level.Debug, "name = {0}", name) {
	///				// >> hier wird der Enter-Trace ausgegeben
	///				...
	///			}	// &lt;&lt; hier wird der Exit-Trace ausgegeben
	///		}
	/// </code> 
	/// 
	/// Achtung: die alte Variante, bei der der Methodenname explizit angegeben werden konnte wird 
	/// nicht mehr verwendet:
	/// <code>
	///		void Method1 (string name) {
	///			const string fn = "Method1(string)";
	///			
	///			using (new EnterExitLogger(s_log, fn, DEBUG) {
	///				// >> hier wird der Enter-Trace ausgegeben
	///				...
	///				log.Debug("name = {0}", name);
	///				...
	///			}	// &lt;&lt; hier wird der Exit-Trace ausgegeben
	///		}
	/// </code>
	/// <p>
	/// Anmerkung: EnterExitLogger wird als <c>struct</c> realisiert, da dann die Speicherverwaltung nicht 
	/// über den Garbagecollector läuft und damit auch die Performance verbessert wird.
	/// Achtung: z.Zt. wird EnterExitLogger wieder als Klasse implementiert!
	/// </p> 
	/// </remarks>
	public class EnterExitLogger : IDisposable, ITraceLog {

		#region Konstante

		/// <summary>
		/// Name der Property, unter der der Methodenname in <cref name="LoggingEvent.Properties"/>
		/// abgelegt wird.
		/// </summary>
		internal const string MethodPropertyName		= "Method";

		/// <summary>
		/// Der String, der bei Methodeneintritt ausgegeben wird.
		/// </summary>
		private const string EnterString			= ">> ";

		/// <summary>
		/// Der String, der bei Methodenaustritt ausgegeben wird.
		/// </summary>
		private const string ExitString = "<< ";

		private const string LogString = "@ ";

		internal static readonly string[] EnterExitStrings = new string[] {EnterString, ExitString, LogString};

		/// <summary>
		/// Der Default-LogLevel
		/// </summary>
		private static readonly Level DefaultLevel	= Level.Debug;

		#endregion Konstante

		#region private Member

        private static readonly Type		s_declaringType = typeof(EnterExitLogger);

	
		/// <summary>
		/// der aktuelle Logger-Wrapper
		/// </summary>
		private readonly TraceLog			m_traceLog;

		/// <summary>
		/// Falls <c>true</c>, wird nicht m_method, sondern m_methodBase verwendet.
		/// </summary>
		private readonly bool						m_useMethodbase;

		/// <summary>
		/// Methodeninformation aus dem Stackframe
		/// </summary>
		private MethodBase					m_methodBase;

		private Type						m_callerStackDeclaringType;

		/// <summary>
		/// der Methodenname
		/// </summary>
		private	string						m_method;

		/// <summary>
		/// der Log-Level
		/// </summary>
		private readonly Level						m_level;

		/// <summary>
		/// der eigentliche Logger
		/// </summary>
		private ILogger						m_loggerWrapper;

		/// <summary>
		/// das LoggeRepository
		/// </summary>
		private	ILoggerRepository			m_loggerRepository;

		/// <summary>
		/// der Logger-Name
		/// </summary>
		private string						m_loggerName;

		/// <summary>
		/// Zeitpunkt des Methodeneintritts; wird im Dispose() für die Berechnung der elapsed Zeit verwendet.
		/// </summary>
		private QueryPerfCounter			m_timeEntered = new QueryPerfCounter(true);

		private Level m_levelEnabledMin = Level.Off;

		private Level m_levelDebug;
		private Level m_levelInfo;
		private Level m_levelWarn;
		private Level m_levelTrace;
		private Level m_levelError;
		private Level m_levelFatal;


		private bool m_isDebugEnabled;
		private bool m_isInfoEnabled;
		private bool m_isTraceEnabled;
		private bool m_isWarnEnabled;
		private bool m_isErrorEnabled;
		private bool m_isFatalEnabled;

		/// <summary>
		/// Die aktuelle Version der Log-Info. Wird immer gegen die statische Info s_loggingConfigurationVersion geprüft und
		/// ggf. wird dann die Info für den aktuellen Logger aktualisiert.
		/// </summary>
		private int m_loggingConfigurationVersion = 0;

		#endregion private Member

		private static int s_loggingConfigurationVersion = 1;

		private static readonly LoggingEventQueue s_loggingEventQueue = new LoggingEventQueue(10000, 1);

        static EnterExitLogger() {
        	var assembly = Assembly.GetEntryAssembly();
			if (assembly == null) {
				assembly = Assembly.GetExecutingAssembly();
			}
        	var repository = LoggerManager.GetRepository(assembly);
			if (repository != null) {
				repository.ConfigurationChanged += EnterExitLogger_ConfigurationChanged;
				repository.ConfigurationReset += EnterExitLogger_ConfigurationReset;
			}
        }

        static void EnterExitLogger_ConfigurationReset(object sender, EventArgs e) {
            s_loggingConfigurationVersion++;
        }

        static void EnterExitLogger_ConfigurationChanged(object sender, EventArgs e) {
            s_loggingConfigurationVersion++;
        }

		#region Public Konstruktoren

		/// <summary>
		/// Der Konstruktor initialisiert die Instanz mit den übergebenen Parametern und
		/// erzeugt einen LogEvent für den Methodeneintritt, falls Tracing 
		/// für den <see cref="Level"/> <paramref name="level"/> konfiguriert ist.
		/// Mit <paramref name="format"/> und <paramref name="args"/> kann zusammen mit 
		/// dem Methodeneintritt weitere Information ausgegeben werden.
		/// </summary>
		/// <param name="traceLog">zugehöriger Trace-Logger</param>
		/// <param name="method">Name der zu tracenden Methode</param>
		/// <param name="level">gewünschter Log-Level</param>		
		/// <param name="format">Formatstring</param>
		/// <param name="args">variable Argumentliste</param>
		/// <seealso cref="Dispose"/>
		public EnterExitLogger(ITraceLog traceLog, string method, Level level, string format, params object[] args) {
            m_traceLog = (TraceLog) traceLog;
			m_level = level;
			m_method = method;
			m_useMethodbase = false;

			if (IsEnabledFor(level)) {
				LogProperty(EnterExit.Enter, 2, level, format, args);
			}		
		}

		/// <summary>
		/// Der Konstruktor initialisiert die Instanz mit den übergebenen Parametern und
		/// erzeugt einen LogEvent für den Methodeneintritt, falls Tracing 
		/// für den <see cref="Level"/> <paramref name="level"/> konfiguriert ist.
		/// Mit dem Parameter <paramref name="message"/> kann zusammen mit 
		/// dem Methodeneintritt weitere Information ausgegeben werden.
		/// </summary>
		/// <param name="traceLog">zugehöriger Trace-Logger</param>
		/// <param name="method">Name der zu tracenden Methode</param>
		/// <param name="level">gewünschter Log-Level</param>		
		/// <param name="message">weitere Information als <c>object</c></param>
		/// <seealso cref="Dispose"/>
		public EnterExitLogger(ITraceLog traceLog, string method, Level level, object message) {
            m_traceLog = (TraceLog) traceLog;
			m_level = level;
			m_method = method;
			m_useMethodbase = false;

			if (IsEnabledFor(level)) {
				LogProperty(EnterExit.Enter, 2, level, message);
			}
		}


		/// <summary>
		/// Der Konstruktor initialisiert die Instanz mit den übergebenen Parametern und
		/// gibt einen Enter-Trace aus, falls Tracing für den <see cref="Level"/> 
		/// <paramref name="level"/> konfiguriert ist.
		/// </summary>
		/// <param name="traceLog">zugehöriger Trace-Logger</param>
		/// <param name="method">Name der zu tracenden Methode</param>
		/// <param name="level">gewünschter Log-Level</param>
		/// <seealso cref="Dispose"/>
		public EnterExitLogger(ITraceLog traceLog, string method, Level level) {
            m_traceLog = (TraceLog) traceLog;
			m_level = level;
			m_method = method;
			m_useMethodbase = false;

			if (IsEnabledFor(level)) {
				LogProperty(EnterExit.Enter, 2, level, String.Empty);
			}
		}


		#region Konstruktoren für implizite Ermittlung des Methodennamens

		/// <summary>
		/// Der Konstruktor initialisiert die Instanz mit den übergebenen Parametern und
		/// erzeugt einen LogEvent für den Methodeneintritt, falls Tracing 
		/// für den <see cref="Level"/> <paramref name="level"/> konfiguriert ist.
		/// Mit <paramref name="format"/> und <paramref name="args"/> kann zusammen mit 
		/// dem Methodeneintritt weitere Information ausgegeben werden.
		/// </summary>
		/// <remarks>
		/// Der Name der zu tracenden Methode wird implizit über Reflection ermittelt.
		/// </remarks>
		/// <param name="framesToSkip">Anzahl der zu überspringenden Stack-Frames</param>
		/// <param name="traceLog">zugehöriger Trace-Logger</param>
		/// <param name="level">gewünschter Log-Level</param>		
		/// <param name="format">Formatstring</param>
		/// <param name="args">variable Argumentliste</param>
		/// <seealso cref="Dispose"/>
		private EnterExitLogger(int framesToSkip, ITraceLog traceLog, Level level, string format, params object[] args) {
            m_traceLog = (TraceLog) traceLog;
			m_level = level;
			m_useMethodbase = true;

			if (IsEnabledFor(level)) {
				LogProperty(EnterExit.Enter, framesToSkip, level, format, args);
			}
		}


		/// <summary>
		/// Der Konstruktor initialisiert die Instanz mit den übergebenen Parametern und
		/// erzeugt einen LogEvent für den Methodeneintritt, falls Tracing 
		/// für den <see cref="Level"/> <paramref name="level"/> konfiguriert ist.
		/// Mit <paramref name="format"/> und <paramref name="args"/> kann zusammen mit 
		/// dem Methodeneintritt weitere Information ausgegeben werden.
		/// </summary>
		/// <remarks>
		/// Der Name der zu tracenden Methode wird implizit über Reflection ermittelt.
		/// </remarks>
		/// <param name="traceLog">zugehöriger Trace-Logger</param>
		/// <param name="level">gewünschter Log-Level</param>		
		/// <param name="format">Formatstring</param>
		/// <param name="args">variable Argumentliste</param>
		/// <seealso cref="Dispose"/>
		public EnterExitLogger(ITraceLog traceLog, Level level, string format, params object[] args) : this(4, traceLog, level, format, args) {
		}


		/// <summary>
		/// Der Konstruktor initialisiert die Instanz mit den übergebenen Parametern und
		/// erzeugt einen LogEvent für den Methodeneintritt, falls Tracing 
		/// für den Default-Level <see cref="DefaultLevel"/> konfiguriert ist.
		/// Mit <paramref name="format"/> und <paramref name="args"/> kann zusammen mit 
		/// dem Methodeneintritt weitere Information ausgegeben werden.
		/// </summary>
		/// <remarks>
		/// Der Name der zu tracenden Methode wird implizit über Reflection ermittelt.
		/// </remarks>
		/// <param name="traceLog">zugehöriger Trace-Logger</param>
		/// <param name="format">Formatstring</param>
		/// <param name="args">variable Argumentliste</param>
		/// <seealso cref="Dispose"/>
		public EnterExitLogger(ITraceLog traceLog, string format, params object[] args) : 
			this(2, traceLog, DefaultLevel, format, args) {
		}




		/// <summary>
		/// Der Konstruktor initialisiert die Instanz mit den übergebenen Parametern und
		/// erzeugt einen LogEvent für den Methodeneintritt, falls Tracing 
		/// für den <see cref="Level"/> <paramref name="level"/> konfiguriert ist.
		/// Mit dem Parameter <paramref name="message"/> kann zusammen mit 
		/// dem Methodeneintritt weitere Information ausgegeben werden.
		/// </summary>
		/// <param name="framesToSkip">Anzahl der zu überspringenden Stack-Frames.</param>
		/// <param name="traceLog">zugehöriger Trace-Logger.</param>
		/// <param name="level">gewünschter Log-Level.</param>		
		/// <param name="message">weitere Information als <c>object</c>.</param>
		/// <seealso cref="Dispose"/>
		private EnterExitLogger(int framesToSkip, ITraceLog traceLog, Level level, object message) {
			m_traceLog = (TraceLog) traceLog;
			m_level = level;
			m_useMethodbase = true;

			if (IsEnabledFor(level)) {
				LogProperty(EnterExit.Enter, framesToSkip, level, message);
			}
		}


		/// <summary>
		/// Der Konstruktor initialisiert die Instanz mit den übergebenen Parametern und
		/// erzeugt einen LogEvent für den Methodeneintritt, falls Tracing 
		/// für den <see cref="Level"/> <paramref name="level"/> konfiguriert ist.
		/// Mit dem Parameter <paramref name="message"/> kann zusammen mit 
		/// dem Methodeneintritt weitere Information ausgegeben werden.
		/// </summary>
		/// <param name="traceLog">zugehöriger Trace-Logger</param>
		/// <param name="level">gewünschter Log-Level</param>		
		/// <param name="message">weitere Information als <c>object</c></param>
		/// <seealso cref="Dispose"/>
		public EnterExitLogger(ITraceLog traceLog, Level level, object message) : this(4, traceLog, level, message) {
		}


		/// <summary>
		/// Der Konstruktor initialisiert die Instanz mit den übergebenen Parametern und
		/// erzeugt einen LogEvent für den Methodeneintritt, falls Tracing 
		/// für den <see cref="DefaultLevel"/> konfiguriert ist.
		/// Mit dem Parameter <paramref name="message"/> kann zusammen mit 
		/// dem Methodeneintritt weitere Information ausgegeben werden.
		/// </summary>
		/// <param name="traceLog">zugehöriger Trace-Logger.</param>
		/// <param name="message">weitere Information als <c>object</c>.</param>
		/// <seealso cref="Dispose"/>
		public EnterExitLogger(ITraceLog traceLog, object message) : this(4, traceLog, DefaultLevel, message) {
		}

		/// <summary>
		/// Der Konstruktor initialisiert die Instanz mit den übergebenen Parametern und
		/// erzeugt einen LogEvent für den Methodeneintritt, falls Tracing 
		/// für den <see cref="DefaultLevel"/> konfiguriert ist.
		/// </summary>
		/// <param name="traceLog">zugehöriger Trace-Logger.</param>
		/// <param name="level">Der Trace-Level.</param>
		/// <seealso cref="Dispose"/>
		public EnterExitLogger(ITraceLog traceLog, Level level) : this(4, traceLog, level, string.Empty) {
		}

		/// <summary>
		/// Der Konstruktor initialisiert die Instanz mit den übergebenen Parametern und
		/// erzeugt einen LogEvent für den Methodeneintritt, falls Tracing 
		/// für den Default-Level <see cref="DefaultLevel"/> konfiguriert ist.
		/// Es wird nur der Methodeneintritt ausgegeben.
		/// </summary>
		/// <param name="traceLog">zugehöriger Trace-Logger</param>
		/// <seealso cref="Dispose"/>
		public EnterExitLogger(ITraceLog traceLog) : this(4, traceLog, DefaultLevel, string.Empty) {
		}


		#endregion


		#endregion Public Konstruktoren


		#region Implementierung von IDisposable


		/// <summary>
		/// Dispose gibt einen Exit-Trace mit den im Konstruktor übergebenen Parametern aus, 
		/// falls Tracing für den im Konstruktor angegebenen <see cref="Level"/> konfiguriert ist.
		/// </summary>
		public void Dispose() {
			if (IsEnabledFor(m_level)) {
				m_timeEntered.Stop();

				LogProperty(EnterExit.Exit, 2, m_timeEntered.TimeSpan, m_level);
			}
		}

		#endregion Implementation of IDisposable

		#region Implementierung von ILog

		#region Debug
		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.Debug"/> Level.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Die Methode prüft zunächst, ob dieser Logger für den Level 
		/// <see cref="T:Level.Debug"/> aktiviert ist.
		/// Falls der Logger aktiviert ist, gibt er die entsprechende Information
		/// mit Hilfe der Methode <cref name="LogProperty"/> auf die die konfigurierten
		/// <cref name="Appender"/> aus.
		/// </para>
		/// <para><b>Hinweis</b>Die Übergabe eines <see cref="Exception"/> Objekts gibt
		/// nur den Namen der Exception, aber keinen Stacktrace aus.
		/// Für die Ausgabe eines Stacktraces muss man die Variante
		/// <see cref="Debug(object,Exception)"/> verwenden.
		/// </para>
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <seealso cref="Debug(object,Exception)"/>
		/// <seealso cref="IsDebugEnabled"/>
		public void Debug(object message) {
			if (IsDebugEnabled) {                
				LogProperty(EnterExit.Log, 3, LevelDebug, message);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.Debug"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsDebugEnabled"/>
		public void Debug(object message, Exception exc) {
			if (IsDebugEnabled) {
				LogProperty(EnterExit.Log, 3, LevelDebug, message, exc);
			}
		}
		#endregion

		#region Trace

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.TRACE"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsTraceEnabled"/>
		public void Trace(object message) {
			if (IsTraceEnabled) {
				LogProperty(EnterExit.Log, 3, LevelTrace, message);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.TRACE"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsTraceEnabled"/>
		public void Trace(object message, Exception exc) {
			if (IsTraceEnabled) {
				LogProperty(EnterExit.Log, 3, LevelTrace, message, exc);
			}
		}
		#endregion

		#region Info
		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.Info"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsInfoEnabled"/>
		public void Info(object message) {
			if (IsInfoEnabled) {
				LogProperty(EnterExit.Log, 3, LevelInfo, message);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.Info"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsInfoEnabled"/>
		public void Info(object message, Exception exc) {
			if (IsInfoEnabled) {
				LogProperty(EnterExit.Log, 3, LevelInfo, message, exc);
			}
		}
		#endregion

		#region	Warn
		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.Warn"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsWarnEnabled"/>
		public void Warn(object message) {
			if (IsWarnEnabled) {
				LogProperty(EnterExit.Log, 3, LevelWarn, message);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.WARN"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsWarnEnabled"/>
		public void Warn(object message, Exception exc) {
			if (IsWarnEnabled) {
				LogProperty(EnterExit.Log, 3, LevelWarn, message, exc);
			}
		}
		#endregion

		#region	Error
		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.ERROR"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsErrorEnabled"/>
		public void Error(object message) {
			if (IsErrorEnabled) {
				LogProperty(EnterExit.Log, 3, LevelError, message);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.ERROR"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsErrorEnabled"/>
		public void Error(object message, Exception exc) {
			if (IsErrorEnabled) {
				LogProperty(EnterExit.Log, 3, LevelError, message, exc);
			}
		}
		#endregion

		#region	Fatal
		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.FATAL"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsFatalEnabled"/>
		public void Fatal(object message) { 
			if (IsFatalEnabled) {
				LogProperty(EnterExit.Log, 3, LevelFatal, message);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.FATAL"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsFatalEnabled"/>
		public void Fatal(object message, Exception exc) { 
			if (IsFatalEnabled) {
				LogProperty(EnterExit.Log, 3, LevelFatal, message, exc);
			}
		}
		#endregion

		#region Properties
		/// <summary>
		/// Prüft, ob der dieser Logger für den <c>DEBUG</c>-Level konfiguriert ist.
		/// </summary>
		/// <value>
		/// <c>true</c> falls der Logger für <c>DEBUG</c> Events konfiguriert ist,
		/// <c>false</c> sonst.
		/// </value>
		/// <remarks>
		/// <para>
		/// Diese Property kann helfen, um den Aufwand für eine nicht konfigurierte Testausgabe
		/// zu minimieren.
		/// </para>
		/// <para>
		/// Falls man beispielsweise folgende Debug-Testausgabe vorsieht,
		/// </para>
		/// <code>
		/// log.Debug("This is entry number: " + i );
		/// </code>
		/// <para>
		/// hat man in jedem Fall den Aufwand für die String-Konkatenation und den Aufruf der Methode, 
		/// unabhängig davon, ob der <c>DEBUG</c>-Level konfiguriert ist oder nicht.
		/// </para>
		/// <para>
		/// Falls Performance ein wesentlicher Faktor ist, sollte man folgenden Test einbauen:
		/// </para>
		/// <code>
		///		if (log.IsDebugEnabled) { 
		///			log.Debug("This is entry number: " + i );
		///		}
		/// </code>
		/// <para>
		/// In diesem Fall entfällt der zusätzliche Aufwand für Aufbereitung der 
		/// Aufrufparameter und den eigentlichen Methodenaufruf, falls keine Testausgaben
		/// konfiguriert sind. Andererseits erfolgt dann der Test doppelt, falls Testausgaben 
		/// erfolgen sollen. Dieser zusätzliche Aufwand für den Test ist aber normalerweise
		/// gegenüber der eigentlichen Testausgabe vernachlässigbar.
		/// </para>
		/// </remarks>
		public bool IsDebugEnabled {
			get {
				if (m_loggingConfigurationVersion < s_loggingConfigurationVersion) {
					UpdateLoggingConfiguration();
				}
				return m_isDebugEnabled;
			}
		}


		/// <summary>
		/// Prüft, ob der dieser Logger für den <c>DEBUG</c>-Level konfiguriert ist.
		/// </summary>
		/// <value>
		/// <c>true</c> falls der Logger für <c>TRACE</c> Events konfiguriert ist,
		/// <c>false</c> sonst.	
		/// </value>
		/// <remarks>
		/// Siehe auch <see cref="IsDebugEnabled"/> für weitere Information und ein Anwendungsbeispiel.
		/// </remarks>
		/// <seealso cref="LogImpl.IsDebugEnabled"/>
		public bool IsTraceEnabled {
			get {
				if (m_loggingConfigurationVersion < s_loggingConfigurationVersion) {
					UpdateLoggingConfiguration();
				}
				return m_isTraceEnabled;
			}
		}

		/// <summary>
		/// Prüft, ob der dieser Logger für den <c>INFO</c>-Level konfiguriert ist.
		/// </summary>
		/// <value>
		/// <c>true</c> falls der Logger für <c>INFO</c> Events konfiguriert ist,
		/// <c>false</c> sonst.		
		/// </value>
		/// <remarks>
		/// Siehe auch <see cref="IsDebugEnabled"/> für weitere Information und ein Anwendungsbeispiel.
		/// </remarks>
		/// <seealso cref="LogImpl.IsDebugEnabled"/>
		public bool IsInfoEnabled {
			get {
				if (m_loggingConfigurationVersion < s_loggingConfigurationVersion) {
					UpdateLoggingConfiguration();
				}
				return m_isInfoEnabled;
			}
		}

		/// <summary>
		/// Prüft, ob der dieser Logger für den <c>WARN</c>-Level konfiguriert ist.
		/// </summary>
		/// <value>
		/// <c>true</c> falls der Logger für <c>WARN</c> Events konfiguriert ist,
		/// <c>false</c> sonst.		
		/// </value>
		/// <remarks>
		/// Siehe auch <see cref="IsDebugEnabled"/> für weitere Information und ein Anwendungsbeispiel.
		/// </remarks>
		/// <seealso cref="LogImpl.IsDebugEnabled"/>
		public bool IsWarnEnabled {
			get {
				if (m_loggingConfigurationVersion < s_loggingConfigurationVersion) {
					UpdateLoggingConfiguration();
				}
				return m_isWarnEnabled;
			}
		}

		/// <summary>
		/// Prüft, ob der dieser Logger für den <c>ERROR</c>-Level konfiguriert ist.
		/// </summary>
		/// <value>
		/// <c>true</c> falls der Logger für <c>ERROR</c> Events konfiguriert ist,
		/// <c>false</c> sonst.		
		/// </value>
		/// <remarks>
		/// Siehe auch <see cref="IsDebugEnabled"/> für weitere Information und ein Anwendungsbeispiel.
		/// </remarks>
		/// <seealso cref="LogImpl.IsDebugEnabled"/>
		public bool IsErrorEnabled {
			get {
				if (m_loggingConfigurationVersion < s_loggingConfigurationVersion) {
					UpdateLoggingConfiguration();
				}
				return m_isErrorEnabled;
			}
		}

		/// <summary>
		/// Prüft, ob der dieser Logger für den <c>FATAL</c>-Level konfiguriert ist.
		/// </summary>
		/// <value>
		/// <c>true</c> falls der Logger für <c>FATAL</c> Events konfiguriert ist,
		/// <c>false</c> sonst.		
		/// </value>
		/// <remarks>
		/// Siehe auch <see cref="IsDebugEnabled"/> für weitere Information und ein Anwendungsbeispiel.
		/// </remarks>
		/// <seealso cref="LogImpl.IsDebugEnabled"/>
		public bool IsFatalEnabled {
			get {
				if (m_loggingConfigurationVersion < s_loggingConfigurationVersion) {
					UpdateLoggingConfiguration();
				}
				return m_isFatalEnabled;
			}
		}
		#endregion Properties

		#endregion Implementierung von ILog


		#region Implementierung von ITraceLog

		
		#region Log

		#region Log mit Level
		/// <summary>
		/// Schreibt einen Log-Eintrag für den angegebenen <see cref="Level"/> <paramref name="level"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Die Methode prüft zunächst, ob dieser Logger für den angegebenen Level 
		/// <paramref name="level"/> aktiviert ist.
		/// Falls der Logger aktiviert ist, gibt er die entsprechende Information
		/// mit Hilfe der Methode <cref name="LogProperty"/> auf die konfigurierten
		/// <cref name="Appender"/> aus.
		/// </para>
		/// <para><b>Hinweis</b>Die Übergabe eines <see cref="Exception"/> Objekts gibt
		/// nur den Namen der Exception, aber keinen Stacktrace aus.
		/// Für die Ausgabe eines Stacktraces muss man die Variante
		/// <see cref="Log(Level,object,Exception)"/> verwenden.
		/// </para>
		/// </remarks>
		/// <param name="level">Der Log-Level.</param>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <seealso cref="Log(Level,object,Exception)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		public void Log(Level level, object message) {
			if (IsEnabledFor(level)) {
				LogProperty(EnterExit.Log, 3, level, message);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag <paramref name="message"/> für den angegebenen 
		/// <see cref="Level"/> <paramref name="level"/> zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="level">Der Log-Level.</param>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		public void Log(Level level, object message, Exception exc) {
			if (IsEnabledFor(level)) {
				LogProperty(EnterExit.Log, 3, level, message, exc);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag <paramref name="message"/> für den angegebenen 
		/// <see cref="Level"/> <paramref name="level"/> zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="level">Der Log-Level.</param>
		/// <param name="message">Die auszugebende Meldung.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		public void Log(Level level, string message, Exception exc) {
			if (IsEnabledFor(level)) {
				LogProperty(EnterExit.Log, 3, level, message, exc);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag über <paramref name="format"/> und <paramref name="args"/> 
		/// für den angegebenen <see cref="Level"/> <paramref name="level"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="level">Der Log-Level.</param>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		public void Log(Level level, string format, params object[] args) {
			if (IsEnabledFor(level)) {
				LogProperty(EnterExit.Log, 3, level, format, args);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag über <paramref name="format"/> und <paramref name="args"/>  
		/// für den angegebenen <see cref="Level"/> <paramref name="level"/> zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="level">Der Log-Level.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		public void Log(Level level, Exception exc, string format, params object[] args) {
			if (IsEnabledFor(level)) {
				LogProperty(EnterExit.Log, 3, level, exc, format, args);
			}
		}
		#endregion


		#region Log mit EnterExitLogger-Level

		/// <summary>
		/// Schreibt einen Log-Eintrag für den angegebenen <see cref="Level"/> <paramref name="level"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Die Methode prüft zunächst, ob dieser Logger für den internen Level aktiviert ist.
		/// Falls der Logger aktiviert ist, gibt er die entsprechende Information
		/// mit Hilfe der Methode <cref name="LogProperty"/> auf die konfigurierten
		/// <cref name="Appender"/> aus.
		/// </para>
		/// <para><b>Hinweis</b>Die Übergabe eines <see cref="Exception"/> Objekts gibt
		/// nur den Namen der Exception, aber keinen Stacktrace aus.
		/// Für die Ausgabe eines Stacktraces muss man die Variante
		/// <see cref="Log(Level,object,Exception)"/> verwenden.
		/// </para>
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <seealso cref="Log(Level,object,Exception)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		public void Log(object message) {
			if (IsEnabledFor(Level)) {
				LogProperty(EnterExit.Log, 3, Level, message);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag <paramref name="message"/> für den internen Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		public void Log(object message, Exception exc) {
			if (IsEnabledFor(Level)) {
				LogProperty(EnterExit.Log, 3, Level, message, exc);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag <paramref name="message"/> für den internen Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Die auszugebende Meldung.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		public void Log(string message, Exception exc) {
			if (IsEnabledFor(Level)) {
				LogProperty(EnterExit.Log, 3, Level, message, exc);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag über <paramref name="format"/> und <paramref name="args"/> 
		/// für den internen Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		public void Log(string format, params object[] args) {
			if (IsEnabledFor(Level)) {
				LogProperty(EnterExit.Log, 3, Level, format, args);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag über <paramref name="format"/> und <paramref name="args"/>  
		/// für den internen Level zusammen mit dem Stacktrace der angegebenen 
		/// <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		public void Log(Exception exc, string format, params object[] args) {
			if (IsEnabledFor(Level)) {
				LogProperty(EnterExit.Log, 3, Level, exc, format, args);
			}
		}
		#endregion

		#endregion



		#region Debug Methoden

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.Debug"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsDebugEnabled"/>
		public void Debug(string message, Exception exc) {
			if (IsDebugEnabled) {
				LogProperty(EnterExit.Log, 3, LevelDebug, message, exc);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.Debug"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsDebugEnabled"/>
		public void Debug(string format, params object[] args) {
			if (IsDebugEnabled) {
				LogProperty(EnterExit.Log, 3, LevelDebug, format, args);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.Debug"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsDebugEnabled"/>
		public void Debug(Exception exc, string format, params object[] args) {
			if (IsDebugEnabled) {
				LogProperty(EnterExit.Log, 3, LevelDebug, exc, format, args);
			}
		}
		#endregion

		#region Trace Methoden

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.TRACE"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsTraceEnabled"/>
		public void Trace(string format, params object[] args) {
			if (IsTraceEnabled) {
				LogProperty(EnterExit.Log, 3, LevelTrace, format, args);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.TRACE"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsTraceEnabled"/>
		public void Trace(Exception exc, string format, params object[] args) {
			if (IsTraceEnabled) {
				LogProperty(EnterExit.Log, 3, LevelTrace, exc, format, args);
			}
		}
		#endregion

		#region Info Methoden
	
		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.Info"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsInfoEnabled"/>
		public void Info(string format, params object[] args) {
			if (IsInfoEnabled) {
				LogProperty(EnterExit.Log, 3, LevelInfo, format, args);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.Info"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsInfoEnabled"/>
		public void Info(Exception exc, string format, params object[] args) {
			if (IsInfoEnabled) {
				LogProperty(EnterExit.Log, 3, LevelInfo, exc, format, args);
			}
		}
		#endregion
	
		#region Warn Methoden
	
		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.WARN"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsWarnEnabled"/>
		public void Warn(string format, params object[] args) {
			if (IsWarnEnabled) {
				LogProperty(EnterExit.Log, 3, LevelWarn, format, args);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.WARN"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsWarnEnabled"/>
		public void Warn(Exception exc, string format, params object[] args) {
			if (IsWarnEnabled) {
				LogProperty(EnterExit.Log, 3, LevelWarn, exc, format, args);
			}
		}
		#endregion

		#region Error Methoden

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.ERROR"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsErrorEnabled"/>
		public void Error(string format, params object[] args) {
			if (IsErrorEnabled) {
				LogProperty(EnterExit.Log, 3, LevelError, format, args);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.ERROR"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsErrorEnabled"/>
		public void Error(Exception exc, string format, params object[] args) {
			if (IsErrorEnabled) {
				LogProperty(EnterExit.Log, 3, LevelError, exc, format, args);
			}
		}
		#endregion

		#region Fatal Methoden

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.FATAL"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsFatalEnabled"/>
		public void Fatal(string format, params object[] args) { 
			if (IsFatalEnabled) {
				LogProperty(EnterExit.Log, 3, LevelFatal, format, args);
			}
		}

		/// <summary>
		/// Schreibt einen Log-Eintrag für den <see cref="T:Level.FATAL"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Debug(object)"/> für weitere Erläuterungen.
		/// </remarks>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring für die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Debug(object)"/>
		/// <seealso cref="IsFatalEnabled"/>
		public void Fatal(Exception exc, string format, params object[] args) { 
			if (IsFatalEnabled) {
				LogProperty(EnterExit.Log, 3, LevelFatal, exc, format, args);
			}
		}
		#endregion

		#endregion Implementation of ITraceLog

		#region public Properties

		/// <summary>
		/// Der aktuelle Logger
		/// </summary>
		/// <value>Liefert den aktuellen Logger</value>
		public ILogger Logger {
			get { return m_traceLog.Logger; }
		}


		public Level LevelEnabledMin {
			get {
				if (m_loggingConfigurationVersion < s_loggingConfigurationVersion) {
					UpdateLoggingConfiguration();
				}
				return m_levelEnabledMin;
			}
		}

		public Level LevelDebug {
			get {
				if (m_loggingConfigurationVersion < s_loggingConfigurationVersion) {
					UpdateLoggingConfiguration();
				}
				return m_levelDebug;
			}
		}

		public Level LevelInfo {
			get {
				if (m_loggingConfigurationVersion < s_loggingConfigurationVersion) {
					UpdateLoggingConfiguration();
				}
				return m_levelInfo;
			}
		}

		public Level LevelWarn {
			get {
				if (m_loggingConfigurationVersion < s_loggingConfigurationVersion) {
					UpdateLoggingConfiguration();
				}
				return m_levelWarn;
			}
		}

		public Level LevelTrace {
			get {
				if (m_loggingConfigurationVersion < s_loggingConfigurationVersion) {
					UpdateLoggingConfiguration();
				}
				return m_levelTrace;
			}
		}

		public Level LevelError {
			get {
				if (m_loggingConfigurationVersion < s_loggingConfigurationVersion) {
					UpdateLoggingConfiguration();
				} 
				return m_levelError;
			}
		}

		public Level LevelFatal {
			get {
				if (m_loggingConfigurationVersion < s_loggingConfigurationVersion) {
					UpdateLoggingConfiguration();
				}
				return m_levelFatal;
			}
		}


		#endregion


		#region private Memberfunktionen

		/// <summary>
		/// Diese Methode erzeugt einen <cref name="LoggingEvent"/> für die angegebenen Parameter
		/// und übergibt diesen an den aktuellen <cref name="Logger"/>, falls das
		/// Logging für den <cref name="Level"/>
		/// 	<paramref name="level"/> konfiguriert ist.
		/// </summary>
		/// <param name="enterExit">The enter exit.</param>
		/// <param name="framesToSkip">The frames to skip.</param>
		/// <param name="elapsed">The elapsed.</param>
		/// <param name="level">The level.</param>
		private void LogProperty(EnterExit enterExit, int framesToSkip, TimeSpan elapsed, Level level) {
			if (IsEnabledFor(level)) {
				if (m_useMethodbase) {
					BuildStackInfo(framesToSkip);
				}
				var loggingEvent = new LoggingEventWrapper(enterExit, Method, elapsed,
					CallerStackDeclaringType, LoggerRepository, LoggerName, level);
				
				s_loggingEventQueue.Enqueue(m_traceLog, loggingEvent);
			}
		}


		/// <summary>
		/// Diese Methode erzeugt einen <cref name="LoggingEvent"/> für die angegebenen Parameter
		/// und übergibt diesen an den aktuellen <cref name="Logger"/>, falls das
		/// Logging für den <cref name="Level"/>
		/// 	<paramref name="level"/> konfiguriert ist.
		/// </summary>
		/// <param name="enterExit">The enter exit.</param>
		/// <param name="framesToSkip">The frames to skip.</param>
		/// <param name="level">The level.</param>
		/// <param name="message">The message.</param>
		/// <param name="exc">The exc.</param>
		private void LogProperty(EnterExit enterExit, int framesToSkip, Level level, object message, Exception exc) {
			if (IsEnabledFor(level)) {
				if (m_useMethodbase) {
					BuildStackInfo(framesToSkip);
				}
				var loggingEvent = new LoggingEventWrapper(enterExit, Method,
					CallerStackDeclaringType, LoggerRepository, LoggerName, level, message, exc);

				s_loggingEventQueue.Enqueue(m_traceLog, loggingEvent);
			}
		}

		/// <summary>
		/// Diese Methode erzeugt einen <cref name="LoggingEvent"/> für die angegebenen Parameter
		/// und übergibt diesen an den aktuellen <cref name="Logger"/>, falls das
		/// Logging für den <cref name="Level"/>
		/// 	<paramref name="level"/> konfiguriert ist.
		/// </summary>
		/// <param name="enterExit">The enter exit.</param>
		/// <param name="framesToSkip">The frames to skip.</param>
		/// <param name="level">The level.</param>
		/// <param name="message">The message.</param>
		private void LogProperty(EnterExit enterExit, int framesToSkip, Level level, object message) {
			if (IsEnabledFor(level)) {
				if (m_useMethodbase) {
					BuildStackInfo(framesToSkip);
				}
				var loggingEvent = new LoggingEventWrapper(enterExit, Method,
					CallerStackDeclaringType, LoggerRepository, LoggerName, level, message, null);

				s_loggingEventQueue.Enqueue(m_traceLog, loggingEvent);
			}
		}

		/// <summary>
		/// Diese Methode erzeugt einen <cref name="LoggingEvent"/> für die angegebenen Parameter
		/// und übergibt diesen an den aktuellen <cref name="Logger"/>, falls das
		/// Logging für den <cref name="Level"/>
		/// 	<paramref name="level"/> konfiguriert ist.
		/// </summary>
		/// <param name="enterExit">The enter exit.</param>
		/// <param name="framesToSkip">The frames to skip.</param>
		/// <param name="level">The level.</param>
		/// <param name="message">The message.</param>
		/// <param name="exc">The exc.</param>
		private void LogProperty(EnterExit enterExit, int framesToSkip, Level level, string message, Exception exc) {
			if (IsEnabledFor(level)) {
				if (m_useMethodbase) {
					BuildStackInfo(framesToSkip);
				}
				var loggingEvent = new LoggingEventWrapper(enterExit, Method, 
					CallerStackDeclaringType, LoggerRepository, LoggerName, level, message, exc);

				s_loggingEventQueue.Enqueue(m_traceLog, loggingEvent);
			}
		}

		/// <summary>
		/// Diese Methode erzeugt einen <cref name="LoggingEvent"/> für die angegebenen Parameter
		/// und übergibt diesen an den aktuellen <cref name="Logger"/>, falls das
		/// Logging für den <cref name="Level"/>
		/// 	<paramref name="level"/> konfiguriert ist.
		/// </summary>
		/// <param name="enterExit">The enter exit.</param>
		/// <param name="framesToSkip">The frames to skip.</param>
		/// <param name="level">The level.</param>
		/// <param name="message">The message.</param>
		private void LogProperty(EnterExit enterExit, int framesToSkip, Level level, string message) {
			if (IsEnabledFor(level)) {
				if (m_useMethodbase) {
					BuildStackInfo(framesToSkip);
				}
				var loggingEvent = new LoggingEventWrapper(enterExit, Method, 
					CallerStackDeclaringType, LoggerRepository, LoggerName, level, message, null);

				s_loggingEventQueue.Enqueue(m_traceLog, loggingEvent);
			}
		}

		/// <summary>
		/// Diese Methode erzeugt einen <cref name="LoggingEvent"/> für die angegebenen Parameter
		/// und übergibt diesen an den aktuellen <cref name="Logger"/>, falls das
		/// Logging für den <cref name="Level"/>
		/// 	<paramref name="level"/> konfiguriert ist.
		/// </summary>
		/// <param name="enterExit">The enter exit.</param>
		/// <param name="framesToSkip">The frames to skip.</param>
		/// <param name="level">The level.</param>
		/// <param name="format">The format.</param>
		/// <param name="args">The args.</param>
		private void LogProperty(EnterExit enterExit, int framesToSkip, Level level, string format, params object[] args) {
			if (IsEnabledFor(level)) {
				if (m_useMethodbase) {
					BuildStackInfo(framesToSkip);
				}
				var loggingEvent = new LoggingEventWrapper(enterExit, Method, 
					CallerStackDeclaringType, LoggerRepository, LoggerName, level, null, format, args);

				s_loggingEventQueue.Enqueue(m_traceLog, loggingEvent);
			}
		}

		/// <summary>
		/// Diese Methode erzeugt einen <cref name="LoggingEvent"/> für die angegebenen Parameter
		/// und übergibt diesen an den aktuellen <cref name="Logger"/>, falls das
		/// Logging für den <cref name="Level"/>
		/// 	<paramref name="level"/> konfiguriert ist.
		/// </summary>
		/// <param name="enterExit">The enter exit.</param>
		/// <param name="framesToSkip">The frames to skip.</param>
		/// <param name="level">The level.</param>
		/// <param name="exc">The exc.</param>
		/// <param name="format">The format.</param>
		/// <param name="args">The args.</param>
		private void LogProperty(EnterExit enterExit, int framesToSkip, Level level, Exception exc, string format, params object[] args) {
			if (IsEnabledFor(level)) {
				if (m_useMethodbase) {
					BuildStackInfo(framesToSkip);
				}

				var loggingEvent = new LoggingEventWrapper(enterExit, Method,
					CallerStackDeclaringType, LoggerRepository, LoggerName, level, exc, format, args);

				s_loggingEventQueue.Enqueue(m_traceLog, loggingEvent);
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
                if (String.IsNullOrEmpty(m_method)) {
					return "-unknown-";
				}
				return m_method; 
			}
		}


		private Type CallerStackDeclaringType {
			get { return m_traceLog.DeclaringType; }
		}
	
		/// <summary>
		/// Das aktuelle LoggerRepository
		/// </summary>
		/// <value>Liefert das aktuelle LoggerRepository</value>
		private ILoggerRepository LoggerRepository {
			get { return Logger.Repository; }
		}

		/// <summary>
		/// Der aktuelle LoggerName
		/// </summary>
		/// <value>Liefert den aktuelle LoggerNamen</value>
		private string LoggerName {
			get { return Logger.Name; }
		}

		/// <summary>
		/// Der aktuelle Logger-Level.
		/// </summary>
		/// <value>Liefert den aktuelle Logger-Level</value>
		private Level Level {
			get { return m_level; }
		}

		#endregion

        #region ILoggerWrapper Member

        ILogger ILoggerWrapper.Logger {
            get { return Logger; }
        }

        #endregion
       

        #region ILog Member

        #region DebugFormat 

        public void DebugFormat(IFormatProvider provider, string format, params object[] args) {
            if (IsDebugEnabled) {
                Logger.Log(s_declaringType, LevelDebug, new SystemStringFormat(provider, format, args), null);
            }
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2) {
            if (IsDebugEnabled) {
                Logger.Log(s_declaringType, LevelDebug, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
            }
        }

        public void DebugFormat(string format, object arg0, object arg1) {
            if (IsDebugEnabled) {
                Logger.Log(s_declaringType, LevelDebug, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0, arg1), null);
            }
        }

        public void DebugFormat(string format, object arg0) {
            if (IsDebugEnabled) {
                Logger.Log(s_declaringType, LevelDebug, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0), null);
            }
        }

        public void DebugFormat(string format, params object[] args) {
            if (IsDebugEnabled) {
				Logger.Log(s_declaringType, LevelDebug, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
            }
        }
        #endregion

        #region ErrorFormat

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args) {
			Logger.Log(s_declaringType, LevelError, new SystemStringFormat(provider, format, args), null);
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2) {
            Logger.Log(s_declaringType, LevelError, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0, arg2), null);
        }

        public void ErrorFormat(string format, object arg0, object arg1) {
			Logger.Log(s_declaringType, LevelError, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0, arg1), null);
        }

        public void ErrorFormat(string format, object arg0) {
			Logger.Log(s_declaringType, LevelError, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0), null);
		}

        public void ErrorFormat(string format, params object[] args) {
			Logger.Log(s_declaringType, LevelError, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
		}
        #endregion

		#region FatalFormat
		public void FatalFormat(IFormatProvider provider, string format, params object[] args) {
			Logger.Log(s_declaringType, LevelFatal, new SystemStringFormat(provider, format, args), null);
		}

        public void FatalFormat(string format, object arg0, object arg1, object arg2) {
			Logger.Log(s_declaringType, LevelFatal, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
		}

        public void FatalFormat(string format, object arg0, object arg1) {
			Logger.Log(s_declaringType, LevelFatal, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0, arg1), null);
		}

        public void FatalFormat(string format, object arg0) {
			Logger.Log(s_declaringType, LevelFatal, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0), null);
		}

        public void FatalFormat(string format, params object[] args) {
			Logger.Log(s_declaringType, LevelFatal, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
		}

#endregion

		#region InfoFormat
		public void InfoFormat(IFormatProvider provider, string format, params object[] args) {
			if (IsInfoEnabled) {
				Logger.Log(s_declaringType, LevelInfo, new SystemStringFormat(provider, format, args), null);
			}
		}

        public void InfoFormat(string format, object arg0, object arg1, object arg2) {
			if (IsInfoEnabled) {
				Logger.Log(s_declaringType, LevelInfo, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
			}
		}

        public void InfoFormat(string format, object arg0, object arg1) {
			if (IsInfoEnabled) {
				Logger.Log(s_declaringType, LevelInfo, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0, arg1), null);
			}
        }

        public void InfoFormat(string format, object arg0) {
			if (IsInfoEnabled) {
				Logger.Log(s_declaringType, LevelInfo, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0), null);
			}
        }

        public void InfoFormat(string format, params object[] args) {
			if (IsInfoEnabled) {
				Logger.Log(s_declaringType, LevelInfo, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
			}
		}
		#endregion

		#region WarnFormat
		public void WarnFormat(IFormatProvider provider, string format, params object[] args) {
			if (IsWarnEnabled) {
				Logger.Log(s_declaringType, LevelWarn, new SystemStringFormat(provider, format, args), null);
			}
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2) {
			if (IsWarnEnabled) {
				Logger.Log(s_declaringType, LevelWarn, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
			}
        }

        public void WarnFormat(string format, object arg0, object arg1) {
			if (IsWarnEnabled) {
				Logger.Log(s_declaringType, LevelWarn, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0, arg1), null);
			}
        }

        public void WarnFormat(string format, object arg0) {
			if (IsWarnEnabled) {
				Logger.Log(s_declaringType, LevelWarn, new SystemStringFormat(CultureInfo.InvariantCulture, format, arg0), null);
			}
        }

        public void WarnFormat(string format, params object[] args) {
			if (IsWarnEnabled) {
				Logger.Log(s_declaringType, LevelWarn, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
			}
		}
		#endregion

		#endregion

		public event LevelsReloadedEventHandler LevelsReloaded;

		//protected virtual void OnLevelsReloaded(LevelsReloadedEventArgs e) {
		//    if (LevelsReloaded != null) {
		//        LevelsReloaded(this, e);
		//    }
		//}


		private void BuildStackInfo(int framesToSkip) {
			if (m_methodBase == null) {
				//
				// wir holen einen Stacktrace ohne detaillierte Information (nur Methodeninformation)
				// und ermitteln daraus den Namen der aufrufenden Methode
				//
				var st = new StackTrace(framesToSkip, false);
				m_methodBase = st.GetFrame(0).GetMethod();
				m_method = m_methodBase.Name;
			}
		}


		/// <summary>
		/// Updates the logging configuration.
		/// </summary>
		private void UpdateLoggingConfiguration() {
			m_levelEnabledMin = m_traceLog.LevelEnabledMin;
			m_levelTrace = m_traceLog.LevelTrace;
			m_levelDebug = m_traceLog.LevelDebug;
			m_levelInfo = m_traceLog.LevelInfo;
			m_levelWarn = m_traceLog.LevelWarn;
			m_levelError = m_traceLog.LevelError;
			m_levelFatal = m_traceLog.LevelFatal;

			m_isDebugEnabled = m_traceLog.IsEnabledFor(m_levelDebug);
			m_isInfoEnabled = m_traceLog.IsEnabledFor(m_levelInfo);
			m_isTraceEnabled = m_traceLog.IsEnabledFor(m_levelTrace);
			m_isWarnEnabled = m_traceLog.IsEnabledFor(m_levelWarn);
			m_isErrorEnabled = m_traceLog.IsEnabledFor(m_levelError);
			m_isFatalEnabled = m_traceLog.IsEnabledFor(m_levelFatal);


			m_loggingConfigurationVersion = s_loggingConfigurationVersion;
		}


		/// <summary>
		/// Liefert <c>true</c>, falls der Logger für den angebenen Level <paramref name="level"/> loggen soll.
		/// </summary>
		/// <param name="level">Der Log-Level.</param>
		/// <returns>
		/// 	<c>true</c>, falls für den angebenen Level enabled; sonst <c>false</c>.
		/// </returns>
		private bool IsEnabledFor(Level level) {
			if (level >= LevelEnabledMin) {
				return true;
			}
			return false;
		}		
	}

	/// <summary>
	/// Log-Typ
	/// </summary>
	public enum EnterExit {
		/// <summary>
		/// Log für Methodeneintritt
		/// </summary>
		Enter = 0,

		/// <summary>
		/// Log für Methodenaustritt
		/// </summary>
		Exit = 1,

		/// <summary>
		/// Sonstiger Log (Warn, Error, ...)
		/// </summary>
		Log = 2
	}
}