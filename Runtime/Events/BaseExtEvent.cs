namespace ExtEvents
{
    using System;
    using UnityEngine;

    [Serializable]
    public abstract class BaseExtEvent
    {
        [SerializeField] internal PersistentListener[] _persistentListeners;
        
#if UNITY_EDITOR
        [SerializeField] internal bool Expanded = true;
#endif

        public void Initialize()
        {
            for (int index = 0; index < _persistentListeners.Length; index++)
            {
                _persistentListeners[index].Initialize();
            }
        }
    }
}