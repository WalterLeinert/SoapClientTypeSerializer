namespace NTools.Core {

	/// <summary>
	/// Zusammenfassung für Utility.
	/// </summary>
	public sealed class Utility {

		public static bool IsNullOrEmpty(string s) {
			return (s == null || s.Length <= 0);
		}
	}
}
