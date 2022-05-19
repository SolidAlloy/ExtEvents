namespace ExtEvents
{
    using System;
    using System.Runtime.CompilerServices;
    using UnityEngine;
    using UnityEngine.Scripting;

    /// <summary>
    /// A class that allows to serialize any object through UnityEngine.JsonUtility.
    /// Without this class, Vector2 or int passed to JsonUtility won't be serialized properly.
    /// Also, by exposing 'object Value' it is possible to deserialize the object without knowing its type.
    /// </summary>
    public abstract class ArgumentHolder
    {
        [Preserve]
        public abstract unsafe void* ValuePointer { get; }

        [Preserve]
        public abstract object Value { get; set; }

        public abstract Type ValueType { get; }
    }

    [Serializable]
    public class ArgumentHolder<T> : ArgumentHolder
    {
        [SerializeField] private T _value;

        [Preserve]
        public override unsafe void* ValuePointer => Unsafe.AsPointer(ref _value);

        [Preserve]
        public override object Value
        {
            get => _value;
            set => _value = value == null ? default : (T) value;
        }

        public override Type ValueType => typeof(T);

        public ArgumentHolder() { }

        public ArgumentHolder(T value)
        {
            _value = value;
        }
    }
}