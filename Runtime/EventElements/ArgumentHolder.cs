namespace ExtEvents
{
    using System;
    using UnityEngine;

    /// <summary>
    /// A class that allows to serialize any object through UnityEngine.JsonUtility.
    /// Without this class, Vector2 or int passed to JsonUtility won't be serialized properly.
    /// Also, by exposing 'object Value' it is possible to deserialize the object without knowing its type.
    /// </summary>
    public abstract class ArgumentHolder
    {
        public abstract object Value { get; }
    }

    [Serializable]
    public class ArgumentHolder<T> : ArgumentHolder
    {
        [SerializeField] private T _value;

        public override object Value => _value;

        public ArgumentHolder(T value)
        {
            _value = value;
        }
    }
}