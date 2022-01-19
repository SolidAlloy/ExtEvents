namespace ExtEvents
{
    using System;
    using UnityEngine;

    [Serializable]
    public abstract class BaseExtEvent
    {
        [SerializeField] internal SerializedResponse[] _responses;
        
#if UNITY_EDITOR
        [SerializeField] internal bool Expanded = true;
#endif

        public void Initialize()
        {
            for (int index = 0; index < _responses.Length; index++)
            {
                _responses[index].Initialize();
            }
        }
    }
}