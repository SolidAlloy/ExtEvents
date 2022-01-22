namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;
    using SolidUtilities;
    using UnityEngine;

    public partial class PersistentListener
    {
        [SerializeField] internal string _methodName;

        /// <summary>
        /// The name of the method that is invoked by this listener.
        /// </summary>
        [PublicAPI]
        public string MethodName => _methodName;
        
        /// <summary>
        /// The method info of this listener, or null if the method is not set or missing.
        /// </summary>
        [PublicAPI]
        public MethodInfo MethodInfo
        {
            get
            {
                if (!_initializationComplete) 
                    Initialize();

                return _invokableCall?.Method;
            }
        }
        
        private static readonly Dictionary<Type, Func<object, MethodInfo, BaseInvokableCall>> _constructorDictionary =
            new Dictionary<Type, Func<object, MethodInfo, BaseInvokableCall>>();

        private static readonly Type[] _invokableCallConstructorArgTypes = new Type[] { typeof(object), typeof(MethodInfo) };

        private BaseInvokableCall GetInvokableCall(Type declaringType, Type[] paramTypes, object target)
        {
            var method = GetMethod(declaringType, paramTypes, _methodName, GetFlags(_isStatic));

            if (method == null)
                return null;

            bool isVoid = method.ReturnType == typeof(void);
            
            if (paramTypes.Length == 0 && isVoid)
                return new InvokableActionCall(target, method);
            
            Type genericTypeDefinition = GetInvokableCallDefinition(paramTypes.Length, isVoid);
            
            if (!isVoid)
                ArrayHelper.Add(ref paramTypes, method.ReturnType);

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
                0 => typeof(InvokableFuncCall<>),
                1 => typeof(InvokableFuncCall<,>),
                2 => typeof(InvokableFuncCall<,,>),
                3 => typeof(InvokableFuncCall<,,,>),
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