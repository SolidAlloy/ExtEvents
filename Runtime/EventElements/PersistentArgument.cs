namespace ExtEvents
{
    using System;
    using TypeReferences;
    using UnityEngine;

    [Serializable]
    public class PersistentArgument
    {
        public int Index;
        public bool IsSerialized;
        public TypeReference Type;
        [SerializeField] internal string _serializedArg;
        [SerializeField] internal bool _canBeDynamic;

        public object Value
        {
            get
            {
                if (!IsSerialized)
                    throw new InvalidOperationException();

                return GetValue(_serializedArg, Type);
            }
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