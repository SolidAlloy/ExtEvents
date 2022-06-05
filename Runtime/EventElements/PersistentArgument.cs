namespace ExtEvents
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;
    using OdinSerializer;
    using TypeReferences;
    using UnityEngine;
    using UnityEngine.Assertions;
    using UnityEngine.Serialization;

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

        [FormerlySerializedAs("_type")] [SerializeField] internal TypeReference _targetType;
        [SerializeField] internal TypeReference _fromType;

        /// <summary>
        /// The type of the argument that is passed to the listener by ExtEvent.
        /// This is usually the same as <see cref="Type"/> except for cases when types are implicitly converted.
        /// For example, int may be passed from ExtEvent, but the listener will invoke a method that accepts float.
        /// In this case, <see cref="Type"/> will be float, but <see cref="OriginalType"/> will be int.
        /// </summary>
        [PublicAPI] public Type OriginalType => _fromType;

        /// <summary> The type of the argument. </summary>
        [PublicAPI] public Type Type => _targetType;

        [SerializeField] private SerializationData _serializationData;
        [SerializeField] internal bool _canBeDynamic;

        // old code support
        [SerializeField] private string _serializedArg;

        private ArgumentHolder _argumentHolder;
        private Converter _converter;

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
                    Debug.LogWarning($"Tried to invoke a method with a serialized argument of type {_targetType} but there was no code generated for it ahead of time.");
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

                if (_targetType.Type == null)
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
                    Debug.LogWarning($"Tried to invoke a method with a serialized argument of type {_targetType} but there was no code generated for it ahead of time.");
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
            _targetType = argumentType;
            _argumentHolder = CreateArgumentHolder(argumentType, value);
        }

        private PersistentArgument(Type argumentType, int index)
        {
            _isSerialized = false;
            _targetType = argumentType;
            _index = index;
            _canBeDynamic = true;
        }

        internal void InitDynamic()
        {
            // Check if fromType is null because this field didn't exist in previous versions of ExtEvents,
            // so we don't have to create a converter because conversions were not supported previously.
            if (_fromType?.Type != null && _fromType != _targetType)
                _converter = Converter.GetForTypes(_fromType, _targetType);
        }

        internal unsafe void* ProcessDynamicArgument(void* sourceTypePointer)
        {
            return _converter == null ? sourceTypePointer : _converter.Convert(sourceTypePointer);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // also, should we check if _isSerialized?
#if UNITY_EDITOR
            CustomSerialization.SerializeValue(_argumentHolder?.Value, _targetType, ref _serializationData);
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if UNITY_EDITOR
            // old code support
            if (DeserializeOldArgumentHolder())
                return;

            if (_argumentHolder == null || _argumentHolder.ValueType != _targetType.Type)
                _argumentHolder = CreateArgumentHolder(_targetType);

            _argumentHolder.Value = CustomSerialization.DeserializeValue(_targetType, _serializationData);
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
                _argumentHolder = CreateArgumentHolder(_targetType);
                _argumentHolder.Value = CustomSerialization.DeserializeValue(_targetType, _serializationData);
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

            var type = typeof(ArgumentHolder<>).MakeGenericType(_targetType);
            _argumentHolder = (ArgumentHolder) JsonUtility.FromJson(_serializedArg, type);
            _serializedArg = null;
            return true;
        }
    }
}