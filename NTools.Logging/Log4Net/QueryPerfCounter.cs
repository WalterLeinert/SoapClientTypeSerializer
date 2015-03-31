using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace NTools.Logging {

	/// <summary>
	/// Klasse zur Zeitmessung in Nanosekundenauflösung: 
	/// aus dem Microsoft-Buch: "Improving .NET Application Performance and Scalability"
	/// </summary>
	[Serializable]
	public struct QueryPerfCounter : ISerializable, IDisposable {
		[DllImport("KERNEL32")]
		private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

		[DllImport("Kernel32.dll")]
		private static extern bool QueryPerformanceFrequency(out long lpFrequency);
		
		private long					m_start;
		private long					m_stop;
        private long                    m_diff;
        private long                    m_ticks;

		private static readonly long	s_frequency;
		private static readonly double	s_frequencyDouble;
		private static readonly long	s_multiplierNano	= 1000000000;
		private static readonly long	s_multiplierTicks	=    10000000;
		private static readonly long	s_multiplierMicro	=	  1000000;
		private static readonly long	s_multiplierMilli	=        1000;

		private static readonly double s_multiplierNanoFactor;
		private static readonly double s_multiplierTicksFactor;
		private static readonly double s_multiplierMicroFactor;
		private static readonly double s_multiplierMilliFactor;


		static QueryPerfCounter() {
			if (QueryPerformanceFrequency(out s_frequency) == false) {
				throw new Win32Exception();
			}

			s_frequencyDouble = s_frequency;

			s_multiplierNanoFactor	= s_multiplierNano / s_frequencyDouble;
			s_multiplierTicksFactor = s_multiplierTicks / s_frequencyDouble;
			s_multiplierMicroFactor = s_multiplierMicro / s_frequencyDouble;
			s_multiplierMilliFactor = s_multiplierMilli / s_frequencyDouble;
		}


		public QueryPerfCounter(bool getCounter) {
			QueryPerformanceCounter(out m_start);
			m_diff = 0;
			m_stop = m_start;
		    m_ticks = 0;
		}


		private QueryPerfCounter(SerializationInfo info, StreamingContext context) {
			//m_items = (ArrayList) info.GetValue("m_items", typeof(ArrayList));
			
			//try {
			//    m_type = (SuchprofilTyp) info.GetValue("m_type", typeof(SuchprofilTyp));
			//} catch (Exception exc) {
			//    string type = info.GetString("m_type");
			//    m_type = (SuchprofilTyp) Enum.Parse(typeof(SuchprofilTyp), type);
			//}
			//try {				
			//    m_bestand = (Bestand) info.GetValue("m_bestand", typeof(Bestand));
			//} catch (Exception exc) {
			//    m_bestand = Bestand.Alle;
			//}

			m_start = (long)info.GetValue("m_start", typeof(long));
			m_diff = (long)info.GetValue("m_diff", typeof(long));
            m_stop = (long)info.GetValue("m_stop", typeof(long));
            m_ticks = (long)info.GetValue("m_ticks", typeof(long));
		}


		/// <summary>
		/// Startet eine neue Messung.
		/// </summary>
		public void Start() {
			QueryPerformanceCounter(out m_start);
		}

		/// <summary>
		/// Bendet die aktuelle Messung. 
		/// (Genaugenommen wird nur der aktuelle Zählerstand gespeichert).
		/// </summary>
		public void Stop() {
			QueryPerformanceCounter(out m_stop);
			m_diff = m_stop - m_start;
            m_ticks = ((m_diff * s_multiplierNano * 100) / s_frequency) / 100;
		}


		/// <summary>
		/// Liefert die Dauer einer Iteration in Sekunden, falls insgesamt <paramref name="iterations"/> 
		/// Durchläufe erfolgten.
		/// </summary>
		/// <param name="iterations">Die Anzahl der Durchläufe.</param>
		/// <returns>Dauer einer Iteration in Sekunden.</returns>
		public double SecondsPerIteration(int iterations) {
			return (m_diff / s_frequencyDouble / iterations);
		}


		/// <summary>
		/// Liefert die Dauer der Messung in Ticks (1 Tick = 100 Nanosekunden).
		/// </summary>
		/// <returns>Dauer in Ticks.</returns>
		public long Ticks {
            get {
                return m_ticks;
            }
		}


		/// <summary>
		/// Liefert die Dauer der Messung in Sekunden.
		/// </summary>
		/// <returns>Dauer in Sekunden.</returns>
		public double ElapsedSeconds {
			get {
				return m_diff / s_frequencyDouble;
			}
		}

		/// <summary>
		/// Liefert die Dauer der Messung in Millisekunden.
		/// </summary>
		/// <returns>Dauer in Millisekunden.</returns>
		public double ElapsedMilliseconds {
			get { 
				return m_diff * s_multiplierMilliFactor;
			}
		}

		/// <summary>
		/// Liefert die Dauer der Messung in Microsekunden.
		/// </summary>
		/// <returns>Dauer in Microsekunden.</returns>
		public double ElapsedMicroseconds {
			get { 
				return m_diff * s_multiplierMicroFactor;
			}
		}

		/// <summary>
		/// Liefert die Dauer der Messung in Nanosekunden.
		/// </summary>
		/// <returns>Dauer in Nanosekunden.</returns>
		public double ElapsedNanoseconds {
			get { 
				return m_diff * s_multiplierNanoFactor;
			}
		}

		public TimeSpan TimeSpan {
			get { return new TimeSpan(m_ticks); }
		}

		#region IDisposable Member

		public void Dispose() {
			Stop();
		}

		#endregion

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
			if (info == null) {
				throw new ArgumentNullException("info");
			}
		
			info.AddValue("m_start", m_start);
			info.AddValue("m_stop", m_stop);
			info.AddValue("m_diff", m_diff);
			info.AddValue("m_ticks", m_ticks);
		}

	}
}
