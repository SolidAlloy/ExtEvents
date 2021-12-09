namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class ExtEvent<T> : BaseExtEvent
    {
        [SerializeField] private string _argName;
        [SerializeField] private List<SerializedResponse<T>> _responses;

        public IReadOnlyList<Action<T>> DynamicListeners { get; }
        public IReadOnlyList<Action<T>> PersistentListeners { get; }

        public ExtEvent(string argName)
        {
            _argName = argName;
        }

        public void Invoke(T arg)
        {

        }

        public void AddPersistentListener(Action<T> action)
        {

        }

        public void RemovePersistentListener(Action<T> action)
        {

        }

        public void AddDynamicListener(Action<T> action)
        {

        }

        public void RemoveDynamicListener(Action<T> action)
        {

        }
    }
}