namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    public partial class PersistentListener
    {
        [SerializeField] internal string _methodName;
        
        private static readonly Dictionary<Type, Func<object, MethodInfo, BaseInvokableCall>> _constructorDictionary =
            new Dictionary<Type, Func<object, MethodInfo, BaseInvokableCall>>();

        private static readonly Type[] _invokableCallConstructorArgTypes = new Type[] { typeof(object), typeof(MethodInfo) };

        private BaseInvokableCall GetInvokableCall(Type declaringType, Type[] paramTypes, object target)
        {
            var method = GetMethod(declaringType, paramTypes, _methodName, GetFlags(_isStatic));

            if (method == null)
                return null;

            if (paramTypes.Length == 0)
                return new InvokableActionCall(target, method);
            
            Type genericTypeDefinition = GetInvokableCallDefinition(paramTypes.Length, method.ReturnType == typeof(void));
            Type invokableCallType = genericTypeDefinition.MakeGenericType(paramTypes);
            return CreateInvokableCall(invokableCallType, target, method);
        }

        internal static MethodInfo GetMethod(Type declaringType, Type[] argumentTypes, string methodName, BindingFlags flags)
        {
            return declaringType.GetMethod(methodName, flags, null, CallingConventions.Any, argumentTypes, null);
        }

        private Type GetInvokableCallDefinition(int paramTypesCount, bool isVoid)
        {
            if (isVoid)
            {
                return paramTypesCount switch
                {
                    1 => typeof(InvokableActionCall<>),
                    2 => typeof(InvokableActionCall<,>),
                    3 => typeof(InvokableActionCall<,,>),
                    _ => throw new NotImplementedException()
                };
            }
            
            return paramTypesCount switch
            {
                1 => typeof(InvokableFuncCall<>),
                2 => typeof(InvokableFuncCall<,>),
                3 => typeof(InvokableFuncCall<,,>),
                _ => throw new NotImplementedException()
            };
        }

        private BaseInvokableCall CreateInvokableCall(Type type, object target, MethodInfo method)
        {
            if (_constructorDictionary.TryGetValue(type, out var constructor))
                return constructor(target, method);

            var createMethod = type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, null, _invokableCallConstructorArgTypes, null);
            // ReSharper disable once AssignNullToNotNullAttribute
            constructor = (Func<object, MethodInfo, BaseInvokableCall>) Delegate.CreateDelegate(typeof(Func<object, MethodInfo, BaseInvokableCall>), createMethod);
            _constructorDictionary.Add(type, constructor);
            return constructor(target, method);
        }
    }
}