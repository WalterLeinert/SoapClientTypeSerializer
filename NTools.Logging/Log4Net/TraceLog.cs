//- Standard Namespaces --------------------------------------------------
using System;
using System.Globalization;
using System.Runtime.InteropServices;

//- Zusätzliche Namespaces -----------------------------------------------
using log4net.Core;


namespace NTools.Logging.Log4Net {

	/// <summary>
	/// Diese Klasse implementiert ein gegenüber <see cref="log4net"/> erweitertes Logging.
	/// Es wird vorallem die Verwendung vereinfacht, indem neue Log-Methoden implementiert werden,
	/// die auch einen Formatstring und eine variable Liste von Argumenten erlauben. 
	/// </summary>
	[ComVisible(false)]
	public class TraceLog : LogImpl, ITraceLog {

        #region Member
		private Type m_declaringType = typeof(TraceLog);

        private Level m_levelDebug;
        private Level m_levelInfo;
        private Level m_levelWarn;
        private Level m_levelTrace;
        private Level m_levelError;
        private Level m_levelFatal;


		private Level m_levelEnabledMin = Level.Off;

		public static readonly Level[] SupportedLevels =
			new Level[] {Level.Trace, Level.Debug, Level.Info,  Level.Warn, Level.Error, Level.Fatal};
		#endregion

        #region Public Konstruktoren

        /// <summary>
		/// Dieser Konstruktor initialisiert nur die Basisklasse mit dem angegebenen 
		/// <cref name="ILogger"/> <paramref name="logger"/>.
		/// </summary>
		/// <param name="logger"></param>
		public TraceLog(ILogger logger) : base(logger) {
		}

		#endregion Public Instance Constructors


		internal Type DeclaringType {
			get { return m_declaringType; }
			set { m_declaringType = value; }
		}

		#region Implementierung von ITraceLog

