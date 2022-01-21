namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;
    using TypeReferences;
    using UnityEngine;
    using UnityEngine.Events;
    using Object = UnityEngine.Object;

    [Serializable]
    public partial class SerializedResponse
    {
        [SerializeField] internal SerializedArgument[] _serializedArguments;
        [SerializeField] internal Object _target;
        [SerializeField] internal bool _isStatic;
        [SerializeField] internal UnityEventCallState _callState = UnityEventCallState.RuntimeOnly;
        [SerializeField, TypeOptions(IncludeAdditionalAssemblies = new[] { "Assembly-CSharp" }, ShowNoneElement = false)] internal TypeReference _type; // TODO: remove includeAdditionalAssemblies

        [NonSerialized] internal bool _initializationComplete = false;
        [NonSerialized] private bool _initializationSuccessful = false;

        private object[] _arguments;

        private BaseInvokableCall _invokableCall;

        public SerializedResponse()
        {
            _initializationComplete = false;
        }

        public void Invoke([CanBeNull] object[] args)
        {
            // If no function is chosen, exit without any warnings.
            if (_callState == UnityEventCallState.Off || string.IsNullOrEmpty(_methodName))
                 return;

#if UNITY_EDITOR
            if (_callState == UnityEventCallState.RuntimeOnly && !Application.isPlaying)
                return;
#endif

            if (_initializationComplete)
            {
                if (_initializationSuccessful)
                    InvokeImpl(args);

                return;
            }

            _initializationSuccessful = Initialize();
            _initializationComplete = true;
            InvokeImpl(args);
        }
        
        internal static BindingFlags GetFlags(bool isStatic) => BindingFlags.Public | BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance | BindingFlags.Static);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvokeImpl(object[] args)
        {
            FillWithDynamicArgs(args);
            _invokableCall.Invoke(_arguments);
        }

        public bool Initialize()
        {
            var declaringType = GetDeclaringType();

            if (declaringType == null)
                return false;
            
            var argumentTypes = GetArgumentTypes();

            if (argumentTypes == null)
                return false;
            
            var target = _isStatic ? null : _target;
            _invokableCall = GetInvokableCall(declaringType, argumentTypes, target);

            if (_invokableCall == null)
            {
                LogMethodInfoWarning();
                return false;
            }

            _arguments = GetArguments();
            return true;
        }

        private Type GetDeclaringType()
        {
            if (_isStatic)
            {
                if (_type.Type == null)
                    Logger.LogWarning($"Tried to invoke a response to an event but the declaring type is missing: {_type.TypeNameAndAssembly}");

                return _type.Type;
            }

            if (_target is null) 
                Logger.LogWarning("Tried to invoke a response to an event but the target is missing");

            return _target?.GetType();
        }
        
        private void LogMethodInfoWarning()
        {
#if UNITY_EDITOR
            if (!PackageSettings.ShowInvocationWarning)
                return;
#endif

            string typeName = _isStatic ? _type.TypeNameAndAssembly : _target.GetType().Name;
            bool isProperty = _methodName.IsPropertySetter();
            string memberName = isProperty ? "property" : "method";
            string methodName = isProperty ? $"{_methodName.Substring(4)} setter" : _methodName;
            Logger.LogWarning($"Tried to invoke a response to an event but the {memberName} {typeName}.{methodName} is missing.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FillWithDynamicArgs(object[] args)
        {
            if (args == null || _arguments == null)
                return;

            for (int i = 0; i < _arguments.Length; i++)
            {
                var serializedArg = _serializedArguments[i];
                
                if (!serializedArg.IsSerialized)
                    _arguments[i] = args[serializedArg.Index];
            }
        }

        private object[] GetArguments()
        {
            if (_serializedArguments.Length == 0)
                return null;

            var arguments = new object[_serializedArguments.Length];

            for (int i = 0; i < _serializedArguments.Length; i++)
            {
                var serializedArg = _serializedArguments[i];

                if (serializedArg.IsSerialized)
                {
                    arguments[i] = serializedArg.Value;
                }
                else
                {
                    arguments[i] = null;
                }
            }

            return arguments;
        }

        private IEnumerable<string> GetNullArgumentTypeNames()
        {
            foreach (var argument in _serializedArguments)
            {
                if (argument.Type.Type == null)
                    yield return argument.Type.TypeNameAndAssembly;
            }
        }

        private Type[] GetArgumentTypes()
        {
            var types = new Type[_serializedArguments.Length];

            for (int i = 0; i < _serializedArguments.Length; i++)
            {
                types[i] = _serializedArguments[i].Type.Type;

                if (types[i] == null)
                {
                    if (PackageSettings.ShowInvocationWarning)
                        Logger.LogWarning($"Tried to invoke a response to an event but some of the argument types are missing: {string.Join(", ", GetNullArgumentTypeNames().Select(TypeReference.GetTypeNameFromNameAndAssembly))}.");
                    
                    return null;
                }
            }

            return types;
        }
    }
}