namespace ExtEvents
{
    using System;
    using System.Reflection;
    using JetBrains.Annotations;
    using SolidUtilities;
    using UnityEngine;
    using UnityEngine.Events;
    using Object = UnityEngine.Object;

    [Serializable]
    public abstract class BaseExtEvent
    {
        [SerializeField] internal PersistentListener[] _persistentListeners;
        
#if UNITY_EDITOR
        [SerializeField] internal bool Expanded = true;
#endif
        
        protected abstract Type[] EventParamTypes { get; }

        public void Initialize()
        {
            for (int index = 0; index < _persistentListeners.Length; index++)
            {
                _persistentListeners[index].Initialize();
            }
        }
        
        public void AddPersistentListener([NotNull] MethodInfo method, [CanBeNull] Object target, UnityEventCallState callState = UnityEventCallState.RuntimeOnly, [CanBeNull] params PersistentArgument[] arguments)
        {
            var persistentListener = ExtEventHelper.CreatePersistentListener(method, target, EventParamTypes, callState, arguments);
            ArrayHelper.Add(ref _persistentListeners, persistentListener);
        }
    }
}