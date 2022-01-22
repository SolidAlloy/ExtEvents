namespace ExtEvents
{
    using System;
    using TypeReferences;
    using UnityEngine;

    [Serializable]
    public class PersistentArgument
    {
        [SerializeField] internal int _index;
        public int Index => _index;
        
        [SerializeField] internal bool _isSerialized;
        public bool IsSerialized => _isSerialized;
        
        [SerializeField] internal TypeReference _type;
        public Type Type => _type;
        
        [SerializeField] internal string _serializedArg;
        [SerializeField] internal bool _canBeDynamic;

        internal object _value => GetValue(_serializedArg, _type);

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

        public static PersistentArgument CreateSerialized<T>(T value)
        {
            return CreateSerialized(value, typeof(T));
        }

        public static PersistentArgument CreateSerialized(object value, Type argumentType)
        {
            return new PersistentArgument
            {
                _isSerialized = true,
                _type = argumentType,
                _serializedArg = SerializeValue(value, argumentType)
            };
        }

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