namespace ExtEvents
{
    using System;
    using JetBrains.Annotations;

    [Serializable]
    public class ExtEvent<T1, T2, T3> : BaseExtEvent
    {
        private readonly object[] _arguments = new object[3];
        
        private Type[] _eventParamTypes;
        protected override Type[] EventParamTypes => _eventParamTypes ??= new Type[] { typeof(T1), typeof(T2), typeof(T3) };
        
        /// <summary>
        /// The dynamic listeners list that you can add your listener to.
        /// </summary>
        [PublicAPI]
        public event Action<T1, T2, T3> DynamicListeners;
        internal override Delegate _dynamicListeners => DynamicListeners;

        /// <summary>
        /// Invokes all listeners of the event.
        /// </summary>
        [PublicAPI]
        public void Invoke(T1 arg1, T2 arg2, T3 arg3)
        {
            _arguments[0] = arg1;
            _arguments[1] = arg2;
            _arguments[2] = arg3;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (int index = 0; index < _persistentListeners.Length; index++)
            {
                _persistentListeners[index].Invoke(_arguments);
            }
            
            DynamicListeners?.Invoke(arg1, arg2, arg3);
        }
        
        public static ExtEvent<T1, T2, T3> operator +(ExtEvent<T1, T2, T3> extEvent, Action<T1, T2, T3> listener)
        {
            if (extEvent == null)
                return null;

            extEvent.DynamicListeners += listener;
            return extEvent;
        }
        
        public static ExtEvent<T1, T2, T3> operator -(ExtEvent<T1, T2, T3> extEvent, Action<T1, T2, T3> listener)
        {
            if (extEvent == null)
                return null;

            extEvent.DynamicListeners -= listener;
            return extEvent;
        }
    }
}