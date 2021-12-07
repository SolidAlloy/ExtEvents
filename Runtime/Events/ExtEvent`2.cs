namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class ExtEvent<T1, T2> : BaseExtEvent
    {
        [SerializeField] private string _arg1Name;
        [SerializeField] private string _arg2Name;

        public IReadOnlyList<Action<T1, T2>> DynamicListeners { get; }
        public IReadOnlyList<Action<T1, T2>> PersistentListeners { get; }

        public ExtEvent(string arg1Name, string arg2Name)
        {
            _arg1Name = arg1Name;
            _arg2Name = arg2Name;
        }

        public void Invoke(T1 arg1, T2 arg2)
        {

        }

        public void AddPersistentListener(Action<T1, T2> action)
        {

        }

        public void RemovePersistentListener(Action<T1, T2> action)
        {

        }

        public void AddDynamicListener(Action<T1, T2> action)
        {

        }

        public void RemoveDynamicListener(Action<T1, T2> action)
        {

        }
    }
}