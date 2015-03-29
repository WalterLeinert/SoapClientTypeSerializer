using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NTools.Core.DynamicCode {
    public delegate object MethodDelegate(object instance, params object[] args);

    public  class MethodProxy : MemberProxy {
        private DynamicMethod m_method;
        private MethodDelegate m_methodDelegate;

        public MethodProxy(MethodInfo methodInfo) : base(methodInfo) {
            m_method = CreateMethodInvoker(methodInfo, methodInfo.DeclaringType);
            // m_methodDelegate = (MethodDelegate)m_method.CreateDelegate(typeof(MethodDelegate));			
        }

        private static DynamicMethod CreateMethodInvoker(MethodInfo methodInfo, Type owner) {
            List<Type> parameterTypes = new List<Type>( );
            parameterTypes.Add(methodInfo.DeclaringType);
            ParameterInfo [] parameterInfos = methodInfo.GetParameters( );

            foreach (ParameterInfo pi in parameterInfos) {
                parameterTypes.Add(pi.ParameterType);
                //parameterTypes.Add(typeof(object));
            }

            DynamicMethod method = new DynamicMethod(methodInfo.Name + "___generatedInvoker",
                //MethodAttributes.Public,
                //CallingConventions.VarArgs,
                methodInfo.ReturnType,
                parameterTypes.ToArray( ),
                owner,
                false                
                );

            ILGenerator generator = method.GetILGenerator( );
            generator.Emit(OpCodes.Ldarg_0);

            for (int i = 0 ; i < parameterInfos.Length ; i++) {
                generator.Emit(OpCodes.Ldarg, i + 1);
            }

            //generator.Emit(OpCodes.Ldarg_0);
            generator.EmitCall(OpCodes.Call, methodInfo, null);

            //if (methodInfo.ReturnType != typeof(void)) {
            //    generator.Emit(OpCodes.Stloc_1);
            //}
            generator.Emit(OpCodes.Ret);

            for (int i = 0 ; i < parameterInfos.Length ; i++) {
                ParameterBuilder pb = method.DefineParameter(
                    i + 1,
                    parameterInfos[i].Attributes,
                    parameterInfos[i].Name);
            }
            
            return method;
        }

        public Delegate CreateDelegate(Type delegateType, object instance) {
            return m_method.CreateDelegate(delegateType, instance);
        }


        public object Invoke(object instance, params object [] args) {
            return m_method.Invoke(instance, args);
            //return m_methodDelegate(instance, args);
        }

        public DynamicMethod Method {
            get { return m_method;  }   
        }
    }
}
