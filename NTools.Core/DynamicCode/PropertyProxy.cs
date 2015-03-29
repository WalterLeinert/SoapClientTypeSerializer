using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace NTools.Core.DynamicCode {


	public delegate T PropertyGetDelegate<TClass, T>(TClass obj);
	public delegate void PropertySetDelegate<TClass, T>(TClass obj, T value);

	public delegate object PropertyGetDelegate<TClass>(TClass obj);
	public delegate void PropertySetDelegate<TClass>(TClass obj, object value);

	public delegate object PropertyGetDelegate(object obj);
	public delegate void PropertySetDelegate(object obj, object value);

	public delegate T MemberGetDelegate<T>(object obj);

	

	//
	// Delegate Deklaration für die generischen Getter/Setter-Aufrufe
	//
	public delegate T PropertyGetter<T>(object obj);
	public delegate void PropertySetter<T>(object obj, T value);

	#region Delegates für Privitive
	public delegate int IntGetterDelegate(object obj);
	public delegate void IntSetterDelegate(object obj, int value);
	#endregion


	public class PropertyProxy : MemberProxy {
		private PropertyGetDelegate m_propertyGetDelegate;
		private PropertySetDelegate m_propertySetDelegate;

		public PropertyProxy(Type type, string propertyName)
			: this(type.GetProperty(propertyName)) {
		}


		public PropertyProxy(PropertyInfo info) : base(info) {

			MethodInfo getter = Info.GetGetMethod();
			if (getter != null) {
				DynamicMethod getterMethod = CreateGetter(Info.DeclaringType, Info.PropertyType, Info.DeclaringType, getter);
				m_propertyGetDelegate = (PropertyGetDelegate) getterMethod.CreateDelegate(typeof(PropertyGetDelegate));

				//m_propertySetDelegate = (PropertySetDelegate)
				//    Delegate.CreateDelegate(typeof(PropertySetDelegate), miSetter);
			}

			MethodInfo setter = Info.GetSetMethod();
			if (setter != null) {		
				DynamicMethod setterMethod = CreateSetter(Info.DeclaringType, Info.PropertyType, Info.DeclaringType, setter);
				m_propertySetDelegate = (PropertySetDelegate) setterMethod.CreateDelegate(typeof(PropertySetDelegate));
			}
		}

		/// <summary>
		/// Gets the info.
		/// </summary>
		/// <value>The info.</value>
		public new PropertyInfo Info {
			get { return (PropertyInfo)base.Info; }
		}



		public object GetValue(object instance) {
			if (m_propertyGetDelegate == null) {
				throw new InvalidOperationException("m_propertyGetDelegate== null");
			}
			return m_propertyGetDelegate(instance);
		}


		public void SetValue(object instance, object value) {
			if (m_propertySetDelegate == null) {
				throw new InvalidOperationException("m_propertySetDelegate== null");
			}
			m_propertySetDelegate(instance, value);
		}



		public T GetValue<TClass, T>(TClass instance) {
			return (T) GetValue(instance);
		}

		public void SetValue<TClass, T>(TClass instance, T value) {
			SetValue((object) instance, (object) value);
		}

		/// <summary>
		/// Create a dynamic static field getter method; first parameter is a instance of
		/// the declaring type of the field.
		/// </summary>
		/// <param name="classType">Type of the class.</param>
		/// <param name="propertyType">Type of the field.</param>
		/// <param name="owner">The owner.</param>
		/// <param name="getter">MethodInfo für Getter.</param>
		/// <returns></returns>
		protected DynamicMethod CreateGetter(Type classType, Type propertyType, Type owner, MethodInfo getter) {
			DynamicMethod method = new DynamicMethod(Info.Name + "___generatedGetter",
				typeof(object),
				new Type[] { typeof(object) },
				owner
			);

			ILGenerator generator = method.GetILGenerator();
			generator.DeclareLocal(typeof(object));
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, classType);

			generator.EmitCall(OpCodes.Callvirt, getter, null);

			if (!propertyType.IsClass) {
				generator.Emit(OpCodes.Box, propertyType);
			}

			generator.Emit(OpCodes.Ret);

			return method;
		}

		/// <summary>
		/// Create a dynamic field setter method.
		/// </summary>
		/// <param name="classType">Type of the class.</param>
		/// <param name="propertyType">Type of the field.</param>
		/// <param name="owner">The owner.</param>
		/// <param name="setter">MethodInfo für Setter.</param>
		/// <returns></returns>
		protected DynamicMethod CreateSetter(Type classType, Type propertyType, Type owner, MethodInfo setter) {
			DynamicMethod method = new DynamicMethod(Info.Name + "___generatedSetter",
				typeof(void),
				new Type[] { typeof(object), typeof(object) },
				owner
			);

			ILGenerator generator = method.GetILGenerator();

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, classType);
			generator.Emit(OpCodes.Ldarg_1);

			if (propertyType.IsClass) {
				generator.Emit(OpCodes.Castclass, propertyType);
			} else {
				generator.Emit(OpCodes.Unbox_Any, propertyType);
			}

			generator.EmitCall(OpCodes.Callvirt, setter, null);
			generator.Emit(OpCodes.Ret);

			return method;
		}



	}

	public class PropertyProxy<TClass> : PropertyProxy {
		private readonly Delegate m_primitiveGetterDelegate;
		private readonly Delegate m_primitiveSetterDelegate;

		private readonly PropertyGetDelegate m_getterDelegate;
		private readonly PropertySetDelegate m_setterDelegate;
		private readonly DynamicMethod m_getterMethod;
		private readonly DynamicMethod m_setterMethod;

		private IntGetterDelegate m_propertyGetDelegate;
		private PropertyGetDelegate<TClass, int> m_intGetterDelegate;
		private PropertySetDelegate<TClass, int> m_intSetterDelegate;

		private static Dictionary<Type, DelegateHelper> s_primitiveDelegateTypes;


		public delegate T PropertyGetDelegate<TCl, T>(TCl obj);
		public delegate void PropertySetDelegate<TCl, T>(TCl obj, T value);
	
		public delegate int IntPropertyGetDelegate(TClass obj);



		static PropertyProxy() {
			s_primitiveDelegateTypes = new Dictionary<Type, DelegateHelper>();

			s_primitiveDelegateTypes.Add(typeof(int), new DelegateHelper(typeof(IntGetterDelegate), typeof(IntSetterDelegate)));
		}


		public PropertyProxy(string propertyName)
			: this(typeof(TClass).GetProperty(propertyName)) {
		}


		public PropertyProxy(PropertyInfo info)
			: base(info) {

			MethodInfo miGetter = info.GetGetMethod();
			if (miGetter != null) {
				// NOTE:  As reader J. Dunlap pointed out...
				//  Calling a property's get accessor is faster/cleaner using
				//  Delegate.CreateDelegate rather than Reflection.Emit 
				//IntPropertyGetDelegate propertyGetDelegate = (IntPropertyGetDelegate)
				//    Delegate.CreateDelegate(typeof(IntPropertyGetDelegate), mi);

				m_intGetterDelegate = (PropertyGetDelegate<TClass, int>)
					Delegate.CreateDelegate(typeof(PropertyGetDelegate<TClass, int>), miGetter);
			}

			MethodInfo miSetter = info.GetSetMethod();
			if (miSetter != null) {
				m_intSetterDelegate = (PropertySetDelegate<TClass, int>)
					Delegate.CreateDelegate(typeof(PropertySetDelegate<TClass, int>), miSetter);
			}


			if (Info.PropertyType.IsPrimitive) {
				DelegateHelper delegateHelper;
				if (s_primitiveDelegateTypes.TryGetValue(Info.PropertyType, out delegateHelper)) {
					m_getterMethod = CreatePrimitiveGetter(Info.DeclaringType, Info.PropertyType, Info.DeclaringType);
					m_primitiveGetterDelegate = m_getterMethod.CreateDelegate(delegateHelper.GetterDelegateType);

					m_setterMethod = CreatePrimitiveSetter(Info.DeclaringType, Info.PropertyType, Info.DeclaringType);
					m_primitiveSetterDelegate = m_setterMethod.CreateDelegate(delegateHelper.SetterDelegateType);
				}

			} else {
				DynamicMethod m_getterMethod = CreateGetMethod();
				DynamicMethod m_setterMethod = CreateSetMethod();

				m_getterDelegate = (PropertyGetDelegate)m_getterMethod.CreateDelegate(typeof(PropertyGetDelegate));
				m_setterDelegate = (PropertySetDelegate)m_setterMethod.CreateDelegate(typeof(PropertySetDelegate));
			}
		}


		#region Zugriff auf die Propertywerte

		public T GetValueByMethod<T>(TClass instance) {
			if (m_getterMethod == null) {
				throw new InvalidOperationException("m_getterMethod == null");
			}
			return (T) m_getterMethod.Invoke(instance, null);
		}

		public void SetValueByMethod<T>(TClass instance, T value) {
			if (m_setterMethod == null) {
				throw new InvalidOperationException("m_setterMethod == null");
			}
			m_setterMethod.Invoke(instance, new object[] { value });
		}


		public T GetValueByDelegate<T>(TClass instance) {
			if (m_getterDelegate == null) {
				throw new InvalidOperationException("m_getterDelegate == null");
			}
			return (T) m_getterDelegate(instance);
		}

		public void SetValueByDelegate<T>(TClass instance, T value) {
			if (m_setterDelegate == null) {
				throw new InvalidOperationException("m_setterDelegate == null");
			}
			m_setterDelegate(instance, value);
		}

		#endregion

		public int GetIntValue(object instance) {
			return ((IntGetterDelegate) m_primitiveGetterDelegate)(instance);
		}

		public void SetIntValue(object instance, int value) {
			((IntSetterDelegate)m_primitiveSetterDelegate)(instance, value);
		}


		public int GetIntValue(TClass instance) {
			return m_intGetterDelegate(instance);
		}

		public void SetIntValue(TClass instance, int value) {
			m_intSetterDelegate(instance, value);
		}

		#region Erzeugen von DynamicMethods mit object-Parametern

		/// <summary>
		/// Erzeugt eine <see cref="DynamicMethod"/> für den Getter-Zugriff mit <see cref="object"/>-Parametern.
		/// </summary>
		/// <returns>Eine <see cref="DynamicMethod"/>.</returns>
		protected DynamicMethod CreateGetMethod() {
		
			MethodInfo getMethod = Info.GetGetMethod();
			if (getMethod == null) {
				return null;				
			}

			DynamicMethod getter = new DynamicMethod(
				Info.Name + "___generatedGetter",
				typeof(object), 
				new Type[] { typeof(object) }, 
				Info.DeclaringType
				);

			ILGenerator generator = getter.GetILGenerator();
			generator.DeclareLocal(typeof(object));
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, Info.DeclaringType);
			generator.EmitCall(OpCodes.Callvirt, getMethod, null);

			if (!Info.PropertyType.IsClass)
				generator.Emit(OpCodes.Box, Info.PropertyType);

			generator.Emit(OpCodes.Ret);

			return getter;
		}


		/// <summary>
		/// Erzeugt eine <see cref="DynamicMethod"/> für den Setter-Zugriff mit <see cref="object"/>-Parametern.
		/// </summary>
		/// <returns>Eine <see cref="DynamicMethod"/>.</returns>
		protected DynamicMethod CreateSetMethod() {
			MethodInfo setMethod = Info.GetSetMethod();
			if (setMethod == null) {
				return null;
			}
	
			DynamicMethod setter = new DynamicMethod(
				Info.Name + "___generatedSetter",
				typeof(void),
				new Type[] { typeof(object), typeof(object) },
				Info.DeclaringType
			);
		
			ILGenerator generator = setter.GetILGenerator();
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, Info.DeclaringType);
			generator.Emit(OpCodes.Ldarg_1);

			if (Info.PropertyType.IsClass)
				generator.Emit(OpCodes.Castclass, Info.PropertyType);
			else
				generator.Emit(OpCodes.Unbox_Any, Info.PropertyType);

			generator.EmitCall(OpCodes.Callvirt, setMethod, null);
			generator.Emit(OpCodes.Ret);

			return setter;
		}

		#endregion

