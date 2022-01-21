namespace ExtEvents
{
    using System;
    using JetBrains.Annotations;

    [Serializable]
    public class ExtEvent : BaseExtEvent
    {
        protected override Type[] EventParamTypes => Type.EmptyTypes;
        
        /// <summary>
        /// The dynamic listeners list that you can add your listener to.
        /// </summary>
        [PublicAPI]
        public event Action DynamicListeners;

        public void Invoke()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int index = 0; index < _persistentListeners.Length; index++)
            {
                _persistentListeners[index].Invoke(null);
            }
            
            DynamicListeners?.Invoke();
        }
    }
}