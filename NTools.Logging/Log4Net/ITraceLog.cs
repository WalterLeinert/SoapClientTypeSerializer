//- Standard Namespaces --------------------------------------------------
using System;
using System.Runtime.InteropServices;

//- Zus�tzliche Namespaces -----------------------------------------------
using log4net;
using log4net.Core;



namespace NTools.Logging.Log4Net {

	/// <summary>
	/// Das Interface <see cref="ITraceLog"/> wird verwendet f�r das Logging 
	/// mit dem log4net-Framework.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Mit Hilfe des <see cref="TraceLogManager"/> erh�lt man Instanzen von Objekten,
	/// die das Interface implementieren. Die statische Methode <see cref="TraceLogManager.GetLogger"/>
	/// liefert Logger-Instanzen.
	/// </para>
	/// <para>
	/// Das Interface umfasst Methoden f�r die Ausgabe bei verschiedenen Log-Leveln. 
	/// Ausserdem gibt es Properties f�r einige Log-Level, mit deren Hilfe man testen kann,
	/// ob das Logging f�r einen Level �berhaupt konfiguriert ist.
	/// </para>
	/// </remarks>
	/// <example>Beispiel:
	/// <code>
	///		ITraceLog log = TraceLogManager.GetLogger("application-log");
	/// 
	///		log.Info("Application Start");
	///		log.Debug("This is a debug message");
	/// 
	///		if (log.IsDebugEnabled) {
	///			log.Debug("This is another debug message");
	///		}
	/// </code>
	/// </example>
	/// <seealso cref="TraceLogManager"/>
	/// <seealso cref="TraceLogManager.GetLogger"/>
	[ComVisible(false)]
	public interface ITraceLog : ILog {
	
		#region Log Methoden

		/// <summary>
		/// Schreibt einen Log-Eintrag f�r den angegebenen <see cref="Level"/> <paramref name="level"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Die Methode pr�ft zun�chst, ob dieser Logger f�r den angegebenen Level 
		/// <paramref name="level"/> aktiviert ist.
		/// Falls der Logger aktiviert ist, gibt er die entsprechende Information
		/// auf die konfigurierten <cref name="Appender"/> aus.
		/// </para>
		/// <para><b>Hinweis</b>Die �bergabe eines <see cref="Exception"/> Objekts gibt
		/// nur den Namen der Exception, aber keinen Stacktrace aus.
		/// F�r die Ausgabe eines Stacktraces muss man die Variante
		/// <see cref="Log(Level,object,Exception)"/> verwenden.
		/// </para>
		/// </remarks>
		/// <param name="level">Der Log-Level.</param>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <seealso cref="Log(Level,object,Exception)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		void Log(Level level, object message);

		/// <summary>
		/// Schreibt einen Log-Eintrag <paramref name="message"/> f�r den angegebenen 
		/// <see cref="Level"/> <paramref name="level"/> zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="level">Der Log-Level.</param>
		/// <param name="message">Das auszugebende Meldungsobjekt.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		void Log(Level level, object message, Exception exc);

		/// <summary>
		/// Schreibt einen Log-Eintrag <paramref name="message"/> f�r den angegebenen 
		/// <see cref="Level"/> <paramref name="level"/> zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="level">Der Log-Level.</param>
		/// <param name="message">Die auszugebende Meldung.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		void Log(Level level, string message, Exception exc);

		/// <summary>
		/// Schreibt einen Log-Eintrag �ber <paramref name="format"/> und <paramref name="args"/> 
		/// f�r den angegebenen <see cref="Level"/> <paramref name="level"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="level">Der Log-Level.</param>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		void Log(Level level, string format, params object[] args);

		/// <summary>
		/// Schreibt einen Log-Eintrag �ber <paramref name="format"/> und <paramref name="args"/>  
		/// f�r den angegebenen <see cref="Level"/> <paramref name="level"/> zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="level">Der Log-Level.</param>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILogger.IsEnabledFor"/>
		void Log(Level level, Exception exc, string format, params object[] args);

		#endregion

		#region Debug Methoden

		/// <summary>
		/// Schreibt einen Log-Eintrag f�r den <see cref="Level.Debug"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILog.IsDebugEnabled"/>
		void Debug(string format, params object[] args) ;