#if (false)

		/// <summary>
		/// Erzeugt einen <see cref="DynamicMethod"/>, die den Zugriff auf den Property-Getter kapselt.
		/// </summary>
		/// <param name="classType">Der Klassentyp, der die Property enthält.</param>
		/// <param name="primitiveType">Der Propertytyp (primitiv).</param>
		/// <param name="owner">Der Typ, in dem die <see cref="DynamicMethod"/> generiert wird.</param>
		/// <param name="getter">Die <see cref="MethodInfo"/> des Getters.</param>
		/// <returns>Eine <see cref="DynamicMethod"/>.</returns>
		/// <remarks>
		/// Siehe: ms-help://MS.MSDNQTR.v90.en/fxref_mscorlib/html/7bee8d22-b853-7de0-b6a4-9405f4cb5864.htm
		/// </remarks>
		protected PropertyGetDelegate CreateGenericPrimitiveGetter(Type classType, Type primitiveType, Type owner, MethodInfo getter) {
			// Creating a dynamic assembly requires an AssemblyName
			// object, and the current application domain.
			//
			AssemblyName asmName =
				new AssemblyName("DemoMethodBuilder1");
			AppDomain domain = AppDomain.CurrentDomain;
			AssemblyBuilder demoAssembly =
				domain.DefineDynamicAssembly(
					asmName,
					AssemblyBuilderAccess.RunAndSave
				);

			// Define the module that contains the code. For an 
			// assembly with one module, the module name is the 
			// assembly name plus a file extension.
			ModuleBuilder demoModule =
				demoAssembly.DefineDynamicModule(
					asmName.Name,
					asmName.Name + ".dll"
				);


			TypeBuilder demoType = demoModule.DefineType(classType.Name, 
				TypeAttributes.Public | TypeAttributes.Abstract);


			MethodBuilder demoMethod = demoType.DefineMethod(
				Info.Name + "___generatedGetter",
				MethodAttributes.Public | MethodAttributes.Static
			);
			
			MethodBuilder mb;
			string[] typeParamNames = { "T" };
			GenericTypeParameterBuilder[] typeParameters =
				demoMethod.DefineGenericParameters(typeParamNames);


			// Use the IsGenericMethod property to find out if a
			// dynamic method is generic, and IsGenericMethodDefinition
			// to find out if it defines a generic method.
			Console.WriteLine("Is DemoMethod generic? {0}",
				demoMethod.IsGenericMethod);
			Console.WriteLine("Is DemoMethod a generic method definition? {0}",
				demoMethod.IsGenericMethodDefinition);

			// Set parameter types for the method. The method takes
			// one parameter, and its type is specified by the first
			// type parameter, T.
			Type[] parms = { typeParameters[0] };
			demoMethod.SetParameters(parms);

			// Set the return type for the method. The return type is
			// specified by the second type parameter, U.
			demoMethod.SetReturnType(typeParameters[1]);


			ILGenerator generator = demoMethod.GetILGenerator();
			generator.DeclareLocal(typeof(object));
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, classType);

			generator.EmitCall(OpCodes.Callvirt, getter, null);
			generator.Emit(OpCodes.Ret);

			return demoMethod.CreateDelegate(typeof(PropertyGetDelegate));
		}

