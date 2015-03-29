//- Standard Namespaces --------------------------------------------------
using System;
using System.Reflection;

//- Zusätzliche Namespaces -----------------------------------------------
using log4net;
using log4net.Core;

//- Projekt Namespaces ---------------------------------------------------


namespace NTools.Logging.Log4Net {

	/// /// <summary>
	/// Statischer Manager, der die Erzeugung von Repositories steuert.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Statischer Manager, der die Erzeugung von Repositories steuert.
	/// </para>
	/// <para>
	/// Diese Klasse wird von den Wrapper-Managern benutzt (z.B. <see cref="log4net.LogManager"/>)
	/// um darüber <see cref="ILogger"/> Objekte zur Verfügung zu stellen.
	/// </para>
	/// </remarks>
	public sealed class TraceLogManager {

		#region Statische Member

		/// <summary>
		/// Diese Map enthält alle <see cref="TraceLog"/> Objekte.
		/// </summary>
		private static readonly WrapperMap s_wrapperMap = new WrapperMap(new WrapperCreationHandler(WrapperCreationHandler));

		#endregion

		#region private Konstruktoren

		/// <summary>
		/// Privater Konstruktor verhindert die Erzeugung von Instanzen.
		/// </summary>
		private TraceLogManager() { }

		#endregion

		#region Type Specific Manager Methods

		/// <summary>
		/// Liefert den angegebenen Logger <paramref name="name"/> falls er existiert.
		/// </summary>
		/// <remarks>
		/// <para>Falls der angegebene Logger (in der Default-Hierarchie) existiert, wird das 
		/// entsprechende Logger-Objekt geliefert, sonst <c>null</c>.</para>
		/// </remarks>
		/// <param name="name">Der voll-qualifizierte Name des Loggers.</param>
		/// <returns>Den entsprechenden Logger oder null.</returns>
		public static ITraceLog Exists(string name) {
			return Exists(Assembly.GetCallingAssembly(), name);
		}

	
		/// <summary>
		/// Liefert den angegebenen Logger <paramref name="name"/> falls er existiert.
		/// </summary>
		/// <remarks>
		/// <para>Falls der angegebene Logger (in der Default-Hierarchie) existiert, wird das 
		/// entsprechende Logger-Objekt geliefert, sonst <c>null</c>.</para>
		/// </remarks>
		/// <param name="domain">Die Logger-Domain, in der gesucht werden soll.</param> 
		/// <param name="name">Der voll-qualifizierte Name des Loggers.</param>
		/// <returns>Den entsprechenden Logger oder null.</returns>
		public static ITraceLog Exists(string domain, string name) {
			return WrapLogger(LoggerManager.Exists(domain, name));
		}


		/// <summary>
		/// Liefert den angegebenen Logger <paramref name="name"/> falls er existiert.
		/// </summary>
		/// <remarks>
		/// <para>Falls der angegebene Logger (in der Default-Hierarchie) existiert, wird das 
		/// entsprechende Logger-Objekt geliefert, sonst <c>null</c>.</para>
		/// </remarks>
		/// <param name="theAssembly">Die Assembly, in der gesucht werden soll.</param>
		/// <param name="name">Der voll-qualifizierte Name des Loggers.</param>
		/// <returns>Den entsprechenden Logger oder null.</returns>
		public static ITraceLog Exists(Assembly theAssembly, string name) {
			return WrapLogger(LoggerManager.Exists(theAssembly, name));
		}

		/// <summary>
		/// Liefert alle aktuell definierten Logger (default Domain).
		/// </summary>
		/// <remarks>
		/// <para>Der Root-Logger ist <b>nicht</b> enthalten.</para>
		/// </remarks>
		/// <returns>Alle definierten Logger.</returns>
		public static ITraceLog[] GetCurrentLoggers() {
			return GetCurrentLoggers(Assembly.GetCallingAssembly());
		}

	
		/// <summary>
		/// Liefert alle aktuell definierten Logger für die angegebene Domain <paramref name="domain"/>.
		/// </summary>
		/// <remarks>
		/// <para>Der Root-Logger ist <b>nicht</b> enthalten.</para>
		/// </remarks>
		/// <param name="domain">Die spezifizierte Domain.</param> 
		/// <returns>Alle definierten Logger.</returns>
		public static ITraceLog[] GetCurrentLoggers(string domain) {
			return WrapLoggers(LoggerManager.GetCurrentLoggers(domain));
		}

		/// <summary>
		/// Liefert alle aktuell definierten Logger für die angegebene 
		/// Assembly <paramref name="theAssembly"/>.
		/// </summary>
		/// <remarks>
		/// <para>Der Root-Logger ist <b>nicht</b> enthalten.</para>
		/// </remarks>
		/// <param name="theAssembly">Die spezifizierte Assembly.</param> 
		/// <returns>Alle definierten Logger.</returns>
		public static ITraceLog[] GetCurrentLoggers(Assembly theAssembly) {
			return WrapLoggers(LoggerManager.GetCurrentLoggers(theAssembly));
		}

