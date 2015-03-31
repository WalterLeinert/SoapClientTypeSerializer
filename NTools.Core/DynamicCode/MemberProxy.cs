using System.Reflection;

namespace NTools.Core.DynamicCode {

	public abstract class MemberProxy {

		#region Constants

		/// <summary>
		/// all instance members, declared in type
		/// </summary>
		public static BindingFlags AllInstanceDeclared = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

		/// <summary>
		/// all non public instance members, declared in type
		/// </summary>
		public static BindingFlags PrivateInstanceDeclared = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		/// <summary>
		/// all static members, declared in type
		/// </summary>
		public static BindingFlags AllStaticDeclared = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		/// <summary>
		/// all members, declared in type
		/// </summary>
		public static BindingFlags AllDeclared = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		#endregion


		private readonly MemberInfo m_info;

		protected MemberProxy(MemberInfo info) {
			m_info = info;
		}

		protected MemberInfo Info {
			get { return m_info;  }
		}
	}
}
