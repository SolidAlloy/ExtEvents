namespace ExtEvents
{
    using System;
    using UnityEngine;

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