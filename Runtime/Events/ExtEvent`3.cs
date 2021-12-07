namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class ExtEvent<T1, T2, T3> : BaseExtEvent
    {
        [SerializeField] private string _arg1Name;
        [SerializeField] private string _arg2Name;
        [SerializeField] private string _arg3Name;

        public IReadOnlyList<Action<T1, T2, T3>> DynamicListeners { get; }
        public IReadOnlyList<Action<T1, T2, T3>> PersistentListeners { get; }

        public ExtEvent(string arg1Name, string arg2Name, string arg3Name)
        {
            _arg1Name = arg1Name;
            _arg2Name = arg2Name;
            _arg3Name = arg3Name;
        }

        public void Invoke(T1 arg1, T2 arg2, T3 arg3)
        {

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