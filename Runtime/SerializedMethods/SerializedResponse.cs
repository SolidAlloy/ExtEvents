namespace ExtEvents
{
    using System;
    using System.Reflection;
    using UnityEngine;
    using Object = UnityEngine.Object;

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

        private Invokable _invokable;

        public void Invoke()
        {
            if (_member.AnyTypeNull(_member.ArgumentTypes))
            {
                Debug.LogWarning("One of the argument types changed. Cannot invoke the response anymore");
                return;
            }

            // get invokable from serialized member and invoke it.
            _invokable ??= _member.GetInvokable(_isStatic
                ? BindingFlags.Public | BindingFlags.Static
                : BindingFlags.Public | BindingFlags.Instance);

            var arguments = new object[_serializedArguments.Length];

            for (int i = 0; i < _serializedArguments.Length; i++)
            {
                var type = typeof(ArgumentHolder<>).MakeGenericType(_member.ArgumentTypes[i]);
                var argumentHolder = (ArgumentHolder) JsonUtility.FromJson(_serializedArguments[i], type);
                arguments[i] = argumentHolder.Value;
            }

            _invokable.Invoke(GetTarget(), arguments);
        }

        private object GetTarget()
        {
            if (_isStatic)
                return null;

            if (_target == null)
            {
                Debug.LogWarning("Trying to invoke the response but the target is null");
                return null;
            }

            return _target;
        }
    }
}