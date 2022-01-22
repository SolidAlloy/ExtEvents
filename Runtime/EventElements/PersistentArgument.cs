namespace ExtEvents
{
    using System;
    using JetBrains.Annotations;
    using TypeReferences;
    using UnityEngine;

    /// <summary>
    /// An argument that can be dynamic or serialized, and is configured through editor UI as a part of <see cref="ExtEvent"/>.
    /// </summary>
    [Serializable]
    public class PersistentArgument
    {
        [SerializeField] internal int _index;
        
        /// <summary>
        /// An index of the argument passed through ExtEvent.Invoke().
        /// </summary>
        [PublicAPI]
        public int Index => _index;
        
        [SerializeField] internal bool _isSerialized;
        
        /// <summary>
        /// Whether the argument is serialized or dynamic.
        /// </summary>
        [PublicAPI]
        public bool IsSerialized => _isSerialized;
        
        [SerializeField] internal TypeReference _type;
        
        /// <summary>
        /// The type of the argument.
        /// </summary>
        [PublicAPI]
        public Type Type => _type;
        
        [SerializeField] internal string _serializedArg;
        [SerializeField] internal bool _canBeDynamic;

        internal object _value => GetValue(_serializedArg, _type);

        /// <summary>
        /// The value of the argument if it is serialized.
        /// </summary>
        /// <exception cref="Exception">The argument is not serialized but a dynamic one.</exception>
        [PublicAPI]
        public object Value
        {
            get
            {
                if (!_isSerialized)
                    throw new Exception("Tried to access a persistent value of an argument but the argument is dynamic");

                if (_type.Type == null || string.IsNullOrEmpty(_serializedArg))
                    return null;

                return GetValue(_serializedArg, _type);
            }
        }

        /// <summary>
        /// Creates a serialized argument.
        /// </summary>
        /// <param name="value">The initial value of the serialized argument.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <returns>An instance of the serialized argument.</returns>
        public static PersistentArgument CreateSerialized<T>(T value)
        {
            return CreateSerialized(value, typeof(T));
        }

        /// <summary>
        /// Creates a serialized argument.
        /// </summary>
        /// <param name="value">The initial value of the serialized argument.</param>
        /// <param name="argumentType">The type of the value.</param>
        /// <returns>An instance of the serialized argument.</returns>
        public static PersistentArgument CreateSerialized(object value, Type argumentType)
        {
            return new PersistentArgument
            {
                _isSerialized = true,
                _type = argumentType,
                _serializedArg = SerializeValue(value, argumentType)
            };
        }

        /// <summary>
        /// Creates a dynamic argument.
        /// </summary>
        /// <param name="eventArgumentIndex">An index of the argument passed through ExtEvent.Invoke().</param>
        /// <param name="argumentType">The type of the argument.</param>
        /// <returns>An instance of the dynamic argument.</returns>
        public static PersistentArgument CreateDynamic(int eventArgumentIndex, Type argumentType)
        {
            return new PersistentArgument
            {
                _isSerialized = false,
                _type = argumentType,
                _index = eventArgumentIndex,
                _canBeDynamic = true
            };
        }

        public static object GetValue(string serializedArg, Type valueType)
        {
            var type = typeof(ArgumentHolder<>).MakeGenericType(valueType);
            var argumentHolder = (ArgumentHolder) JsonUtility.FromJson(serializedArg, type);
            return argumentHolder?.Value;
        }

        public static string SerializeValue(object value, Type valueType)
        {
            var argHolderType = typeof(ArgumentHolder<>).MakeGenericType(valueType);
            var argHolder = Activator.CreateInstance(argHolderType, value);
            return JsonUtility.ToJson(argHolder);
        }
    }
}