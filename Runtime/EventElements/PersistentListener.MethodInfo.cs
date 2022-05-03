namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
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

        private BaseInvokableCall GetInvokableCall(Type declaringType, Type[] paramTypes, object target)
        {
            var method = GetMethod(declaringType, paramTypes, _methodName, GetFlags(_isStatic));

            if (method == null)
                return null;

            bool isVoid = method.ReturnType == typeof(void);

            if (paramTypes.Length == 0 && isVoid)
                return new InvokableActionCall(target, method);

            if (!isVoid)
                ArrayHelper.Add(ref paramTypes, method.ReturnType);

            return InvokableCallCreator.CreateInvokableCall(paramTypes, isVoid, target, method);
        }

        internal static MethodInfo GetMethod(Type declaringType, Type[] argumentTypes, string methodName, BindingFlags flags)
        {
            return declaringType.GetMethod(methodName, flags, null, CallingConventions.Any, argumentTypes, null);
        }

        internal static class InvokableCallCreator
        {
            private static readonly Dictionary<int, MethodInfo> _createActionMethods = new Dictionary<int, MethodInfo>();
            private static readonly Dictionary<int, MethodInfo> _createFuncMethods = new Dictionary<int, MethodInfo>();

            private static readonly Dictionary<Type[], Func<object, MethodInfo, BaseInvokableCall>> _createActionCache =
                new Dictionary<Type[], Func<object, MethodInfo, BaseInvokableCall>>(new ArrayEqualityComparer<Type>());

            private static readonly Dictionary<Type[], Func<object, MethodInfo, BaseInvokableCall>> _createFuncCache =
                new Dictionary<Type[], Func<object, MethodInfo, BaseInvokableCall>>(new ArrayEqualityComparer<Type>());

            static InvokableCallCreator()
            {
                var staticMethods = typeof(BaseInvokableCall).GetMethods(BindingFlags.Public | BindingFlags.Static);

                foreach (var createActionMethod in staticMethods.Where(method => method.Name == nameof(BaseInvokableCall.CreateAction)))
                {
                    _createActionMethods.Add(createActionMethod.GetGenericArguments().Length, createActionMethod);
                }

                foreach (var createFuncMethod in staticMethods.Where(method => method.Name == nameof(BaseInvokableCall.CreateFunc)))
                {
                    _createFuncMethods.Add(createFuncMethod.GetGenericArguments().Length, createFuncMethod);
                }
            }

            public static BaseInvokableCall CreateInvokableCall(Type[] paramTypes, bool isVoid, object target, MethodInfo method)
            {
                try
                {
                    return isVoid
                        ? CreateActionInvokableCall(paramTypes, target, method)
                        : CreateFuncInvokableCall(paramTypes, target, method);
                }
#pragma warning disable CS0618
                catch (ExecutionEngineException)
#pragma warning restore CS0618
                {
                    Debug.LogWarning($"Tried to invoke a method {method} but there was no code generated for it ahead of time.");
                    return null;
                }
            }

            public static MethodInfo GetCreateMethod(Type[] paramTypes, bool isVoid)
            {
                var createMethodDefinition = isVoid ? _createActionMethods[paramTypes.Length] : _createFuncMethods[paramTypes.Length];
                return createMethodDefinition.MakeGenericMethod(paramTypes);
            }

            private static BaseInvokableCall CreateActionInvokableCall(Type[] paramTypes, object target, MethodInfo method)
            {
                if (_createActionCache.TryGetValue(paramTypes, out var createDelegate))
                    return createDelegate(target, method);

                var createMethodDefinition = _createActionMethods[paramTypes.Length];

                var createMethod = createMethodDefinition.MakeGenericMethod(paramTypes);
                createDelegate = (Func<object, MethodInfo, BaseInvokableCall>) Delegate.CreateDelegate(typeof(Func<object, MethodInfo, BaseInvokableCall>), createMethod);
                _createActionCache.Add(paramTypes, createDelegate);
                return createDelegate(target, method);
            }

            private static BaseInvokableCall CreateFuncInvokableCall(Type[] paramTypes, object target, MethodInfo method)
            {
                if (_createFuncCache.TryGetValue(paramTypes, out var createDelegate))
                    return createDelegate(target, method);

                var createMethodDefinition = _createFuncMethods[paramTypes.Length];
                var createMethod = createMethodDefinition.MakeGenericMethod(paramTypes);
                createDelegate = (Func<object, MethodInfo, BaseInvokableCall>) Delegate.CreateDelegate(typeof(Func<object, MethodInfo, BaseInvokableCall>), createMethod);
                _createFuncCache.Add(paramTypes, createDelegate);
                return createDelegate(target, method);
            }
        }
    }
}