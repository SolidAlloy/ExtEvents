namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class ExtEvent : BaseExtEvent
    {
        [SerializeField] private List<SerializedResponse> _responses;

        public IReadOnlyList<Action> DynamicListeners { get; }
        public IReadOnlyList<Action> PersistentListeners { get; } // maybe not a list of actions

        public void Invoke()
        {
            foreach (var response in _responses)
            {
                response.Invoke();
            }
        }

        public void AddPersistentListener(Action action)
        {

        }

        public void RemovePersistentListener(Action action)
        {

        }

        public void AddDynamicListener(Action action)
        {

        }

        public void RemoveDynamicListener(Action action)
        {

        }
    }
}