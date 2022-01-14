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

    public enum MemberType { Field, Property, Method }

    [Serializable]
    public partial class SerializedResponse
    {
        // link to serialized member
        // serialized field for any argument of the action
        // enum for the argument field whether it is dynamic or serialized
        // there can be up to three arguments on a response and all of them need to have the ability of a serializedField
        // let's try to think of a solution for responses with one serialized argument at least. How does UnityEvent solve it?
        // Unity just lists all possible argument values in a structure.
        // I can use JsonUtility to convert serialized fields to string and back, and store the type of fields. If performance needs to be considered, add a bunch of fields for common types like Unity does.
        [SerializeField] internal SerializedArgument[] _serializedArguments;
        [SerializeField] internal Object _target;
        [SerializeField] internal bool _isStatic;
        [SerializeField] internal UnityEventCallState _callState = UnityEventCallState.RuntimeOnly;
        [SerializeField, TypeOptions(IncludeAdditionalAssemblies = new []{ "Assembly-CSharp" }, ShowNoneElement = false)] internal TypeReference _type; // TODO: remove includeAdditionalAssemblies
        [SerializeField] private BuiltResponse _builtResponse;

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

            if (_builtResponse != null)
            {
                _builtResponse.Invoke(_objectTarget, _arguments);
                return;
            }

            if (_invokable != null)
            {
                _invokable.Invoke(_objectTarget, _arguments);
                return;
            }

            LogInvocationWarning();
        }

        private void LogInvocationWarning()
        {
            if (!PackageSettings.ShowInvocationWarning)
                return;

            string typeName = _isStatic ? _type.TypeNameAndAssembly : _target?.GetType().Name;
            string memberType = _memberType.ToString().ToLower();
            Debug.LogWarning($"Tried to invoke a response to an event but the {memberType} {typeName}.{_memberName} is missing.");
        }

        public void Initialize()
        {
            if (_builtResponse == null)
            {
                var types = GetArgumentTypes();
                var declaringType = _isStatic ? _type.Type : _target.GetType();

                if (types != null && declaringType != null)
                {
                    _invokable = GetInvokable(declaringType, types);
                }
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