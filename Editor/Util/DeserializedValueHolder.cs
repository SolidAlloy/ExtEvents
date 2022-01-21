namespace ExtEvents.Editor
{
    using UnityEngine;

    public abstract class DeserializedValueHolder<T> : ScriptableObject
    {
        public T Value;
    }
}