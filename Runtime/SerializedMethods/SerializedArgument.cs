namespace ExtEvents
{
    using System;
    using TypeReferences;
    using UnityEngine;

    [Serializable]
    public class SerializedArgument
    {
        public int Index;
        public bool IsSerialized;
        public TypeReference Type;
        [SerializeField] private string _serializedArg;

        public object Value
        {
            get
            {
                if (!IsSerialized)
                    throw new InvalidOperationException();

                var type = typeof(ArgumentHolder<>).MakeGenericType(Type);
                var argumentHolder = (ArgumentHolder) JsonUtility.FromJson(_serializedArg, type);
                return argumentHolder.Value;
            }
        }
    }
}