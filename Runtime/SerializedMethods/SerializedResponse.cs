namespace ExtEvents
{
    using System;
    using System.Reflection;
    using JetBrains.Annotations;
    using TypeReferences;
    using UnityEngine;
    using UnityEngine.Events;
    using Object = UnityEngine.Object;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [Serializable]
    public partial class SerializedResponse
    {
        [SerializeField] internal SerializedArgument[] _serializedArguments;
        [SerializeField] internal Object _target;
        [SerializeField] internal bool _isStatic;
        [SerializeField] internal UnityEventCallState _callState = UnityEventCallState.RuntimeOnly;
        [SerializeField, TypeOptions(IncludeAdditionalAssemblies = new[] { "Assembly-CSharp" }, ShowNoneElement = false)] internal TypeReference _type; // TODO: remove includeAdditionalAssemblies

        [NonSerialized] internal bool _initialized;

        private object[] _arguments;

        private EfficientInvoker _invokable;
        private object _objectTarget;

        private BindingFlags Flags => BindingFlags.Public | (_isStatic ? BindingFlags.Static : BindingFlags.Instance | BindingFlags.Static);

        public SerializedResponse()
        {
            _initialized = false;
        }

        public void Invoke([NotNull] object[] args)
        {
            if (_callState == UnityEventCallState.Off)
                return;

#if UNITY_EDITOR
            if (_callState == UnityEventCallState.RuntimeOnly && !EditorApplication.isPlaying)
                return;
    #endif

            if (_initialized)
            {
                InvokeImpl(args);
                return;
            }

            Initialize();
            _initialized = true;
            InvokeImpl(args);
        }

        // TODO remove
        public MethodInfo GetMethod() => GetMethod(_isStatic ? _type.Type : _target.GetType(), GetArgumentTypes());

        private void InvokeImpl(object[] args)
        {
            FillWithDynamicArgs(args);

            if (_invokable != null)
            {
                _invokable.Invoke(_objectTarget, _arguments);
                return;
            }

            LogInvocationWarning();
        }

        private void LogInvocationWarning()
        {
#if UNITY_EDITOR
            if (!PackageSettings.ShowInvocationWarning)
                return;
#endif

            string typeName = _isStatic ? _type.TypeNameAndAssembly : _target?.GetType().Name ?? "Unknown_Type";
            bool isProperty = _methodName.IsPropertySetter();
            string memberName = isProperty ? "property" : "method";
            string methodName = isProperty ? $"{_methodName.Substring(4)} setter" : _methodName;
            Debug.LogWarning($"Tried to invoke a response to an event but the {memberName} {typeName}.{methodName} is missing.");
        }

        public void Initialize()
        {
            var types = GetArgumentTypes();
            var declaringType = _isStatic ? _type.Type : _target.GetType();

            if (types != null && declaringType != null)
            {
                _invokable = GetInvokable(declaringType, types);
            }

            _objectTarget = GetTarget();
            _arguments = GetArguments();
        }

        private object GetTarget()
        {
            if (!_isStatic)
            {
                if (_target != null)
                    return _target;

                Debug.LogWarning("Trying to invoke the response but the target is null");
                return null;
            }

            return null;
        }

        private void FillWithDynamicArgs(object[] args)
        {
            if (args.Length == 0)
                return;

            for (int i = 0; i < _arguments.Length; i++)
            {
                _arguments[i] ??= args[_serializedArguments[i].Index];
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

        private Type[] GetArgumentTypes()
        {
            var types = new Type[_serializedArguments.Length];

            for (int i = 0; i < _serializedArguments.Length; i++)
            {
                types[i] = _serializedArguments[i].Type.Type;

                if (types[i] == null)
                {
                    return null;
                }
            }

            return types;
        }
    }
}