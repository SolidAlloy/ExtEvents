namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class ExtEvent<T> : BaseExtEvent
    {
        [SerializeField] private SerializedResponse[] _responses;

        private readonly object[] _arguments = new object[1];

        public IReadOnlyList<Action<T>> DynamicListeners { get; }
        public IReadOnlyList<Action<T>> PersistentListeners { get; }

        public void Invoke(T arg)
        {
            _arguments[0] = arg;

            for (int index = 0; index < _responses.Length; index++)
            {
                _responses[index].Invoke(_arguments);
            }
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