#endif

		/// <summary>
		/// Create a dynamic field setter method.
		/// </summary>
		/// <param name="classType">Type of the class.</param>
		/// <param name="propertyType">Type of the field.</param>
		/// <param name="owner">The owner.</param>
		/// <returns></returns>
		protected DynamicMethod CreatePrimitiveGetter(Type classType, Type propertyType, Type owner) {
			MethodInfo getter = Info.GetGetMethod();
			if (getter == null) {
				return null;
			}

			DynamicMethod method = new DynamicMethod(Info.Name + "___generatedGetter",
				propertyType,
				new Type[] { typeof(object) },
				owner
			);

			ILGenerator generator = method.GetILGenerator();

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, classType);
			//generator.Emit(OpCodes.Ldarg_1);

			generator.EmitCall(OpCodes.Callvirt, getter, null);
			generator.Emit(OpCodes.Ret);
			return method;
		}



		/// <summary>
		/// Erzeugt einen <see cref="DynamicMethod"/>, die den Zugriff auf den Property-Setter kapselt.
		/// </summary>
		/// <param name="classType">Der Klassentyp, der die Property enthält.</param>
		/// <param name="primitiveType">Der Propertytyp (primitiv).</param>
		/// <param name="owner">Der Typ, in dem die <see cref="DynamicMethod"/> generiert wird.</param>
		/// <returns>Eine <see cref="DynamicMethod"/>.</returns>
		protected DynamicMethod CreatePrimitiveSetter(Type classType, Type primitiveType, Type owner) {
			MethodInfo setter = Info.GetGetMethod();
			if (setter == null) {
				return null;
			}

			DynamicMethod method = new DynamicMethod(Info.Name + "___generatedSetter",
				typeof(void),
				new Type[] { typeof(object), primitiveType },
				owner
			);

			ILGenerator generator = method.GetILGenerator();

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, classType);

			generator.Emit(OpCodes.Ldarg_1);

			//if (primitiveType.IsClass) {
			//    generator.Emit(OpCodes.Castclass, primitiveType);
			//} else {
			//    generator.Emit(OpCodes.Unbox_Any, primitiveType);
			//}

			generator.EmitCall(OpCodes.Callvirt, setter, null);
			generator.Emit(OpCodes.Ret);

			method.Module.Assembly.GetName();

			return method;
		}

		class DelegateHelper {
			private Type m_getterDelegateType;
			private Type m_setterDelegateType;

			public DelegateHelper(Type getterDelegateType, Type setterDelegateType) {
				m_getterDelegateType = getterDelegateType;
				m_setterDelegateType = setterDelegateType;
			}

			public Type GetterDelegateType {
				get { return m_getterDelegateType; }
			}

			public Type SetterDelegateType {
				get { return m_setterDelegateType; }
			}
		}
	}

	public class PropertyProxy<Type, PropertyType> : MemberProxy {
		private PropertyGetDelegate<Type, PropertyType> m_getter;
		private PropertySetDelegate<Type, PropertyType> m_setter;

		#region Constructor / Cleanup

		public PropertyProxy(string propertyName)
			: this(typeof(Type).GetProperty(propertyName)) {
		}

		public PropertyProxy(PropertyInfo info) : base(info) {
			m_getter = GetPropertyGetter(info);
			m_setter = GetPropertySetter(info);
		}

		#endregion


		public PropertyType GetValue(Type instance) {
			if (m_setter == null) {
				throw new InvalidOperationException("m_setter == null");
			}
			return m_getter(instance);
		}

		public void SetValue(Type instance, PropertyType value) {
			if (m_getter == null) {
				throw new InvalidOperationException("m_getter == null");
			}
			m_setter(instance, value);
		}

		public static PropertyGetDelegate<Type, PropertyType> GetPropertyGetter(PropertyInfo propertyInfo) {
			if (propertyInfo == null) {
				throw new ArgumentNullException("propertyInfo");
			}

			MethodInfo getter = propertyInfo.GetGetMethod();
			if (getter != null) {
				return (PropertyGetDelegate<Type, PropertyType>)Delegate.CreateDelegate(typeof(PropertyGetDelegate<Type, PropertyType>), getter);
			}
			return null;
		}

		public static PropertySetDelegate<Type, PropertyType> GetPropertySetter(PropertyInfo propertyInfo) {
			if (propertyInfo == null) {
				throw new ArgumentNullException("propertyInfo");
			}

			MethodInfo setter = propertyInfo.GetSetMethod();
			if (setter != null) {
				return (PropertySetDelegate<Type, PropertyType>) Delegate.CreateDelegate(typeof(PropertySetDelegate<Type, PropertyType>), setter);
			}
			return null;
		}
	}


	public class TypeUtility<Type> {

	}

}
