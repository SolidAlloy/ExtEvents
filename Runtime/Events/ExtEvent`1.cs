namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using JetBrains.Annotations;
    using UnityEngine;

    [Serializable]
    public class ExtEvent<T> : BaseExtEvent
    {
        private readonly object[] _arguments = new object[1];

        private Type[] _eventParamTypes;
        protected override Type[] EventParamTypes => _eventParamTypes ??= new Type[] { typeof(T) };

        /// <summary>
        /// The dynamic listeners list that you can add your listener to.
        /// </summary>
        [PublicAPI]
        public event Action<T> DynamicListeners;

        public void Invoke(T arg)
        {
            _arguments[0] = arg;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (int index = 0; index < _persistentListeners.Length; index++)
            {
                _persistentListeners[index].Invoke(_arguments);
            }
            
            DynamicListeners?.Invoke(arg);
        }
    }
}