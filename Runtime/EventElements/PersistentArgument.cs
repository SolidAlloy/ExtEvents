namespace ExtEvents
{
    using System;
    using JetBrains.Annotations;
    using OdinSerializer;
    using TypeReferences;
    using UnityEngine;
    using UnityEngine.Assertions;

    /// <summary>
    /// An argument that can be dynamic or serialized, and is configured through editor UI as a part of <see cref="ExtEvent"/>.
    /// </summary>
    [Serializable]
    public class PersistentArgument : ISerializationCallbackReceiver
    {
        [SerializeField] internal int _index;

        /// <summary> An index of the argument passed through ExtEvent.Invoke(). </summary>
        [PublicAPI]
        public int Index => _index;

        [SerializeField] internal bool _isSerialized;

        /// <summary> Whether the argument is serialized or dynamic. </summary>
        [PublicAPI] public bool IsSerialized => _isSerialized;

        [SerializeField] internal TypeReference _type;

        /// <summary> The type of the argument. </summary>
        [PublicAPI] public Type Type => _type;

        [SerializeField] private SimpleSerializationData _serializationData;
        [SerializeField] internal bool _canBeDynamic;

        // old code support
        [SerializeField] private string _serializedArg;

        private ArgumentHolder _argumentHolder;

        internal unsafe void* SerializedValuePointer
        {
            get
            {
                try
                {
                    EnsureArgumentHolderInitialized();
                    return _argumentHolder.ValuePointer;
                }
#pragma warning disable CS0618
                catch (ExecutionEngineException)
#pragma warning restore CS0618
                {
                    Debug.LogWarning($"Tried to invoke a method with a serialized argument of type {_type} but there was no code generated for it ahead of time.");
                    return default;
                }
            }
        }

        /// <summary> The value of the argument if it is serialized. </summary>
        /// <exception cref="Exception">The argument is not serialized but a dynamic one.</exception>
        [PublicAPI]
        public object SerializedValue
        {
            get
            {
                if (!_isSerialized)
                    throw new Exception("Tried to access a persistent value of an argument but the argument is dynamic");

                if (_type.Type == null)
                    return null;

                try
                {
                    EnsureArgumentHolderInitialized();
                    return _argumentHolder.Value;
                }
#pragma warning disable CS0618
                catch (ExecutionEngineException)
#pragma warning restore CS0618
                {
                    Debug.LogWarning($"Tried to invoke a method with a serialized argument of type {_type} but there was no code generated for it ahead of time.");
                    return null;
                }
            }
            internal set
            {
                EnsureArgumentHolderInitialized();
                _argumentHolder.Value = value;
            }
        }

        /// <summary> Creates a serialized argument. </summary>
        /// <param name="value">The initial value of the serialized argument.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <returns>An instance of the serialized argument.</returns>
        public static PersistentArgument CreateSerialized<T>(T value)
        {
            return CreateSerialized(value, typeof(T));
        }

        /// <summary> Creates a serialized argument. </summary>
        /// <param name="value">The initial value of the serialized argument.</param>
        /// <param name="argumentType">The type of the value.</param>
        /// <returns>An instance of the serialized argument.</returns>
        public static PersistentArgument CreateSerialized(object value, Type argumentType)
        {
            return new PersistentArgument(argumentType, value);
        }

        /// <summary> Creates a dynamic argument. </summary>
        /// <param name="eventArgumentIndex">An index of the argument passed through ExtEvent.Invoke().</param>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <returns>An instance of the dynamic argument.</returns>
        public static PersistentArgument CreateDynamic<T>(int eventArgumentIndex)
        {
            return CreateDynamic(eventArgumentIndex, typeof(T));
        }

        /// <summary> Creates a dynamic argument. </summary>
        /// <param name="eventArgumentIndex">An index of the argument passed through ExtEvent.Invoke().</param>
        /// <param name="argumentType">The type of the argument.</param>
        /// <returns>An instance of the dynamic argument.</returns>
        public static PersistentArgument CreateDynamic(int eventArgumentIndex, Type argumentType)
        {
            return new PersistentArgument(argumentType, eventArgumentIndex);
        }

        private PersistentArgument(Type argumentType, object value)
        {
            _isSerialized = true;
            _type = argumentType;
            _argumentHolder = CreateArgumentHolder(argumentType, value);
        }

        private PersistentArgument(Type argumentType, int index)
        {
            _isSerialized = false;
            _type = argumentType;
            _index = index;
            _canBeDynamic = true;
        }

        public void OnBeforeSerialize()
        {
            // also, should we check if _isSerialized?
#if UNITY_EDITOR
            CustomSerialization.SerializeValue(_argumentHolder?.Value, _type, ref _serializationData);
#endif
        }

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            // old code support
            if (DeserializeOldArgumentHolder())
                return;

            _argumentHolder ??= CreateArgumentHolder(_type);
            _argumentHolder.Value = CustomSerialization.DeserializeValue(_type, _serializationData);
#endif
        }

        private void EnsureArgumentHolderInitialized()
        {
#if UNITY_EDITOR
            // old code support
            if (DeserializeOldArgumentHolder())
                return;

            Assert.IsNotNull(_argumentHolder);
#else
            if (_argumentHolder == null)
            {
                _argumentHolder = CreateArgumentHolder(_type);
                _argumentHolder.Value = CustomSerialization.DeserializeValue(_type, _serializationData);
            }
#endif
        }

        private ArgumentHolder CreateArgumentHolder(Type valueType, object value = null)
        {
            var holderType = typeof(ArgumentHolder<>).MakeGenericType(valueType);
            return (ArgumentHolder) (value == null ? Activator.CreateInstance(holderType) : Activator.CreateInstance(holderType, value));
        }

        private bool DeserializeOldArgumentHolder()
        {
            if (_argumentHolder != null || string.IsNullOrEmpty(_serializedArg))
                return false;

            var type = typeof(ArgumentHolder<>).MakeGenericType(_type);
            _argumentHolder = (ArgumentHolder) JsonUtility.FromJson(_serializedArg, type);
            _serializedArg = null;
            return true;
        }
    }
}