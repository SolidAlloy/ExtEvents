namespace ExtEvents
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class ExtEvent : BaseExtEvent
    {
        public IReadOnlyList<Action> DynamicListeners { get; }
        public IReadOnlyList<Action> PersistentListeners { get; } // maybe not a list of actions

        public void Invoke()
        {

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