		/// <summary>
		/// Liefert oder erzeugt einen Logger für den angebenen Namen <paramref name="name"/>.
		/// </summary>
		/// <param name="name">Der Name des zu liefernden Loggers.</param>
		/// <returns>Der gesuchte Logger oder null.</returns>
		public static ITraceLog GetLogger(string name) {
			return GetLogger(Assembly.GetCallingAssembly(), name);
		}

		/// <summary>
		/// Liefert oder erzeugt einen Logger für den angebenen Namen <paramref name="name"/>
		/// in der Domain <paramref name="domain"/>.
		/// </summary>
		/// <param name="domain">Die Logger-Domain.</param>
		/// <param name="name">Der Name des zu liefernden Loggers.</param>
		/// <returns>Der gesuchte Logger oder null.</returns>
		public static ITraceLog GetLogger(string domain, string name) {
			return WrapLogger(LoggerManager.GetLogger(domain, name));
		}

		/// <summary>
		/// Liefert oder erzeugt einen Logger für den angebenen Namen <paramref name="name"/>
		/// in der Assembly <paramref name="theAssembly"/>.
		/// </summary>
		/// <param name="theAssembly">Die angegebene Assembly.</param>
		/// <param name="name">Der Name des zu liefernden Loggers.</param>
		/// <returns>Der gesuchte Logger oder null.</returns>
		public static ITraceLog GetLogger(Assembly theAssembly, string name) {
			return WrapLogger(LoggerManager.GetLogger(theAssembly, name));
		}	

		/// <summary>
		/// Hilfsmethode für <see cref="LogManager.GetLogger(Type)"/>.
		/// </summary>
		/// <remarks>
		/// Liefert den Logger für den Type <paramref name="type"/>.
		/// </remarks>
		/// <param name="type">Der Typ der Loggers.</param>
		/// <returns>Der gesuchte Logger oder null.</returns>
		public static ITraceLog GetLogger(Type type) {
			#region Checks
			if (type == null) {
				throw new ArgumentNullException("type");
			}
			#endregion

			return GetLogger(type.Assembly, type);
		}

		/// <summary>
		/// Hilfsmethode für <see cref="LogManager.GetLogger(string,Type)"/>.
		/// </summary>
		/// <remarks>
		/// Liefert den Logger für den Type <paramref name="type"/> und
		/// die Logger-Domain <paramref name="domain"/>.
		/// </remarks>
		/// <param name="domain">Die Logger-Domain.</param>
		/// <param name="type">Der Typ der Loggers.</param>
		/// <returns>Der gesuchte Logger oder null.</returns>
		public static ITraceLog GetLogger(string domain, Type type) {
			TraceLog traceLog = (TraceLog)WrapLogger(LoggerManager.GetLogger(domain, type));
			traceLog.DeclaringType = type;
			return traceLog;
		}

		/// <summary>
		/// Hilfsmethode für <see cref="LogManager.GetLogger(Assembly,Type)"/>.
		/// </summary>
		/// <remarks>
		/// Liefert den Logger für den Type <paramref name="type"/> und
		/// die Assembly <paramref name="theAssembly"/>.
		/// </remarks>
		/// <param name="theAssembly">Die Logger-Assembly.</param>
		/// <param name="type">Der Typ der Loggers.</param>
		/// <returns>Der gesuchte Logger oder null.</returns>
		public static ITraceLog GetLogger(Assembly theAssembly, Type type) {
			TraceLog traceLog = (TraceLog) WrapLogger(LoggerManager.GetLogger(theAssembly, type));
			traceLog.DeclaringType = type;
			return traceLog;
		}

		#endregion

		#region Extension Handlers

		/// <summary>
		/// Liefert das Wrapper-Objekt für den angegebenen Logger <paramref name="logger"/>.
		/// Lookup the wrapper object for the logger specified
		/// </summary>
		/// <param name="logger">Der Logger, für den das Wrapper-Objekt zu liefern ist.</param>
		/// <returns>Das ITraceLog Wrapper-Objekt.</returns>
		public static ITraceLog WrapLogger(ILogger logger) {
			return (ITraceLog) s_wrapperMap.GetWrapper(logger);
		}

		/// <summary>
		/// Liefert alle Wrapper-Objekte für die angegebenen Logger <paramref name="loggers"/>.
		/// </summary>
		/// <param name="loggers">Arary von ILogger-Objekten, für die die Wrapper-Objekte zu liefern sind.</param>
		/// <returns>Ein Array von ITraceLog Wrapper-Objekten.</returns>
		public static ITraceLog[] WrapLoggers(ILogger[] loggers) {
			ITraceLog[] results = new ITraceLog[loggers.Length];
			for(int i=0; i<loggers.Length; i++) {
				results[i] = WrapLogger(loggers[i]);
			}
			return results;
		}

		/// <summary>
		/// Methode zum Erzeugen eines Wrapper-Objekts <see cref="ILoggerWrapper"/> für
		/// das angegebene ILogger-Objekt <paramref name="logger"/>.
		/// </summary>
		/// <param name="logger">Der Logger, für den ein Wrapper-Objekt zu erzeugen ist.</param>
		/// <returns>Ein neues Wrapper-Objekt für den angegebenen Logger.</returns>
		private static ILoggerWrapper WrapperCreationHandler(ILogger logger) {
			return new TraceLog(logger);
		}

		#endregion
	}
}
