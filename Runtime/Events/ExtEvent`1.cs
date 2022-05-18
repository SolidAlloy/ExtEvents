namespace ExtEvents
{
    using System;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;

    [Serializable]
    public class ExtEvent<T> : BaseExtEvent
    {
        private readonly unsafe void*[] _arguments = new void*[1];

        private Type[] _eventParamTypes;
        protected override Type[] EventParamTypes => _eventParamTypes ??= new Type[] { typeof(T) };

        /// <summary>
        /// The dynamic listeners list that you can add your listener to.
        /// </summary>
        [PublicAPI]
        public event Action<T> DynamicListeners;

        internal override Delegate _dynamicListeners => DynamicListeners;

        /// <summary>
        /// Invokes all listeners of the event.
        /// </summary>
        [PublicAPI]
        public void Invoke(T arg)
        {
            unsafe
            {
                _arguments[0] = Unsafe.AsPointer(ref arg);

                // ReSharper disable once ForCanBeConvertedToForeach
                for (int index = 0; index < _persistentListeners.Length; index++)
                {
                    _persistentListeners[index].Invoke(_arguments);
                }
            }

            DynamicListeners?.Invoke(arg);
        }

        public static ExtEvent<T> operator +(ExtEvent<T> extEvent, Action<T> listener)
        {
            if (extEvent == null)
                return null;

            extEvent.DynamicListeners += listener;
            return extEvent;
        }

        public static ExtEvent<T> operator -(ExtEvent<T> extEvent, Action<T> listener)
        {
            if (extEvent == null)
                return null;

            extEvent.DynamicListeners -= listener;
            return extEvent;
        }
    }
}