		#region Log

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
			if (Logger.IsEnabledFor(level)) {
				Logger.Log(m_declaringType, level, message, null);
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
			if (Logger.IsEnabledFor(level)) {
				Logger.Log(m_declaringType, level, message, exc);
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
			if (Logger.IsEnabledFor(level)) {
				Logger.Log(m_declaringType, level, message, exc);
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
			if (Logger.IsEnabledFor(level)) {
				Logger.Log(m_declaringType, level, String.Format (CultureInfo.CurrentCulture, format, args), null);
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
			if (Logger.IsEnabledFor(level)) {
				Logger.Log(m_declaringType, level, String.Format(CultureInfo.CurrentCulture, format, args), exc);
			}
		}
		#endregion

		#region Debug

       
	
		/// <seealso cref="Log(Level, string, object[])"/>
		public void Debug(string format, params object[] args) {
            DebugFormat(format, args);
		}

		/// <seealso cref="Log(Level, Exception, string, object[])"/>
		public void Debug(Exception exc, string format, params object[] args) {
			if (IsDebugEnabled) {
				Logger.Log(m_declaringType, m_levelDebug, String.Format(CultureInfo.CurrentCulture, format, args), exc);
			}
		}
		#endregion


		#region Trace

		/// <seealso cref="Log(Level, string, object[])"/>
		public void Trace(string format, params object[] args) {
			if (IsTraceEnabled) {
				Logger.Log(m_declaringType, m_levelTrace, String.Format(CultureInfo.CurrentCulture, format, args), null);
			}
		}

		/// <seealso cref="Log(Level, Exception, string, object[])"/>
		public void Trace(Exception exc, string format, params object[] args) {
			if (IsTraceEnabled) {
				Logger.Log(m_declaringType, m_levelTrace, String.Format(CultureInfo.CurrentCulture, format, args), exc);
			}
		}
		#endregion


		#region Info

		/// <seealso cref="Log(Level, string, object[])"/>
		public void Info(string format, params object[] args) {
			if (IsInfoEnabled) {
                InfoFormat(format, args);
			}
		}

		/// <seealso cref="Log(Level, Exception, string, object[])"/>
 		public void Info(Exception exc, string format, params object[] args) {
			if (IsInfoEnabled) {
				Logger.Log(m_declaringType, m_levelInfo, String.Format(CultureInfo.CurrentCulture, format, args), exc);
			}
		}
		#endregion


		#region Warn
		
		/// <seealso cref="Log(Level, string, object[])"/>
		public void Warn(string format, params object[] args) {
			if (IsWarnEnabled) {
                WarnFormat(format, args);
			}
		}

		/// <seealso cref="Log(Level, Exception, string, object[])"/>
		public void Warn(Exception exc, string format, params object[] args) {
			if (IsWarnEnabled) {
				Logger.Log(m_declaringType, m_levelWarn, String.Format(CultureInfo.CurrentCulture, format, args), exc);
			}
		}
		#endregion


		#region Error

		/// <seealso cref="Log(Level, string, object[])"/>
		public void Error(string format, params object[] args){
			if (IsErrorEnabled) {
                ErrorFormat(format, args);
			}
		}


		/// <seealso cref="Log(Level, Exception, string, object[])"/>
		public void Error(Exception exc, string format, params object[] args){
			if (IsErrorEnabled) {
				Logger.Log(m_declaringType, m_levelError, String.Format(CultureInfo.CurrentCulture, format, args), exc);
			}
		}
		#endregion


		#region Fatal

		/// <seealso cref="Log(Level, string, object[])"/>
		public void Fatal(string format, params object[] args) { 
			if (IsFatalEnabled) {
                FatalFormat(format, args);
			}
		}


		/// <seealso cref="Log(Level, Exception, string, object[])"/>
		public void Fatal(Exception exc, string format, params object[] args) { 
			if (IsFatalEnabled) {
				Logger.Log(m_declaringType, m_levelFatal, String.Format(CultureInfo.CurrentCulture, format, args), exc);
			}
		}
		#endregion

		/// <summary>
		/// Liefert true, falls der Trace-Level aktiviert ist.
		/// </summary>
		virtual public bool IsTraceEnabled {
			get { return Logger.IsEnabledFor(m_levelTrace); }
		}



		/// <summary>
		/// Liefert <c>true</c>, falls der Logger für den angebenen Level <paramref name="level"/> loggen soll.
		/// </summary>
		/// <param name="level">Der Log-Level.</param>
		/// <returns>
		/// 	<c>true</c>, falls für den angebenen Level enabled; sonst <c>false</c>.
		/// </returns>
	    public bool IsEnabledFor(Level level) {
			if (level >= m_levelEnabledMin) {
				return true;
			}
	        return false;
	    }

	    #endregion Implementation of ITraceLog



		#region Public Properties

		public Level LevelEnabledMin {
			get { return m_levelEnabledMin; }
		}

		public Level LevelDebug {
			get { return m_levelDebug; }
		}

		public Level LevelInfo {
			get { return m_levelInfo; }
		}

		public Level LevelWarn { 
			get { return m_levelWarn; } 
		}

		public Level LevelTrace {
			get { return m_levelTrace; }
		}

		public Level LevelError {
			get { return m_levelError; }
		}

		public Level LevelFatal {
			get { return m_levelFatal; }
		}
		
		#endregion

        
		protected override void ReloadLevels(log4net.Repository.ILoggerRepository repository) {
            base.ReloadLevels(repository);

            LevelMap levelMap = repository.LevelMap;

            m_levelDebug = levelMap.LookupWithDefault(Level.Debug);
            m_levelInfo = levelMap.LookupWithDefault(Level.Info);
            m_levelWarn = levelMap.LookupWithDefault(Level.Warn);
            m_levelTrace = levelMap.LookupWithDefault(Level.Trace);
            m_levelError = levelMap.LookupWithDefault(Level.Error);
            m_levelFatal = levelMap.LookupWithDefault(Level.Fatal);
	
			foreach (Level level in SupportedLevels) {
				if (Logger.IsEnabledFor(level)) {
#if !NET_1	
					m_levelEnabledMin = Min<Level>(m_levelEnabledMin, level);
#endif
				}
			}

            OnLevelsReloaded(new LevelsReloadedEventArgs(levelMap));
        }

#if !NET_1		
		public static T Min<T>(T v1, T v2) where T : IComparable  {
			int res = v1.CompareTo(v2);
			if (res < 0) {
				return v1;
			} else {
				return v2;
			}
		}
#endif


		public event LevelsReloadedEventHandler LevelsReloaded;

        protected virtual void OnLevelsReloaded(LevelsReloadedEventArgs e) {
            if (LevelsReloaded != null) {
                LevelsReloaded(this, e);
            }
        }
	}


    /// <summary>
    /// 
    /// </summary>
    public class LevelsReloadedEventArgs : EventArgs {
        private LevelMap m_levelMap;


        public LevelMap LevelMap {
            get { return m_levelMap; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        public LevelsReloadedEventArgs(LevelMap levelMap) {
            m_levelMap = levelMap;
        }
    }


    //
    // Delegate declaration.
    //
    public delegate void LevelsReloadedEventHandler(object sender, LevelsReloadedEventArgs e);
}