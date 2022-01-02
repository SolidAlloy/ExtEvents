namespace ExtEvents
{
    using UnityEngine;

    public abstract class BuiltResponse : ScriptableObject
    {
        public abstract void Invoke(object obj, object[] args);
    }
}