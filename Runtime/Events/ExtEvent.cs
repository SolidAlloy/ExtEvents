namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using JetBrains.Annotations;
    using UnityEngine;

    [Serializable]
    public class ExtEvent : BaseExtEvent
    {
        /// <summary>
        /// The dynamic listeners list that you can add your listener to.
        /// </summary>
        [PublicAPI]
        public event Action DynamicListeners;

        public void Invoke()
        {
            for (int index = 0; index < _persistentListeners.Length; index++)
            {
                _persistentListeners[index].Invoke(null);
            }
        }

        public void AddPersistentListener(Action action)
        {
            
        }

        public void RemovePersistentListener(Action action)
        {

        }
    }
}