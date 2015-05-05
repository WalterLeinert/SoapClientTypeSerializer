using System;
using System.Reflection;
using NTools.Logging.Log4Net;

namespace NTools.Core.Reflection {

	/// <summary>
	/// Abstract base class for member access by reflection.
	/// </summary>
	[Serializable]
	public abstract class MemberReflector {
        private static readonly ITraceLog s_log = TraceLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public static BindingFlags AllInstanceDeclared = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;
		public static BindingFlags PrivateInstanceDeclared = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
		public static BindingFlags AllStaticDeclared = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
		public static BindingFlags AllDeclared = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		private readonly Type m_type;
		private readonly string m_memberName;
		private readonly BindingFlags m_bindingFlags;
		private MemberInfo m_memberInfo;

		#region Konstruktor / Cleanup


		/// <summary>
		/// Initialisiert eine neue Instanz der class <see cref="MemberReflector"/>.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="memberName">Name of the member.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		protected MemberReflector(Type type, string memberName, BindingFlags bindingFlags) {
			#region Checks
			if (type == null) {
				throw new ArgumentNullException("type");
			}
			if (String.IsNullOrEmpty(memberName)) {
				throw new ArgumentNullException("memberName");
			}
			#endregion

			m_type = type;
			m_memberName = memberName;
			m_bindingFlags = bindingFlags;
		}

		/// <summary>
		/// Initialisiert eine neue Instanz der class <see cref="MemberReflector"/>.
		/// </summary>
		/// <param name="memberInfo">The member info.</param>
		protected MemberReflector(MemberInfo memberInfo) {
			#region Checks
			if (memberInfo == null) {
				throw new ArgumentNullException("memberInfo");
			}
			#endregion

			m_memberInfo = memberInfo;
		}
		#endregion

		#region Properties

		/// <summary>
		/// Gets the type.
		/// </summary>
		/// <value>The type.</value>
		public Type Type {
			get { return m_type; }
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name {
			get { return m_memberName; }
		}

		/// <summary>
		/// Gets the binding flags.
		/// </summary>
		/// <value>The binding flags.</value>
		public BindingFlags BindingFlags {
			get { return m_bindingFlags; }
		}

		/// <summary>
		/// Gets or sets the info.
		/// </summary>
		/// <value>The info.</value>
		public MemberInfo Info {
			get { return m_memberInfo; }
			set { m_memberInfo = value; }
		}
		#endregion
	}	
}
