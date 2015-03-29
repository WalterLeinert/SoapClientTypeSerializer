using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NTools.Core.DynamicCode {

	public delegate TField GetterDelegateGen<TClass, TField>(TClass instance);
	public delegate void SetterDelegateGen<TClass, TField>(TClass instance, TField value);


	/// <summary>
	/// </summary>
	[Serializable]
	public class FieldProxy<TClass> : FieldProxyBase {

		#region Constructor / Cleanup

		public FieldProxy(FieldInfo fieldInfo) : base(fieldInfo) {
			if (!IsKnownFieldType(fieldInfo.FieldType)) {
				m_getterMethod = CreateFieldGetter(typeof(TClass), typeof(object), typeof(TClass));
				m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegateGen<TClass, object>));
			} else {
				m_getterMethod = CreateFieldGetter(typeof(TClass), fieldInfo.FieldType, typeof(TClass));

				if (Info.FieldType == typeof(char)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegateGen<TClass, char>));
				} else if (fieldInfo.FieldType == typeof(bool)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegateGen<TClass, bool>));
				} else if (fieldInfo.FieldType == typeof(short)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegateGen<TClass, short>));
				} else if (fieldInfo.FieldType == typeof(int)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegateGen<TClass, int>));
				} else if (fieldInfo.FieldType == typeof(long)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegateGen<TClass, long>));
				} else if (fieldInfo.FieldType == typeof(float)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegateGen<TClass, float>));
				} else if (fieldInfo.FieldType == typeof(double)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegateGen<TClass, double>));
				} else if (fieldInfo.FieldType == typeof(string)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegateGen<TClass, string>));
				} else if (fieldInfo.FieldType == typeof(DateTime)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegateGen<TClass, DateTime>));
				} else {
					throw new NotSupportedException(string.Format("Unsupported fieldType: {0}", fieldInfo.FieldType));
				}
			}


			if (!IsKnownFieldType(fieldInfo.FieldType)) {
				m_setterMethod = CreateFieldSetter(typeof(TClass), typeof(object), typeof(TClass));
				m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegateGen<TClass, object>));
			} else {
				m_setterMethod = CreateFieldSetter(typeof(TClass), fieldInfo.FieldType, typeof(TClass));

				if (Info.FieldType == typeof (char)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegateGen<TClass, char>));
				} else if (fieldInfo.FieldType == typeof (bool)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegateGen<TClass, bool>));
				} else if (fieldInfo.FieldType == typeof(short)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegateGen<TClass, short>));
				} else if (fieldInfo.FieldType == typeof(int)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegateGen<TClass, int>));
				} else if (fieldInfo.FieldType == typeof (long)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegateGen<TClass, long>));
				} else if (fieldInfo.FieldType == typeof (float)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegateGen<TClass, float>));
				} else if (fieldInfo.FieldType == typeof (double)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegateGen<TClass, double>));
				} else if (fieldInfo.FieldType == typeof (string)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegateGen<TClass, string>));
				} else if (fieldInfo.FieldType == typeof(DateTime)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegateGen<TClass, DateTime>));
				} else {
					throw new NotSupportedException(string.Format("Unsupported fieldType: {0}", fieldInfo.FieldType));
				}
			}
		}

		#endregion


		#region Getter


		/// <summary>
		/// Gets the current field value of type <typeparamref name="TClass"/> for the given <paramref name="instance"/>.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <returns></returns>
		public object GetValue(TClass instance) {
			return GetValue<object>(instance);
		}

		/// <summary>
		/// Get value die aktuelle Instanz.
		/// </summary>
		/// <typeparam name="TField">The type of the field.</typeparam>
		/// <param name="instance">The instance.</param>
		/// <returns></returns>
		public TField GetValue<TField>(TClass instance) {
			return ((GetterDelegateGen<TClass, TField>)m_getterDelegate)(instance);
		}

		#endregion

		#region Setter


		/// <summary>
		/// Set value die aktuelle Instanz.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <param name="value">The value.</param>
		public void SetValue(TClass instance, object value) {
			SetValue<object>(instance, value);
		}

		/// <summary>
		/// Set value die aktuelle Instanz.
		/// </summary>
		/// <typeparam name="TField">The type of the field.</typeparam>
		/// <param name="instance">The instance.</param>
		/// <param name="value">The value.</param>
		public void SetValue<TField>(TClass instance, TField value) {
			((SetterDelegateGen<TClass, TField>)m_setterDelegate)(instance, value);
		}
		#endregion
	
	}


	public delegate TField GetterDelegate<TField>(object instance);
	public delegate void SetterDelegate<TField>(object instance, TField value);


	/// <summary>
	/// </summary>
	[Serializable]
	public class FieldProxy : FieldProxyBase {
	
		#region Constructor / Cleanup

		public FieldProxy(FieldInfo fieldInfo)
			: base(fieldInfo) {

			if (!IsKnownFieldType(fieldInfo.FieldType)) {
				m_getterMethod = CreateFieldGetter(typeof(object), typeof(object), fieldInfo.DeclaringType);
				m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegate<object>));
			} else {
				m_getterMethod = CreateFieldGetter(typeof(object), fieldInfo.FieldType, fieldInfo.DeclaringType);

				if (Info.FieldType == typeof(char)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegate<char>));
				} else if (fieldInfo.FieldType == typeof(bool)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegate<bool>));
				} else if (fieldInfo.FieldType == typeof(short)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegate<short>));
				} else if (fieldInfo.FieldType == typeof(int)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegate<int>));
				} else if (fieldInfo.FieldType == typeof(long)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegate<long>));
				} else if (fieldInfo.FieldType == typeof(float)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegate<float>));
				} else if (fieldInfo.FieldType == typeof(double)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegate<double>));
				} else if (fieldInfo.FieldType == typeof(string)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegate<string>));
				} else if (fieldInfo.FieldType == typeof(DateTime)) {
					m_getterDelegate = m_getterMethod.CreateDelegate(typeof(GetterDelegate<DateTime>));
				} else {
					throw new NotSupportedException(string.Format("Unsupported fieldType: {0}", fieldInfo.FieldType));
				}
			}


			if (!IsKnownFieldType(fieldInfo.FieldType)) {
				m_setterMethod = CreateFieldSetter(typeof(object), typeof(object), fieldInfo.DeclaringType);
				m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegate<object>));
			} else {
				m_setterMethod = CreateFieldSetter(typeof(object), fieldInfo.FieldType, fieldInfo.DeclaringType);

				if (Info.FieldType == typeof(char)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegate<char>));
				} else if (fieldInfo.FieldType == typeof(bool)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegate<bool>));
				} else if (fieldInfo.FieldType == typeof(short)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegate<short>));
				} else if (fieldInfo.FieldType == typeof(int)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegate<int>));
				} else if (fieldInfo.FieldType == typeof(long)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegate<long>));
				} else if (fieldInfo.FieldType == typeof(float)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegate<float>));
				} else if (fieldInfo.FieldType == typeof(double)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegate<double>));
				} else if (fieldInfo.FieldType == typeof(string)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegate<string>));
				} else if (fieldInfo.FieldType == typeof(DateTime)) {
					m_setterDelegate = m_setterMethod.CreateDelegate(typeof(SetterDelegate<DateTime>));
				} else {
					throw new NotSupportedException(string.Format("Unsupported fieldType: {0}", fieldInfo.FieldType));
				}
			}
		}

		#endregion

	}


	/// <summary>
	/// </summary>
	[Serializable]
	public abstract class FieldProxyBase : MemberProxy {
		[NonSerialized]
		protected DynamicMethod m_getterMethod;

		[NonSerialized]
		protected DynamicMethod m_setterMethod;

		[NonSerialized]
		protected Delegate m_getterDelegate;

		[NonSerialized]
		protected Delegate m_setterDelegate;

		[NonSerialized] 
		protected static Dictionary<Type, object> s_knownFieldTypes;

		private readonly bool m_isValueType;
		

		#region Constructor / Cleanup

		static FieldProxyBase() {
			s_knownFieldTypes = new Dictionary<Type, object>();
			s_knownFieldTypes.Add(typeof (char), null);
			s_knownFieldTypes.Add(typeof(bool), null);
			s_knownFieldTypes.Add(typeof(short), null);
			s_knownFieldTypes.Add(typeof(int), null); 
			s_knownFieldTypes.Add(typeof (long), null); 
			s_knownFieldTypes.Add(typeof (float), null);
			s_knownFieldTypes.Add(typeof(double), null);
			s_knownFieldTypes.Add(typeof(string), null);
			s_knownFieldTypes.Add(typeof(DateTime), null);
		}

		protected FieldProxyBase(FieldInfo fieldInfo) : base(fieldInfo) {
			m_isValueType = fieldInfo.FieldType.IsValueType;
		}

		#endregion


		protected static bool IsKnownFieldType(Type type) {
			return s_knownFieldTypes.ContainsKey(type);
		}


		/// <summary>
		/// Gets the info.
		/// </summary>
		/// <value>The info.</value>
		public new FieldInfo Info {
			get { return (FieldInfo)base.Info; }
		}

		public bool IsValueType {
			get { return m_isValueType; }
		}

		#region Getter


		/// <summary>
		/// Gets the current field value for the given <paramref name="instance"/>.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <returns></returns>
		public object GetValue(object instance) {
			return GetValue<object>(instance);
		}

		/// <summary>
		/// Get value die aktuelle Instanz.
		/// </summary>
		/// <typeparam name="TField">The type of the field.</typeparam>
		/// <param name="instance">The instance.</param>
		/// <returns></returns>
		public TField GetValue<TField>(object instance) {
			if (m_isValueType) {
				return ((GetterDelegate<TField>)m_getterDelegate)(instance);
			} else {
				object o = ((GetterDelegate<object>)m_getterDelegate)(instance);
				return (TField) o;
			}
		}

		#endregion

		#region Setter



		/// <summary>
		/// Set value die aktuelle Instanz.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <param name="value">The value.</param>
		public void SetValue(object instance, object value) {
			SetValue<object>(instance, value);			
		}

		/// <summary>
		/// Set value die aktuelle Instanz.
		/// </summary>
		/// <typeparam name="TField">The type of the field.</typeparam>
		/// <param name="instance">The instance.</param>
		/// <param name="value">The value.</param>
		public void SetValue<TField>(object instance, TField value) {
			if (m_isValueType) {
				((SetterDelegate<TField>)m_setterDelegate)(instance, value);
			} else {
				((SetterDelegate<object>)m_setterDelegate)(instance, (object) value);
			}
		}
		#endregion

		/// <summary>
		/// Create a dynamic static field getter method; first parameter is a instance of
		/// the declaring type of the field.
		/// </summary>
		/// <param name="classType">Type of the class.</param>
		/// <param name="fieldType">Type of the field.</param>
		/// <param name="owner">The owner.</param>
		/// <returns></returns>
		protected DynamicMethod CreateFieldGetter(Type classType, Type fieldType, Type owner) {
			DynamicMethod method = new DynamicMethod(Info.Name + "___generatedGetter",
				fieldType,
				new Type[] { classType },
				owner
			);

			ILGenerator generator = method.GetILGenerator();
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, Info);
			generator.Emit(OpCodes.Ret);

			return method;
		}

		/// <summary>
		/// Create a dynamic field setter method.
		/// </summary>
		/// <param name="classType">Type of the class.</param>
		/// <param name="fieldType">Type of the field.</param>
		/// <param name="owner">The owner.</param>
		/// <returns></returns>
		protected DynamicMethod CreateFieldSetter(Type classType, Type fieldType, Type owner) {
			DynamicMethod method = new DynamicMethod(Info.Name + "___generatedSetter",
				typeof(void),
				new Type[] { classType, fieldType },
				owner
			);

			ILGenerator generator = method.GetILGenerator();
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Stfld, Info);
			generator.Emit(OpCodes.Ret);

			return method;
		}
	}
}
