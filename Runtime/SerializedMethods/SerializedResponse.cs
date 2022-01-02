namespace ExtEvents
{
    using System;
    using System.Reflection;
    using UnityEngine;
    using Object = UnityEngine.Object;

    // [Serializable]
    // public class Response
    // {
    //     [SerializeField] private SerializedMember _member;
    //
    //
    //
    //     public void Invoke()
    //     {
    //         if (_member.AnyTypeNull(_member.ArgumentTypes))
    //         {
    //             Debug.LogWarning("One of the argument types changed. Cannot invoke the response anymore");
    //             return;
    //         }
    //
    //         // get invokable from serialized member and invoke it.
    //         _invokable ??= _member.GetInvokable(_isStatic
    //             ? BindingFlags.Public | BindingFlags.Static
    //             : BindingFlags.Public | BindingFlags.Instance);
    //
    //     }
    // }

    [Serializable]
    public class TypedSerializedResponse
    {
        // list all possible combinations of arguments as separate fields (with up to three args)
        // duplicate all the combinations with funcs that have a return value.

        // variables where any variable is serialized cannot be cast to delegate - any way to speed them up?
    }

    [Serializable]
    public class SerializedResponse
    {
        // link to serialized member
        // serialized field for any argument of the action
        // enum for the argument field whether it is dynamic or serialized
        // there can be up to three arguments on a response and all of them need to have the ability of a serializedField
        // let's try to think of a solution for responses with one serialized argument at least. How does UnityEvent solve it?
        // Unity just lists all possible argument values in a structure.
        // I can use JsonUtility to convert serialized fields to string and back, and store the type of fields. If performance needs to be considered, add a bunch of fields for common types like Unity does.
        [SerializeField] private string[] _serializedArguments;
        [SerializeField] private SerializedMember _member;
        [SerializeField] private Object _target;
        [SerializeField] private bool _isStatic;

        [SerializeField] private BuiltResponse _builtResponse;

        [NonSerialized] private bool _initialized;
        private object[] _arguments;

        private Invokable _invokable;
        private object _objectTarget;

        private BindingFlags Flags => BindingFlags.Public | (_isStatic ? BindingFlags.Static : BindingFlags.Instance);

        public SerializedResponse()
        {
            _initialized = false;
        }

        public void Invoke()
        {
            if (_initialized)
            {
                InvokeImpl();
                return;
            }

            Initialize();
            _initialized = true;
            InvokeImpl();
        }

        public MethodInfo GetMethod() => _member.GetMethod(Flags);

        private void InvokeImpl()
        {
            if (_builtResponse != null)
            {
                _builtResponse.Invoke(_objectTarget, _arguments);
                return;
            }

            _invokable.Invoke(_objectTarget, _arguments);
        }

        private void Initialize()
        {
            if (_builtResponse == null)
                _invokable = _member.GetInvokable(Flags);

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

        private object[] GetArguments()
        {
            if (_serializedArguments.Length == 0)
                return null;

            var arguments = new object[_serializedArguments.Length];

            for (int i = 0; i < _serializedArguments.Length; i++)
            {
                var type = typeof(ArgumentHolder<>).MakeGenericType(_member.ArgumentTypes[i]);
                var argumentHolder = (ArgumentHolder) JsonUtility.FromJson(_serializedArguments[i], type);
                arguments[i] = argumentHolder.Value;
            }

            return arguments;
        }
    }
}