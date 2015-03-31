using System;
using System.Reflection;
using NTools.Core.Reflection;

namespace NTools.WebServiceSupport {

	[Serializable]
	public class FieldSerializer : FieldReflector {
		private readonly object m_value;

		public FieldSerializer(FieldInfo field, object value)
			: base(field) {
			m_value = value;
		}

        /// <summary>
        /// Gets the field value.
        /// </summary>
        /// <value>The value.</value>
		public object Value {
			get { return m_value; }
		}
	}

}
