namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using JetBrains.Annotations;
    using SolidUtilities;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Scripting;
    using Object = UnityEngine.Object;

    /// <summary>
    /// An event whose listeners can be configured through editor UI.
    /// </summary>
    [Serializable]
    public abstract class BaseExtEvent
    {
        [SerializeField] internal PersistentListener[] _persistentListeners;

        /// <summary>
        /// A list of the persistent listeners of this event. <see cref="PersistentListener"/> is a listener that is configured in editor UI.
        /// </summary>
        [PublicAPI]
        public IReadOnlyList<PersistentListener> PersistentListeners => _persistentListeners;

        [SerializeField] internal bool Expanded = true;
        
        protected abstract Type[] EventParamTypes { get; }

        /// <summary>
        /// Prepares an event for invocation, so that it takes less time to invoke later.
        /// </summary>
        public void Initialize()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int index = 0; index < _persistentListeners.Length; index++)
            {
                _persistentListeners[index].Initialize();
            }
        }
        
        /// <summary>
        /// Adds a new persistent listener.
        /// </summary>
        /// <param name="methodDelegate">
        /// A method delegate that refers to a method eligible for invocation in this event. This can be a static method,
        /// or an instance method of a type derived from <see cref="UnityEngine.Object"/>.
        /// The method must not contain a parameter that is not serialized and is not one of the generic argument types of the event.
        /// </param>
        /// <param name="target">A target of the instance method, or null if the method is static.</param>
        /// <param name="callState">When the method must be invoked.</param>
        /// <param name="arguments">
        /// A list of the arguments that will be passed to the method. The number of arguments must match the number of
        /// parameters taken in by the method. An argument can have a pre-determined serialized value or be dynamic which
        /// means it will be passed when the event is invoked. When the argument is dynamic, an index is specified that
        /// reflects the index of an argument in the ExtEvent.Invoke() method.
        /// </param>
        /// <typeparam name="T">The delegate type of the method.</typeparam>
        [PublicAPI]
        public void AddPersistentListener<T>([NotNull] T methodDelegate, [CanBeNull] Object target, UnityEventCallState callState = UnityEventCallState.RuntimeOnly, [CanBeNull] params PersistentArgument[] arguments) 
            where T : Delegate
        {
            AddPersistentListener(methodDelegate.Method, target, callState, arguments);
        }
        
        /// <summary>
        /// Adds a new persistent listener.
        /// </summary>
        /// <param name="method">
        /// The method info of a method eligible for invocation in this event. This can be a static method,
        /// or an instance method of a type derived from <see cref="UnityEngine.Object"/>.
        /// The method must not contain a parameter that is not serialized and is not one of the generic argument types of the event.
        /// </param>
        /// <param name="target">A target of the instance method, or null if the method is static.</param>
        /// <param name="callState">When the method must be invoked.</param>
        /// <param name="arguments">
        /// A list of the arguments that will be passed to the method. The number of arguments must match the number of
        /// parameters taken in by the method. An argument can have a pre-determined serialized value or be dynamic which
        /// means it will be passed when the event is invoked. When the argument is dynamic, an index is specified that
        /// reflects the index of an argument in the ExtEvent.Invoke() method.
        /// </param>
        /// <exception cref="MethodNotEligibleException">The method passed is not eligible for invoking by <see cref="ExtEvent"/> with these generic arguments.</exception>
        /// <exception cref="TargetNullException">The instance method was passed but the target is null.</exception>
        /// <exception cref="ArgumentException">The number of arguments passed does not match the number of parameters the method takes in.</exception>
        /// <exception cref="ArgumentTypeMismatchException">A type of the argument passed does not match the type of the parameter taken in by the method.</exception>
        /// <exception cref="ArgumentIndexException">The index of a dynamic argument is either out of range of the arguments passed in ExtEvent.Invoke() or the type of the parameter by this index in ExtEvent.Invoke() does not match the type of the argument.</exception>
        public void AddPersistentListener([NotNull] MethodInfo method, [CanBeNull] Object target, UnityEventCallState callState = UnityEventCallState.RuntimeOnly, [CanBeNull] params PersistentArgument[] arguments)
        {
            var persistentListener = ExtEventHelper.CreatePersistentListener(method, target, EventParamTypes, callState, arguments);
            ArrayHelper.Add(ref _persistentListeners, persistentListener);
        }

        /// <summary>
        /// Removes a persistent listener at the index.
        /// </summary>
        /// <param name="index">The index of a persistent listener to remove.</param>
        [PublicAPI]
        public void RemovePersistentListenerAt(int index) => ArrayHelper.RemoveAt(ref _persistentListeners, index);

        /// <summary>
        /// Removes the specified persistent listener from the list of listeners.
        /// </summary>
        /// <param name="listener">The listener to remove.</param>
        /// <returns>Whether the listener was found in the list.</returns>
        [PublicAPI]
        public bool RemovePersistentListener(PersistentListener listener) => ArrayHelper.Remove(ref _persistentListeners, listener);
    }
}