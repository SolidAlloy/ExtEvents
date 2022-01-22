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
    public partial class PersistentListener
    {
        [SerializeField] internal PersistentArgument[] _persistentArguments;
        public IReadOnlyList<PersistentArgument> PersistentArguments => _persistentArguments;

        [SerializeField] internal Object _target;
        public Object Target => _target;

        [SerializeField] internal bool _isStatic;
        public bool IsStatic => _isStatic;
        
        [SerializeField] public UnityEventCallState CallState = UnityEventCallState.RuntimeOnly;

        [SerializeField, TypeOptions(IncludeAdditionalAssemblies = new[] { "Assembly-CSharp" }, ShowNoneElement = false)] internal TypeReference _staticType; // TODO: remove includeAdditionalAssemblies
        public Type StaticType => _staticType;

        [NonSerialized] internal bool _initializationComplete;
        [NonSerialized] private bool _initializationSuccessful;

        private object[] _arguments;

        private BaseInvokableCall _invokableCall;
        
        private PersistentListener() { }

        internal PersistentListener(string methodName, bool isStatic, Object target, UnityEventCallState callState, Type staticType, PersistentArgument[] persistentArguments)
        {
            _methodName = methodName;
            _isStatic = isStatic;
            _target = target;
            CallState = callState;
            _staticType = staticType;
            _persistentArguments = persistentArguments;
        }

        internal void Invoke([CanBeNull] object[] args)
        {
            // If no function is chosen, exit without any warnings.
            if (CallState == UnityEventCallState.Off || string.IsNullOrEmpty(_methodName))
                 return;

#if UNITY_EDITOR
            if (CallState == UnityEventCallState.RuntimeOnly && !Application.isPlaying)
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
                if (_staticType.Type == null)
                    Logger.LogWarning($"Tried to invoke a listener to an event but the declaring type is missing: {_staticType.TypeNameAndAssembly}");

                return _staticType.Type;
            }

            if (_target is null) 
                Logger.LogWarning("Tried to invoke a listener to an event but the target is missing");

            return _target?.GetType();
        }
        
        private void LogMethodInfoWarning()
        {
#if UNITY_EDITOR
            if (!PackageSettings.ShowInvocationWarning)
                return;
#endif

            string typeName = _isStatic ? _staticType.TypeNameAndAssembly : _target.GetType().Name;
            bool isProperty = _methodName.IsPropertySetter();
            string memberName = isProperty ? "property" : "method";
            string methodName = isProperty ? $"{_methodName.Substring(4)} setter" : _methodName;
            Logger.LogWarning($"Tried to invoke a listener to an event but the {memberName} {typeName}.{methodName} is missing.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FillWithDynamicArgs(object[] args)
        {
            if (args == null || _arguments == null)
                return;

            for (int i = 0; i < _arguments.Length; i++)
            {
                var serializedArg = _persistentArguments[i];
                
                if (!serializedArg._isSerialized)
                    _arguments[i] = args[serializedArg._index];
            }
        }

        private object[] GetArguments()
        {
            if (_persistentArguments.Length == 0)
                return null;

            var arguments = new object[_persistentArguments.Length];

            for (int i = 0; i < _persistentArguments.Length; i++)
            {
                var serializedArg = _persistentArguments[i];

                if (serializedArg._isSerialized)
                {
                    arguments[i] = serializedArg._value;
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
            foreach (var argument in _persistentArguments)
            {
                if (argument._type.Type == null)
                    yield return argument._type.TypeNameAndAssembly;
            }
        }

        private Type[] GetArgumentTypes()
        {
            var types = new Type[_persistentArguments.Length];

            for (int i = 0; i < _persistentArguments.Length; i++)
            {
                types[i] = _persistentArguments[i]._type.Type;

                if (types[i] == null)
                {
                    if (PackageSettings.ShowInvocationWarning)
                        Logger.LogWarning($"Tried to invoke a listener to an event but some of the argument types are missing: {string.Join(", ", GetNullArgumentTypeNames().Select(TypeReference.GetTypeNameFromNameAndAssembly))}.");
                    
                    return null;
                }
            }

            return types;
        }
    }
}