namespace ExtEvents.OdinSerializer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Utilities;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public static class CustomSerialization
    {
        #region Serialization

#if UNITY_EDITOR
        private static readonly Type _sbpContentPipelineType = TwoWaySerializationBinder.Default.BindToType("UnityEditor.Build.Pipeline.ContentPipeline");

        private static bool? _isBuildingPlayer;
        private static bool IsBuildingPlayer
        {
            get
            {
                if (_isBuildingPlayer == null)
                    InitializeStaticVariables();

                // ReSharper disable once PossibleInvalidOperationException
                return _isBuildingPlayer.Value;
            }
        }

        private static bool? _isRecordingPropertyModifications;
        private static bool IsRecordingPropertyModifications
        {
            get
            {
                if (_isRecordingPropertyModifications == null)
                    InitializeStaticVariables();

                // ReSharper disable once PossibleInvalidOperationException
                return _isRecordingPropertyModifications.Value;
            }
        }

        private static void InitializeStaticVariables()
        {
            var stackFrames = new StackTrace().GetFrames();
            Type buildPipelineType = typeof(BuildPipeline);
            Type prefabUtilityType = typeof(PrefabUtility);

            _isBuildingPlayer = false;
            _isRecordingPropertyModifications = false;

            if (stackFrames == null)
                return;

            // Look through a stack trace to determine some things about the current serialization context.
            // For example, we check if we are currently building a player, or if we are currently recording prefab instance property modifications.
            // This is pretty hacky, but as far as we can tell it's the only way to do it.
            foreach (StackFrame frame in stackFrames)
            {
                var method = frame.GetMethod();

                if (method.DeclaringType == buildPipelineType || method.DeclaringType == _sbpContentPipelineType)
                {
                    _isBuildingPlayer = true;
                    return;
                }

                if (method.DeclaringType == prefabUtilityType && method.Name == "RecordPrefabInstancePropertyModifications")
                {
                    _isRecordingPropertyModifications = true;
                    return;
                }
            }
        }
#endif

        public static void SerializeValue(object value, Type valueType, ref SerializationData data)
        {
#if UNITY_EDITOR
            // Do nothing whatsoever and return immediately, lest we break Unity's "smart" modification recording
            if (IsRecordingPropertyModifications)
                return;

            // Ensure there is no superfluous data left over after serialization
            // (We will reassign all necessary data.)
            data.Reset();

            if (IsBuildingPlayer)
            {
                SerializeValueToBinary(value, valueType, ref data);
            }
            else
            {
                SerializeValueToNodes(value, valueType, ref data);
            }
#else
            SerializeValueToBinary(value, valueType, ref data);
#endif
        }

        public static void SerializeValueToBinary<T>(T value, out byte[] bytes, out List<UnityEngine.Object> referencedUnityObjects)
        {
            bytes = SerializationUtility.SerializeValue(value, DataFormat.Binary, out referencedUnityObjects);
        }

        public static void SerializeValueToBinaryWeak(object value, out byte[] bytes, out List<UnityEngine.Object> referencedUnityObjects)
        {
            bytes = SerializationUtility.SerializeValueWeak(value, DataFormat.Binary, out referencedUnityObjects);
        }

        private static void SerializeValueToBinary(object value, Type valueType, ref SerializationData data)
        {
            data.Bytes = value == null ? null : SerializationUtility.SerializeValue(value, valueType, DataFormat.Binary, out data.ReferencedUnityObjects);
            data.DataFormat = DataFormat.Binary;
        }

        private static void SerializeValueToNodes(object value, Type valueType, ref SerializationData data)
        {
            using var newContext = Cache<SerializationContext>.Claim();
            using var writer = new SerializationNodeDataWriter(newContext);
            using var resolver = Cache<UnityReferenceResolver>.Claim();

            if (data.SerializationNodes != null)
            {
                // Reuse pre-expanded list to keep GC down
                data.SerializationNodes.Clear();
                writer.Nodes = data.SerializationNodes;
            }

            resolver.Value.SetReferencedUnityObjects(data.ReferencedUnityObjects);

            newContext.Value.Config.SerializationPolicy = SerializationPolicies.Unity;
            newContext.Value.IndexReferenceResolver = resolver.Value;

            writer.Context = newContext;

            if (value == null)
            {
                data.SerializationNodes = null;
                data.ReferencedUnityObjects = null;
            }
            else
            {
                SerializationUtility.SerializeValue(value, valueType, writer);
                data.SerializationNodes = writer.Nodes;
                data.ReferencedUnityObjects = resolver.Value.GetReferencedUnityObjects();
            }

            data.DataFormat = DataFormat.Nodes;
        }

        #endregion

        #region Deserialization

        public static object DeserializeValue(Type valueType, SerializationData data)
        {
            if (data.BytesAreFilled && !data.SerializationNodesAreFilled)
            {
                return SerializationUtility.DeserializeValue(valueType, data.Bytes, data.DataFormat, data.ReferencedUnityObjects);
            }

            Cache<DeserializationContext> cachedContext = null;

            try
            {
                var context = GetCachedContext(out cachedContext);

                if (data.DataFormat == DataFormat.Nodes)
                {
                    return DeserializeValueFromNodes(valueType, data, context);
                }
                else if (data.BytesAreFilled)
                {
                    return SerializationUtility.DeserializeValue(valueType, data.Bytes, data.DataFormat, data.ReferencedUnityObjects, context);
                }
                else
                {
                    return default;
                }
            }
            finally
            {
                if (cachedContext != null)
                {
                    Cache<DeserializationContext>.Release(cachedContext);
                }
            }
        }

        public static T DeserializeValue<T>(byte[] bytes, List<UnityEngine.Object> referencedUnityObjects)
        {
            return SerializationUtility.DeserializeValue<T>(bytes, DataFormat.Binary, referencedUnityObjects);
        }

        public static object DeserializeValue(Type type, byte[] bytes, List<UnityEngine.Object> referencedUnityObjects)
        {
            return SerializationUtility.DeserializeValue(type, bytes, DataFormat.Binary, referencedUnityObjects);
        }

        private static DeserializationContext GetCachedContext(out Cache<DeserializationContext> cachedContext)
        {
            cachedContext = Cache<DeserializationContext>.Claim();
            DeserializationContext context = cachedContext;

            context.Config.SerializationPolicy = SerializationPolicies.Unity;
            context.Config.DebugContext.ErrorHandlingPolicy = ErrorHandlingPolicy.Resilient;
            context.Config.DebugContext.LoggingPolicy = LoggingPolicy.LogErrors;
            context.Config.DebugContext.Logger = DefaultLoggers.UnityLogger;
            return context;
        }

        private static object DeserializeValueFromNodes(Type valueType, SerializationData data, DeserializationContext context)
        {
            using var reader = new SerializationNodeDataReader(context);
            using var resolver = Cache<UnityReferenceResolver>.Claim();

            resolver.Value.SetReferencedUnityObjects(data.ReferencedUnityObjects);
            context.IndexReferenceResolver = resolver.Value;
            reader.Nodes = data.SerializationNodes;
            return SerializationUtility.DeserializeValue(valueType, reader);
        }

        #endregion
    }
}