		/// <summary>
		/// Schreibt einen Log-Eintrag f�r den <see cref="Level.Debug"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="Log(Level,object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="Log(Level,object)"/>
		/// <seealso cref="ILog.IsDebugEnabled"/>
		void Debug(Exception exc, string format, params object[] args);
		#endregion

		#region Trace Methoden

		/// <summary>
		/// Schreibt einen Log-Eintrag f�r den <see cref="Level.TRACE"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="ILog.Debug(object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="ILog.Debug(object)"/>
		/// <seealso cref="IsTraceEnabled"/>
		void Trace(string format, params object[] args) ;

		/// <summary>
		/// Schreibt einen Log-Eintrag f�r den <see cref="Level.TRACE"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="ILog.Debug(object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="ILog.Debug(object)"/>
		/// <seealso cref="IsTraceEnabled"/>
		void Trace(Exception exc, string format, params object[] args) ;
		#endregion

		#region Info Methoden
	
		/// <summary>
		/// Schreibt einen Log-Eintrag f�r den <see cref="Level.Info"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="ILog.Debug(object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="ILog.Debug(object)"/>
		/// <seealso cref="ILog.IsInfoEnabled"/>
		void Info(string format, params object[] args) ;

		/// <summary>
		/// Schreibt einen Log-Eintrag f�r den <see cref="Level.Info"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="ILog.Debug(object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="ILog.Debug(object)"/>
		/// <seealso cref="ILog.IsInfoEnabled"/>
		void Info(Exception exc, string format, params object[] args) ;
		#endregion
	
		#region Warn Methoden
	
		/// <summary>
		/// Schreibt einen Log-Eintrag f�r den <see cref="Level.WARN"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="ILog.Debug(object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="ILog.Debug(object)"/>
		/// <seealso cref="ILog.IsWarnEnabled"/>
		void Warn(string format, params object[] args) ;

		/// <summary>
		/// Schreibt einen Log-Eintrag f�r den <see cref="Level.WARN"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="ILog.Debug(object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="ILog.Debug(object)"/>
		/// <seealso cref="ILog.IsWarnEnabled"/>
		void Warn(Exception exc, string format, params object[] args) ;
		#endregion

		#region Error Methoden

		/// <summary>
		/// Schreibt einen Log-Eintrag f�r den <see cref="Level.ERROR"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="ILog.Debug(object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="ILog.Debug(object)"/>
		/// <seealso cref="ILog.IsErrorEnabled"/>
		void Error(string format, params object[] args);

		/// <summary>
		/// Schreibt einen Log-Eintrag f�r den <see cref="Level.ERROR"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="ILog.Debug(object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="ILog.Debug(object)"/>
		/// <seealso cref="ILog.IsErrorEnabled"/>
		void Error(Exception exc, string format, params object[] args);
		#endregion

		#region Fatal Methoden

		/// <summary>
		/// Schreibt einen Log-Eintrag f�r den <see cref="Level.FATAL"/> Level.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="ILog.Debug(object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="ILog.Debug(object)"/>
		/// <seealso cref="ILog.IsFatalEnabled"/>
		void Fatal(string format, params object[] args);

		/// <summary>
		/// Schreibt einen Log-Eintrag f�r den <see cref="Level.FATAL"/> Level zusammen mit
		/// dem Stacktrace der angegebenen <see cref="Exception"/> <paramref name="exc"/>.
		/// </summary>
		/// <remarks>
		/// Siehe auch <see cref="ILog.Debug(object)"/> f�r weitere Erl�uterungen.
		/// </remarks>
		/// <param name="exc">Die auszugebende Exception mit Stacktrace.</param>
		/// <param name="format">Der Formatstring f�r die Formatierung der Argumente <paramref name ="args"/>.</param>
		/// <param name="args">Variable Liste von Argumenten.</param>
		/// <seealso cref="ILog.Debug(object)"/>
		/// <seealso cref="ILog.IsFatalEnabled"/>
		void Fatal(Exception exc, string format, params object[] args);
		
		#endregion

		#region Properties

		Level LevelDebug { get; }
		Level LevelInfo { get; }
		Level LevelWarn { get; }
		Level LevelTrace { get; }
		Level LevelError { get; }
		Level LevelFatal { get; }

		/// <summary>
		/// Liefert true, falls der Trace-Level aktiviert ist.
		/// </summary>
		bool IsTraceEnabled { get; }
		
		#endregion

        event LevelsReloadedEventHandler LevelsReloaded;
	}
}