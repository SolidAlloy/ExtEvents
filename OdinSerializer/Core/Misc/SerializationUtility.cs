//-----------------------------------------------------------------------
// <copyright file="SerializationUtility.cs" company="Sirenix IVS">
// Copyright (c) 2018 Sirenix IVS
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

namespace ExtEvents.OdinSerializer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Utilities;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Provides an array of utility wrapper methods for easy serialization and deserialization of objects of any type.
    /// </summary>
    public static class SerializationUtility
    {
        public static byte[] SerializeValue(object value, Type valueType, DataFormat format, out List<Object> unityObjects, SerializationContext context = null)
        {
            using var stream = CachedMemoryStream.Claim();
            var writer = GetCachedWriter(out IDisposable cache, format, stream.Value.MemoryStream, context);

            try
            {
                if (context != null)
                {
                    SerializeValue(value, valueType, writer, out unityObjects);
                }
                else
                {
                    using var con = Cache<SerializationContext>.Claim();
                    writer.Context = con;
                    SerializeValue(value, valueType, writer, out unityObjects);
                }
            }
            finally
            {
                cache.Dispose();
            }

            return stream.Value.MemoryStream.ToArray();
        }

        public static byte[] SerializeValue<T>(T value, DataFormat format, out List<Object> unityObjects, SerializationContext context = null)
        {
            using var stream = CachedMemoryStream.Claim();
            var writer = GetCachedWriter(out IDisposable cache, format, stream.Value.MemoryStream, context);

            try
            {
                if (context != null)
                {
                    SerializeValue(value, writer, out unityObjects);
                }
                else
                {
                    using var con = Cache<SerializationContext>.Claim();
                    writer.Context = con;
                    SerializeValue(value, writer, out unityObjects);
                }
            }
            finally
            {
                cache.Dispose();
            }

            return stream.Value.MemoryStream.ToArray();
        }

        public static byte[] SerializeValueWeak(object value, DataFormat format, out List<Object> unityObjects, SerializationContext context = null)
        {
            using var stream = CachedMemoryStream.Claim();
            var writer = GetCachedWriter(out IDisposable cache, format, stream.Value.MemoryStream, context);

            try
            {
                if (context != null)
                {
                    SerializeValueWeak(value, writer, out unityObjects);
                }
                else
                {
                    using var con = Cache<SerializationContext>.Claim();
                    writer.Context = con;
                    SerializeValueWeak(value, writer, out unityObjects);
                }
            }
            finally
            {
                cache.Dispose();
            }

            return stream.Value.MemoryStream.ToArray();
        }

        private static void SerializeValue<T>(T value, IDataWriter writer, out List<Object> unityObjects)
        {
            using var unityResolver = Cache<UnityReferenceResolver>.Claim();
            writer.Context.IndexReferenceResolver = unityResolver.Value;
            Serializer.Get<T>().WriteValue(value, writer);
            writer.FlushToStream();
            unityObjects = unityResolver.Value.GetReferencedUnityObjects();
        }

        private static void SerializeValueWeak(object value, IDataWriter writer, out List<Object> unityObjects)
        {
            using var unityResolver = Cache<UnityReferenceResolver>.Claim();
            writer.Context.IndexReferenceResolver = unityResolver.Value;
            var valueType = value.GetType();
            Serializer.Get(valueType).WriteValueWeak(value, writer);
            writer.FlushToStream();
            unityObjects = unityResolver.Value.GetReferencedUnityObjects();
        }

        public static void SerializeValue(object value, Type valueType, IDataWriter writer)
        {
            Serializer.Get(valueType).WriteValueWeak(value, writer);
            writer.FlushToStream();
        }

        private static void SerializeValue(object value, Type valueType, IDataWriter writer, out List<Object> unityObjects)
        {
            using var unityResolver = Cache<UnityReferenceResolver>.Claim();
            writer.Context.IndexReferenceResolver = unityResolver.Value;
            Serializer.Get(valueType).WriteValueWeak(value, writer);
            writer.FlushToStream();
            unityObjects = unityResolver.Value.GetReferencedUnityObjects();
        }

        public static object DeserializeValue(Type valueType, byte[] bytes, DataFormat format, List<Object> referencedUnityObjects, DeserializationContext context = null)
        {
            using var stream = CachedMemoryStream.Claim(bytes);
            var reader = GetCachedReader(out IDisposable cache, format, stream.Value.MemoryStream, context);

            try
            {
                if (context != null)
                {
                    return DeserializeValue(valueType, reader, referencedUnityObjects);
                }
                else
                {
                    using var con = Cache<DeserializationContext>.Claim();
                    reader.Context = con;
                    return DeserializeValue(valueType, reader, referencedUnityObjects);
                }
            }
            finally
            {
                cache.Dispose();
            }
        }

        public static T DeserializeValue<T>(byte[] bytes, DataFormat format, List<Object> referencedUnityObjects, DeserializationContext context = null)
        {
            using var stream = CachedMemoryStream.Claim(bytes);
            var reader = GetCachedReader(out IDisposable cache, format, stream.Value.MemoryStream, context);

            try
            {
                if (context != null)
                {
                    return DeserializeValue<T>(reader, referencedUnityObjects);
                }
                else
                {
                    using var con = Cache<DeserializationContext>.Claim();
                    reader.Context = con;
                    return DeserializeValue<T>(reader, referencedUnityObjects);
                }
            }
            finally
            {
                cache.Dispose();
            }
        }

        public static object DeserializeValue(Type valueType, IDataReader reader)
        {
            return Serializer.Get(valueType).ReadValueWeak(reader);
        }

        private static object DeserializeValue(Type valueType, IDataReader reader, List<Object> referencedUnityObjects)
        {
            using var unityResolver = Cache<UnityReferenceResolver>.Claim();
            unityResolver.Value.SetReferencedUnityObjects(referencedUnityObjects);
            reader.Context.IndexReferenceResolver = unityResolver.Value;
            return Serializer.Get(valueType).ReadValueWeak(reader);
        }

        private static T DeserializeValue<T>(IDataReader reader, List<Object> referencedUnityObjects)
        {
            using var unityResolver = Cache<UnityReferenceResolver>.Claim();
            unityResolver.Value.SetReferencedUnityObjects(referencedUnityObjects);
            reader.Context.IndexReferenceResolver = unityResolver.Value;
            return Serializer.Get<T>().ReadValue(reader);
        }

        private static IDataWriter GetCachedWriter(out IDisposable cache, DataFormat format, Stream stream, SerializationContext context)
        {
            IDataWriter writer;

            if (format == DataFormat.Binary)
            {
                var binaryCache = Cache<BinaryDataWriter>.Claim();
                var binaryWriter = binaryCache.Value;

                binaryWriter.Stream = stream;
                binaryWriter.Context = context;
                binaryWriter.PrepareNewSerializationSession();

                writer = binaryWriter;
                cache = binaryCache;
            }
            else if (format == DataFormat.Nodes)
            {
                throw new InvalidOperationException("Cannot automatically create a writer for the format '" + DataFormat.Nodes + "', because it does not use a stream.");
            }
            else
            {
                throw new NotImplementedException(format.ToString());
            }

            return writer;
        }

        private static IDataReader GetCachedReader(out IDisposable cache, DataFormat format, Stream stream, DeserializationContext context)
        {
            IDataReader reader;

            if (format == DataFormat.Binary)
            {
                var binaryCache = Cache<BinaryDataReader>.Claim();
                var binaryReader = binaryCache.Value;

                binaryReader.Stream = stream;
                binaryReader.Context = context;
                binaryReader.PrepareNewSerializationSession();

                reader = binaryReader;
                cache = binaryCache;
            }
            else if (format == DataFormat.Nodes)
            {
                throw new InvalidOperationException("Cannot automatically create a reader for the format '" + DataFormat.Nodes + "', because it does not use a stream.");
            }
            else
            {
                throw new NotImplementedException(format.ToString());
            }

            return reader;
        }
    }
}