namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class ExtEvent<T1, T2, T3> : BaseExtEvent
    {
        [SerializeField] private SerializedResponse[] _responses;

        private readonly object[] _arguments = new object[3];

        public IReadOnlyList<Action<T1, T2, T3>> DynamicListeners { get; }
        public IReadOnlyList<Action<T1, T2, T3>> PersistentListeners { get; }

        public void Invoke(T1 arg1, T2 arg2, T3 arg3)
        {
            _arguments[0] = arg1;
            _arguments[1] = arg2;
            _arguments[2] = arg3;

            for (int index = 0; index < _responses.Length; index++)
            {
                _responses[index].Invoke(_arguments);
            }
        }

        public void AddPersistentListener(Action<T1, T2, T3> action)
        {

        }

        public void RemovePersistentListener(Action<T1, T2, T3> action)
        {

        }

        public void AddDynamicListener(Action<T1, T2, T3> action)
        {

        }

        public void RemoveDynamicListener(Action<T1, T2, T3> action)
        {

        }
    }
}