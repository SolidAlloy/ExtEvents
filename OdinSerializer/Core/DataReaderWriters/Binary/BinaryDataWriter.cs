//-----------------------------------------------------------------------
// <copyright file="BinaryDataWriter.cs" company="Sirenix IVS">
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
    using System.Runtime.CompilerServices;
    using Utilities;
    using Utilities.Unsafe;

    /// <summary>
    /// Writes data to a stream that can be read by a <see cref="BinaryDataReader"/>.
    /// </summary>
    /// <seealso cref="BaseDataWriter" />
    public unsafe class BinaryDataWriter : BaseDataWriter
    {
        // For byte caching while writing values up to sizeof(decimal) using the old ProperBitConverter method
        // (still occasionally used) and to provide a permanent buffer to read into.
        private readonly byte[] small_buffer = new byte[16];
        private readonly byte[] buffer = new byte[1024 * 100]; // 100 Kb buffer should be enough for most things, and enough to prevent flushing to stream too often
        private int bufferIndex;

        // A dictionary over all seen types, so short type ids can be written after a type's full name has already been written to the stream once
        private readonly Dictionary<Type, int> types = new Dictionary<Type, int>(16, FastTypeComparer.Instance);

        public bool CompressStringsTo8BitWhenPossible = false;

        public BinaryDataWriter() : base(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryDataWriter" /> class.
        /// </summary>
        /// <param name="stream">The base stream of the writer.</param>
        /// <param name="context">The serialization context to use.</param>
        public BinaryDataWriter(Stream stream, SerializationContext context) : base(stream, context)
        {
        }

        /// <summary>
        /// Begins an array node of the given length.
        /// </summary>
        /// <param name="length">The length of the array to come.</param>
        public override void BeginArrayNode(long length)
        {
            EnsureBufferSpace(9);
            buffer[bufferIndex++] = (byte)BinaryEntryType.StartOfArray;
            UNSAFE_WriteToBuffer_8_Int64(length);
            PushArray();
        }

        /// <summary>
        /// Writes the beginning of a reference node.
        /// <para />
        /// This call MUST eventually be followed by a corresponding call to <see cref="IDataWriter.EndNode(string)" />, with the same name.
        /// </summary>
        /// <param name="name">The name of the reference node.</param>
        /// <param name="type">The type of the reference node. If null, no type metadata will be written.</param>
        /// <param name="id">The id of the reference node. This id is acquired by calling <see cref="SerializationContext.TryRegisterInternalReference(object, out int)" />.</param>
        public override void BeginReferenceNode(string name, Type type, int id)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedStartOfReferenceNode;
                WriteStringFast(name);
            }
            else
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedStartOfReferenceNode;
            }

            WriteType(type);
            EnsureBufferSpace(4);
            UNSAFE_WriteToBuffer_4_Int32(id);
            PushNode(name, id, type);
        }

        /// <summary>
        /// Begins a struct/value type node. This is essentially the same as a reference node, except it has no internal reference id.
        /// <para />
        /// This call MUST eventually be followed by a corresponding call to <see cref="IDataWriter.EndNode(string)" />, with the same name.
        /// </summary>
        /// <param name="name">The name of the struct node.</param>
        /// <param name="type">The type of the struct node. If null, no type metadata will be written.</param>
        public override void BeginStructNode(string name, Type type)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedStartOfStructNode;
                WriteStringFast(name);
            }
            else
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedStartOfStructNode;
            }

            WriteType(type);
            PushNode(name, -1, type);
        }

        /// <summary>
        /// Disposes all resources kept by the data writer, except the stream, which can be reused later.
        /// </summary>
        public override void Dispose()
        {
            //this.Stream.Dispose();
            FlushToStream();
        }

        /// <summary>
        /// Ends the current array node, if the current node is an array node.
        /// </summary>
        public override void EndArrayNode()
        {
            PopArray();

            EnsureBufferSpace(1);
            buffer[bufferIndex++] = (byte)BinaryEntryType.EndOfArray;
        }

        /// <summary>
        /// Ends the current node with the given name. If the current node has another name, an <see cref="InvalidOperationException" /> is thrown.
        /// </summary>
        /// <param name="name">The name of the node to end. This has to be the name of the current node.</param>
        public override void EndNode(string name)
        {
            PopNode();

            EnsureBufferSpace(1);
            buffer[bufferIndex++] = (byte)BinaryEntryType.EndOfNode;
        }

        private static readonly Dictionary<Type, Action<BinaryDataWriter, object>> PrimitiveArrayWriters = new Dictionary<Type, Action<BinaryDataWriter, object>>(FastTypeComparer.Instance)
        {
            { typeof(char),    WritePrimitiveArray_char     },
            { typeof(sbyte),   WritePrimitiveArray_sbyte    },
            { typeof(short),   WritePrimitiveArray_short    },
            { typeof(int),     WritePrimitiveArray_int      },
            { typeof(long),    WritePrimitiveArray_long     },
            { typeof(byte),    WritePrimitiveArray_byte     },
            { typeof(ushort),  WritePrimitiveArray_ushort   },
            { typeof(uint),    WritePrimitiveArray_uint     },
            { typeof(ulong),   WritePrimitiveArray_ulong    },
            { typeof(decimal), WritePrimitiveArray_decimal  },
            { typeof(bool),    WritePrimitiveArray_bool     },
            { typeof(float),   WritePrimitiveArray_float    },
            { typeof(double),  WritePrimitiveArray_double   },
            { typeof(Guid),    WritePrimitiveArray_Guid     },
        };

        private static void WritePrimitiveArray_byte(BinaryDataWriter writer, object o)
        {
            byte[] array = o as byte[];

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(1);

            // We can include a special case for byte arrays, as there's no need to copy that to a buffer
            // First we ensure that the stream is up to date with the buffer, then we write directly to
            // the stream.
            writer.FlushToStream();
            writer.Stream.Write(array, 0, array.Length);
        }

        private static void WritePrimitiveArray_sbyte(BinaryDataWriter writer, object o)
        {
            sbyte[] array = o as sbyte[];
            const int bytesPerElement = sizeof(sbyte);
            int byteCount = array.Length * bytesPerElement;

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(bytesPerElement);

            // We copy to a buffer in order to write the entire array into the stream with one call
            // We use our internal buffer if there's space in it, otherwise we claim a buffer from a cache.
            if (writer.TryEnsureBufferSpace(byteCount))
            {
                fixed (byte* toBase = writer.buffer)
                fixed (void* from = array)
                {
                    void* to = toBase + writer.bufferIndex;

                    UnsafeUtilities.MemoryCopy(from, to, byteCount);
                }

                writer.bufferIndex += byteCount;
            }
            else
            {
                // Ensure stream is up to date with the buffer before we write directly to it
                writer.FlushToStream();

                using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                {
                    // No need to check endianness, since sbyte has a size of 1
                    UnsafeUtilities.MemoryCopy(array, tempBuffer.Array, byteCount, 0, 0);
                    writer.Stream.Write(tempBuffer.Array, 0, byteCount);
                }
            }
        }

        private static void WritePrimitiveArray_bool(BinaryDataWriter writer, object o)
        {
            bool[] array = o as bool[];
            const int bytesPerElement = sizeof(bool);
            int byteCount = array.Length * bytesPerElement;

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(bytesPerElement);

            // We copy to a buffer in order to write the entire array into the stream with one call
            // We use our internal buffer if there's space in it, otherwise we claim a buffer from a cache.
            if (writer.TryEnsureBufferSpace(byteCount))
            {
                fixed (byte* toBase = writer.buffer)
                fixed (void* from = array)
                {
                    void* to = toBase + writer.bufferIndex;

                    UnsafeUtilities.MemoryCopy(from, to, byteCount);
                }

                writer.bufferIndex += byteCount;
            }
            else
            {
                // Ensure stream is up to date with the buffer before we write directly to it
                writer.FlushToStream();

                using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                {
                    // No need to check endianness, since bool has a size of 1
                    UnsafeUtilities.MemoryCopy(array, tempBuffer.Array, byteCount, 0, 0);
                    writer.Stream.Write(tempBuffer.Array, 0, byteCount);
                }
            }
        }

        private static void WritePrimitiveArray_char(BinaryDataWriter writer, object o)
        {
            char[] array = o as char[];
            int bytesPerElement = sizeof(char);
            int byteCount = array.Length * bytesPerElement;

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(bytesPerElement);

            // We copy to a buffer in order to write the entire array into the stream with one call
            // We use our internal buffer if there's space in it, otherwise we claim a buffer from a cache.
            if (writer.TryEnsureBufferSpace(byteCount))
            {
                if (BitConverter.IsLittleEndian)
                {
                    fixed (byte* toBase = writer.buffer)
                    fixed (void* from = array)
                    {
                        void* to = toBase + writer.bufferIndex;

                        UnsafeUtilities.MemoryCopy(from, to, byteCount);
                    }

                    writer.bufferIndex += byteCount;
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.UNSAFE_WriteToBuffer_2_Char(array[i]);
                    }
                }
            }
            else
            {
                // Ensure stream is up to date with the buffer before we write directly to it
                writer.FlushToStream();

                using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        // We always store in little endian, so we can do a direct memory mapping, which is a lot faster
                        UnsafeUtilities.MemoryCopy(array, tempBuffer.Array, byteCount, 0, 0);
                    }
                    else
                    {
                        // We have to convert each individual element to bytes, since the byte order has to be reversed
                        var b = tempBuffer.Array;

                        for (int i = 0; i < array.Length; i++)
                        {
                            ProperBitConverter.GetBytes(b, i * bytesPerElement, array[i]);
                        }
                    }

                    writer.Stream.Write(tempBuffer.Array, 0, byteCount);
                }
            }
        }

        private static void WritePrimitiveArray_short(BinaryDataWriter writer, object o)
        {
            short[] array = o as short[];
            int bytesPerElement = sizeof(short);
            int byteCount = array.Length * bytesPerElement;

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(bytesPerElement);

            // We copy to a buffer in order to write the entire array into the stream with one call
            // We use our internal buffer if there's space in it, otherwise we claim a buffer from a cache.
            if (writer.TryEnsureBufferSpace(byteCount))
            {
                if (BitConverter.IsLittleEndian)
                {
                    fixed (byte* toBase = writer.buffer)
                    fixed (void* from = array)
                    {
                        void* to = toBase + writer.bufferIndex;

                        UnsafeUtilities.MemoryCopy(from, to, byteCount);
                    }

                    writer.bufferIndex += byteCount;
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.UNSAFE_WriteToBuffer_2_Int16(array[i]);
                    }
                }
            }
            else
            {
                // Ensure stream is up to date with the buffer before we write directly to it
                writer.FlushToStream();

                using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        // We always store in little endian, so we can do a direct memory mapping, which is a lot faster
                        UnsafeUtilities.MemoryCopy(array, tempBuffer.Array, byteCount, 0, 0);
                    }
                    else
                    {
                        // We have to convert each individual element to bytes, since the byte order has to be reversed
                        var b = tempBuffer.Array;

                        for (int i = 0; i < array.Length; i++)
                        {
                            ProperBitConverter.GetBytes(b, i * bytesPerElement, array[i]);
                        }
                    }

                    writer.Stream.Write(tempBuffer.Array, 0, byteCount);
                }
            }
        }

        private static void WritePrimitiveArray_int(BinaryDataWriter writer, object o)
        {
            int[] array = o as int[];
            int bytesPerElement = sizeof(int);
            int byteCount = array.Length * bytesPerElement;

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(bytesPerElement);

            // We copy to a buffer in order to write the entire array into the stream with one call
            // We use our internal buffer if there's space in it, otherwise we claim a buffer from a cache.
            if (writer.TryEnsureBufferSpace(byteCount))
            {
                if (BitConverter.IsLittleEndian)
                {
                    fixed (byte* toBase = writer.buffer)
                    fixed (void* from = array)
                    {
                        void* to = toBase + writer.bufferIndex;

                        UnsafeUtilities.MemoryCopy(from, to, byteCount);
                    }

                    writer.bufferIndex += byteCount;
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.UNSAFE_WriteToBuffer_4_Int32(array[i]);
                    }
                }
            }
            else
            {
                // Ensure stream is up to date with the buffer before we write directly to it
                writer.FlushToStream();

                using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        // We always store in little endian, so we can do a direct memory mapping, which is a lot faster
                        UnsafeUtilities.MemoryCopy(array, tempBuffer.Array, byteCount, 0, 0);
                    }
                    else
                    {
                        // We have to convert each individual element to bytes, since the byte order has to be reversed
                        var b = tempBuffer.Array;

                        for (int i = 0; i < array.Length; i++)
                        {
                            ProperBitConverter.GetBytes(b, i * bytesPerElement, array[i]);
                        }
                    }

                    writer.Stream.Write(tempBuffer.Array, 0, byteCount);
                }
            }
        }

        private static void WritePrimitiveArray_long(BinaryDataWriter writer, object o)
        {
            long[] array = o as long[];
            int bytesPerElement = sizeof(long);
            int byteCount = array.Length * bytesPerElement;

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(bytesPerElement);

            // We copy to a buffer in order to write the entire array into the stream with one call
            // We use our internal buffer if there's space in it, otherwise we claim a buffer from a cache.
            if (writer.TryEnsureBufferSpace(byteCount))
            {
                if (BitConverter.IsLittleEndian)
                {
                    fixed (byte* toBase = writer.buffer)
                    fixed (void* from = array)
                    {
                        void* to = toBase + writer.bufferIndex;

                        UnsafeUtilities.MemoryCopy(from, to, byteCount);
                    }

                    writer.bufferIndex += byteCount;
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.UNSAFE_WriteToBuffer_8_Int64(array[i]);
                    }
                }
            }
            else
            {
                // Ensure stream is up to date with the buffer before we write directly to it
                writer.FlushToStream();

                using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        // We always store in little endian, so we can do a direct memory mapping, which is a lot faster
                        UnsafeUtilities.MemoryCopy(array, tempBuffer.Array, byteCount, 0, 0);
                    }
                    else
                    {
                        // We have to convert each individual element to bytes, since the byte order has to be reversed
                        var b = tempBuffer.Array;

                        for (int i = 0; i < array.Length; i++)
                        {
                            ProperBitConverter.GetBytes(b, i * bytesPerElement, array[i]);
                        }
                    }

                    writer.Stream.Write(tempBuffer.Array, 0, byteCount);
                }
            }
        }

        private static void WritePrimitiveArray_ushort(BinaryDataWriter writer, object o)
        {
            ushort[] array = o as ushort[];
            int bytesPerElement = sizeof(ushort);
            int byteCount = array.Length * bytesPerElement;

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(bytesPerElement);

            // We copy to a buffer in order to write the entire array into the stream with one call
            // We use our internal buffer if there's space in it, otherwise we claim a buffer from a cache.
            if (writer.TryEnsureBufferSpace(byteCount))
            {
                if (BitConverter.IsLittleEndian)
                {
                    fixed (byte* toBase = writer.buffer)
                    fixed (void* from = array)
                    {
                        void* to = toBase + writer.bufferIndex;

                        UnsafeUtilities.MemoryCopy(from, to, byteCount);
                    }

                    writer.bufferIndex += byteCount;
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.UNSAFE_WriteToBuffer_2_UInt16(array[i]);
                    }
                }
            }
            else
            {
                // Ensure stream is up to date with the buffer before we write directly to it
                writer.FlushToStream();

                using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        // We always store in little endian, so we can do a direct memory mapping, which is a lot faster
                        UnsafeUtilities.MemoryCopy(array, tempBuffer.Array, byteCount, 0, 0);
                    }
                    else
                    {
                        // We have to convert each individual element to bytes, since the byte order has to be reversed
                        var b = tempBuffer.Array;

                        for (int i = 0; i < array.Length; i++)
                        {
                            ProperBitConverter.GetBytes(b, i * bytesPerElement, array[i]);
                        }
                    }

                    writer.Stream.Write(tempBuffer.Array, 0, byteCount);
                }
            }
        }

        private static void WritePrimitiveArray_uint(BinaryDataWriter writer, object o)
        {
            uint[] array = o as uint[];
            int bytesPerElement = sizeof(uint);
            int byteCount = array.Length * bytesPerElement;

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(bytesPerElement);

            // We copy to a buffer in order to write the entire array into the stream with one call
            // We use our internal buffer if there's space in it, otherwise we claim a buffer from a cache.
            if (writer.TryEnsureBufferSpace(byteCount))
            {
                if (BitConverter.IsLittleEndian)
                {
                    fixed (byte* toBase = writer.buffer)
                    fixed (void* from = array)
                    {
                        void* to = toBase + writer.bufferIndex;

                        UnsafeUtilities.MemoryCopy(from, to, byteCount);
                    }

                    writer.bufferIndex += byteCount;
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.UNSAFE_WriteToBuffer_4_UInt32(array[i]);
                    }
                }
            }
            else
            {
                // Ensure stream is up to date with the buffer before we write directly to it
                writer.FlushToStream();

                using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        // We always store in little endian, so we can do a direct memory mapping, which is a lot faster
                        UnsafeUtilities.MemoryCopy(array, tempBuffer.Array, byteCount, 0, 0);
                    }
                    else
                    {
                        // We have to convert each individual element to bytes, since the byte order has to be reversed
                        var b = tempBuffer.Array;

                        for (int i = 0; i < array.Length; i++)
                        {
                            ProperBitConverter.GetBytes(b, i * bytesPerElement, array[i]);
                        }
                    }

                    writer.Stream.Write(tempBuffer.Array, 0, byteCount);
                }
            }
        }

        private static void WritePrimitiveArray_ulong(BinaryDataWriter writer, object o)
        {
            ulong[] array = o as ulong[];
            int bytesPerElement = sizeof(ulong);
            int byteCount = array.Length * bytesPerElement;

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(bytesPerElement);

            // We copy to a buffer in order to write the entire array into the stream with one call
            // We use our internal buffer if there's space in it, otherwise we claim a buffer from a cache.
            if (writer.TryEnsureBufferSpace(byteCount))
            {
                if (BitConverter.IsLittleEndian)
                {
                    fixed (byte* toBase = writer.buffer)
                    fixed (void* from = array)
                    {
                        void* to = toBase + writer.bufferIndex;

                        UnsafeUtilities.MemoryCopy(from, to, byteCount);
                    }

                    writer.bufferIndex += byteCount;
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.UNSAFE_WriteToBuffer_8_UInt64(array[i]);
                    }
                }
            }
            else
            {
                // Ensure stream is up to date with the buffer before we write directly to it
                writer.FlushToStream();

                using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        // We always store in little endian, so we can do a direct memory mapping, which is a lot faster
                        UnsafeUtilities.MemoryCopy(array, tempBuffer.Array, byteCount, 0, 0);
                    }
                    else
                    {
                        // We have to convert each individual element to bytes, since the byte order has to be reversed
                        var b = tempBuffer.Array;

                        for (int i = 0; i < array.Length; i++)
                        {
                            ProperBitConverter.GetBytes(b, i * bytesPerElement, array[i]);
                        }
                    }

                    writer.Stream.Write(tempBuffer.Array, 0, byteCount);
                }
            }
        }

        private static void WritePrimitiveArray_decimal(BinaryDataWriter writer, object o)
        {
            decimal[] array = o as decimal[];
            int bytesPerElement = sizeof(decimal);
            int byteCount = array.Length * bytesPerElement;

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(bytesPerElement);

            // We copy to a buffer in order to write the entire array into the stream with one call
            // We use our internal buffer if there's space in it, otherwise we claim a buffer from a cache.
            if (writer.TryEnsureBufferSpace(byteCount))
            {
                if (BitConverter.IsLittleEndian)
                {
                    fixed (byte* toBase = writer.buffer)
                    fixed (void* from = array)
                    {
                        void* to = toBase + writer.bufferIndex;

                        UnsafeUtilities.MemoryCopy(from, to, byteCount);
                    }

                    writer.bufferIndex += byteCount;
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.UNSAFE_WriteToBuffer_16_Decimal(array[i]);
                    }
                }
            }
            else
            {
                // Ensure stream is up to date with the buffer before we write directly to it
                writer.FlushToStream();

                using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        // We always store in little endian, so we can do a direct memory mapping, which is a lot faster
                        UnsafeUtilities.MemoryCopy(array, tempBuffer.Array, byteCount, 0, 0);
                    }
                    else
                    {
                        // We have to convert each individual element to bytes, since the byte order has to be reversed
                        var b = tempBuffer.Array;

                        for (int i = 0; i < array.Length; i++)
                        {
                            ProperBitConverter.GetBytes(b, i * bytesPerElement, array[i]);
                        }
                    }

                    writer.Stream.Write(tempBuffer.Array, 0, byteCount);
                }
            }
        }

        private static void WritePrimitiveArray_float(BinaryDataWriter writer, object o)
        {
            float[] array = o as float[];
            int bytesPerElement = sizeof(float);
            int byteCount = array.Length * bytesPerElement;

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(bytesPerElement);

            // We copy to a buffer in order to write the entire array into the stream with one call
            // We use our internal buffer if there's space in it, otherwise we claim a buffer from a cache.
            if (writer.TryEnsureBufferSpace(byteCount))
            {
                if (BitConverter.IsLittleEndian)
                {
                    fixed (byte* toBase = writer.buffer)
                    fixed (void* from = array)
                    {
                        void* to = toBase + writer.bufferIndex;

                        UnsafeUtilities.MemoryCopy(from, to, byteCount);
                    }

                    writer.bufferIndex += byteCount;
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.UNSAFE_WriteToBuffer_4_Float32(array[i]);
                    }
                }
            }
            else
            {
                // Ensure stream is up to date with the buffer before we write directly to it
                writer.FlushToStream();

                using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        // We always store in little endian, so we can do a direct memory mapping, which is a lot faster
                        UnsafeUtilities.MemoryCopy(array, tempBuffer.Array, byteCount, 0, 0);
                    }
                    else
                    {
                        // We have to convert each individual element to bytes, since the byte order has to be reversed
                        var b = tempBuffer.Array;

                        for (int i = 0; i < array.Length; i++)
                        {
                            ProperBitConverter.GetBytes(b, i * bytesPerElement, array[i]);
                        }
                    }

                    writer.Stream.Write(tempBuffer.Array, 0, byteCount);
                }
            }
        }

        private static void WritePrimitiveArray_double(BinaryDataWriter writer, object o)
        {
            double[] array = o as double[];
            int bytesPerElement = sizeof(double);
            int byteCount = array.Length * bytesPerElement;

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(bytesPerElement);

            // We copy to a buffer in order to write the entire array into the stream with one call
            // We use our internal buffer if there's space in it, otherwise we claim a buffer from a cache.
            if (writer.TryEnsureBufferSpace(byteCount))
            {
                if (BitConverter.IsLittleEndian)
                {
                    fixed (byte* toBase = writer.buffer)
                    fixed (void* from = array)
                    {
                        void* to = toBase + writer.bufferIndex;

                        UnsafeUtilities.MemoryCopy(from, to, byteCount);
                    }

                    writer.bufferIndex += byteCount;
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.UNSAFE_WriteToBuffer_8_Float64(array[i]);
                    }
                }
            }
            else
            {
                // Ensure stream is up to date with the buffer before we write directly to it
                writer.FlushToStream();

                using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        // We always store in little endian, so we can do a direct memory mapping, which is a lot faster
                        UnsafeUtilities.MemoryCopy(array, tempBuffer.Array, byteCount, 0, 0);
                    }
                    else
                    {
                        // We have to convert each individual element to bytes, since the byte order has to be reversed
                        var b = tempBuffer.Array;

                        for (int i = 0; i < array.Length; i++)
                        {
                            ProperBitConverter.GetBytes(b, i * bytesPerElement, array[i]);
                        }
                    }

                    writer.Stream.Write(tempBuffer.Array, 0, byteCount);
                }
            }
        }

        private static void WritePrimitiveArray_Guid(BinaryDataWriter writer, object o)
        {
            Guid[] array = o as Guid[];
            int bytesPerElement = sizeof(Guid);
            int byteCount = array.Length * bytesPerElement;

            writer.EnsureBufferSpace(9);

            // Write entry flag
            writer.buffer[writer.bufferIndex++] = (byte)BinaryEntryType.PrimitiveArray;

            writer.UNSAFE_WriteToBuffer_4_Int32(array.Length);
            writer.UNSAFE_WriteToBuffer_4_Int32(bytesPerElement);

            // We copy to a buffer in order to write the entire array into the stream with one call
            // We use our internal buffer if there's space in it, otherwise we claim a buffer from a cache.
            if (writer.TryEnsureBufferSpace(byteCount))
            {
                if (BitConverter.IsLittleEndian)
                {
                    fixed (byte* toBase = writer.buffer)
                    fixed (void* from = array)
                    {
                        void* to = toBase + writer.bufferIndex;

                        UnsafeUtilities.MemoryCopy(from, to, byteCount);
                    }

                    writer.bufferIndex += byteCount;
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.UNSAFE_WriteToBuffer_16_Guid(array[i]);
                    }
                }
            }
            else
            {
                // Ensure stream is up to date with the buffer before we write directly to it
                writer.FlushToStream();

                using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        // We always store in little endian, so we can do a direct memory mapping, which is a lot faster
                        UnsafeUtilities.MemoryCopy(array, tempBuffer.Array, byteCount, 0, 0);
                    }
                    else
                    {
                        // We have to convert each individual element to bytes, since the byte order has to be reversed
                        var b = tempBuffer.Array;

                        for (int i = 0; i < array.Length; i++)
                        {
                            ProperBitConverter.GetBytes(b, i * bytesPerElement, array[i]);
                        }
                    }

                    writer.Stream.Write(tempBuffer.Array, 0, byteCount);
                }
            }
        }

        /// <summary>
        /// Writes a primitive array to the stream.
        /// </summary>
        /// <typeparam name="T">The element type of the primitive array. Valid element types can be determined using <see cref="FormatterUtilities.IsPrimitiveArrayType(Type)" />.</typeparam>
        /// <param name="array">The primitive array to write.</param>
        /// <exception cref="System.ArgumentException">Type  + typeof(T).Name +  is not a valid primitive array type.</exception>
        public override void WritePrimitiveArray<T>(T[] array)
        {
            Action<BinaryDataWriter, object> writer;

            if (!PrimitiveArrayWriters.TryGetValue(typeof(T), out writer))
            {
                throw new ArgumentException("Type " + typeof(T).Name + " is not a valid primitive array type.");
            }

            writer(this, array);
        }

        /// <summary>
        /// Writes a <see cref="bool" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteBoolean(string name, bool value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedBoolean;

                WriteStringFast(name);

                EnsureBufferSpace(1);
                buffer[bufferIndex++] = value ? (byte)1 : (byte)0;
            }
            else
            {
                EnsureBufferSpace(2);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedBoolean;
                buffer[bufferIndex++] = value ? (byte)1 : (byte)0;
            }

        }

        /// <summary>
        /// Writes a <see cref="byte" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteByte(string name, byte value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedByte;

                WriteStringFast(name);

                EnsureBufferSpace(1);
                buffer[bufferIndex++] = value;
            }
            else
            {
                EnsureBufferSpace(2);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedByte;
                buffer[bufferIndex++] = value;
            }
        }

        /// <summary>
        /// Writes a <see cref="char" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteChar(string name, char value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedChar;

                WriteStringFast(name);

                EnsureBufferSpace(2);
                UNSAFE_WriteToBuffer_2_Char(value);
            }
            else
            {
                EnsureBufferSpace(3);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedChar;
                UNSAFE_WriteToBuffer_2_Char(value);
            }

        }

        /// <summary>
        /// Writes a <see cref="decimal" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteDecimal(string name, decimal value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedDecimal;

                WriteStringFast(name);

                EnsureBufferSpace(16);
                UNSAFE_WriteToBuffer_16_Decimal(value);
            }
            else
            {
                EnsureBufferSpace(17);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedDecimal;
                UNSAFE_WriteToBuffer_16_Decimal(value);
            }
        }

        /// <summary>
        /// Writes a <see cref="double" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteDouble(string name, double value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedDouble;

                WriteStringFast(name);

                EnsureBufferSpace(8);
                UNSAFE_WriteToBuffer_8_Float64(value);
            }
            else
            {
                EnsureBufferSpace(9);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedDouble;
                UNSAFE_WriteToBuffer_8_Float64(value);
            }

        }

        /// <summary>
        /// Writes a <see cref="Guid" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteGuid(string name, Guid value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedGuid;

                WriteStringFast(name);

                EnsureBufferSpace(16);
                UNSAFE_WriteToBuffer_16_Guid(value);
            }
            else
            {
                EnsureBufferSpace(17);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedGuid;
                UNSAFE_WriteToBuffer_16_Guid(value);
            }

        }

        /// <summary>
        /// Writes an external guid reference to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="guid">The value to write.</param>
        public override void WriteExternalReference(string name, Guid guid)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedExternalReferenceByGuid;

                WriteStringFast(name);

                EnsureBufferSpace(16);
                UNSAFE_WriteToBuffer_16_Guid(guid);
            }
            else
            {
                EnsureBufferSpace(17);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedExternalReferenceByGuid;
                UNSAFE_WriteToBuffer_16_Guid(guid);
            }
        }

        /// <summary>
        /// Writes an external index reference to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="index">The value to write.</param>
        public override void WriteExternalReference(string name, int index)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedExternalReferenceByIndex;

                WriteStringFast(name);

                EnsureBufferSpace(4);
                UNSAFE_WriteToBuffer_4_Int32(index);
            }
            else
            {
                EnsureBufferSpace(5);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedExternalReferenceByIndex;
                UNSAFE_WriteToBuffer_4_Int32(index);
            }
        }

        /// <summary>
        /// Writes an external string reference to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="id">The value to write.</param>
        public override void WriteExternalReference(string name, string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedExternalReferenceByString;
                WriteStringFast(name);
            }
            else
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedExternalReferenceByString;
            }

            WriteStringFast(id);
        }

        /// <summary>
        /// Writes an <see cref="int" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteInt32(string name, int value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedInt;

                WriteStringFast(name);

                EnsureBufferSpace(4);
                UNSAFE_WriteToBuffer_4_Int32(value);
            }
            else
            {
                EnsureBufferSpace(5);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedInt;
                UNSAFE_WriteToBuffer_4_Int32(value);
            }
        }

        /// <summary>
        /// Writes a <see cref="long" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteInt64(string name, long value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedLong;

                WriteStringFast(name);

                EnsureBufferSpace(8);
                UNSAFE_WriteToBuffer_8_Int64(value);
            }
            else
            {
                EnsureBufferSpace(9);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedLong;
                UNSAFE_WriteToBuffer_8_Int64(value);
            }
        }

        /// <summary>
        /// Writes a null value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        public override void WriteNull(string name)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedNull;
                WriteStringFast(name);
            }
            else
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedNull;
            }
        }

        /// <summary>
        /// Writes an internal reference to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="id">The value to write.</param>
        public override void WriteInternalReference(string name, int id)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedInternalReference;

                WriteStringFast(name);

                EnsureBufferSpace(4);
                UNSAFE_WriteToBuffer_4_Int32(id);
            }
            else
            {
                EnsureBufferSpace(5);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedInternalReference;
                UNSAFE_WriteToBuffer_4_Int32(id);
            }
        }

        /// <summary>
        /// Writes an <see cref="sbyte" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteSByte(string name, sbyte value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedSByte;

                WriteStringFast(name);

                EnsureBufferSpace(1);

                unchecked
                {
                    buffer[bufferIndex++] = (byte)value;
                }
            }
            else
            {
                EnsureBufferSpace(2);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedSByte;

                unchecked
                {
                    buffer[bufferIndex++] = (byte)value;
                }
            }
        }

        /// <summary>
        /// Writes a <see cref="short" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteInt16(string name, short value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedShort;

                WriteStringFast(name);

                EnsureBufferSpace(2);
                UNSAFE_WriteToBuffer_2_Int16(value);
            }
            else
            {
                EnsureBufferSpace(3);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedShort;
                UNSAFE_WriteToBuffer_2_Int16(value);
            }
        }

        /// <summary>
        /// Writes a <see cref="float" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteSingle(string name, float value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedFloat;

                WriteStringFast(name);

                EnsureBufferSpace(4);
                UNSAFE_WriteToBuffer_4_Float32(value);
            }
            else
            {
                EnsureBufferSpace(5);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedFloat;
                UNSAFE_WriteToBuffer_4_Float32(value);
            }
        }

        /// <summary>
        /// Writes a <see cref="string" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteString(string name, string value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedString;

                WriteStringFast(name);
            }
            else
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedString;
            }

            WriteStringFast(value);
        }

        /// <summary>
        /// Writes an <see cref="uint" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteUInt32(string name, uint value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedUInt;

                WriteStringFast(name);

                EnsureBufferSpace(4);
                UNSAFE_WriteToBuffer_4_UInt32(value);
            }
            else
            {
                EnsureBufferSpace(5);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedUInt;
                UNSAFE_WriteToBuffer_4_UInt32(value);
            }
        }

        /// <summary>
        /// Writes an <see cref="ulong" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteUInt64(string name, ulong value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedULong;

                WriteStringFast(name);

                EnsureBufferSpace(8);
                UNSAFE_WriteToBuffer_8_UInt64(value);
            }
            else
            {
                EnsureBufferSpace(9);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedULong;
                UNSAFE_WriteToBuffer_8_UInt64(value);
            }
        }

        /// <summary>
        /// Writes an <see cref="ushort" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteUInt16(string name, ushort value)
        {
            if (name != null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.NamedUShort;

                WriteStringFast(name);

                EnsureBufferSpace(2);
                UNSAFE_WriteToBuffer_2_UInt16(value);
            }
            else
            {
                EnsureBufferSpace(3);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedUShort;
                UNSAFE_WriteToBuffer_2_UInt16(value);
            }
        }

        /// <summary>
        /// Tells the writer that a new serialization session is about to begin, and that it should clear all cached values left over from any prior serialization sessions.
        /// This method is only relevant when the same writer is used to serialize several different, unrelated values.
        /// </summary>
        public override void PrepareNewSerializationSession()
        {
            base.PrepareNewSerializationSession();
            types.Clear();
            bufferIndex = 0;
        }

        public override string GetDataDump()
        {
            if (!Stream.CanRead)
            {
                return "Binary data stream for writing cannot be read; cannot dump data.";
            }

            if (!Stream.CanSeek)
            {
                return "Binary data stream cannot seek; cannot dump data.";
            }

            FlushToStream();

            var oldPosition = Stream.Position;

            var bytes = new byte[oldPosition];

            Stream.Position = 0;
            Stream.Read(bytes, 0, (int)oldPosition);

            Stream.Position = oldPosition;

            return "Binary hex dump: " + ProperBitConverter.BytesToHexString(bytes);
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private void WriteType(Type type)
        {
            if (type == null)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)BinaryEntryType.UnnamedNull;
            }
            else
            {
                int id;

                if (types.TryGetValue(type, out id))
                {
                    EnsureBufferSpace(5);
                    buffer[bufferIndex++] = (byte)BinaryEntryType.TypeID;
                    UNSAFE_WriteToBuffer_4_Int32(id);
                }
                else
                {
                    id = types.Count;
                    types.Add(type, id);

                    EnsureBufferSpace(5);
                    buffer[bufferIndex++] = (byte)BinaryEntryType.TypeName;
                    UNSAFE_WriteToBuffer_4_Int32(id);
                    WriteStringFast(Context.Binder.BindToName(type));
                }
            }
        }

        private struct Struct256Bit
        {
            public decimal d1;
            public decimal d2;
        }

        private void WriteStringFast(string value)
        {
            bool needs16BitsPerChar = true;
            int byteCount;

            if (CompressStringsTo8BitWhenPossible)
            {
                needs16BitsPerChar = false;

                // Check if the string requires 16 bit support
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] > 255)
                    {
                        needs16BitsPerChar = true;
                        break;
                    }
                }
            }

            if (needs16BitsPerChar)
            {
                byteCount = value.Length * 2;

                if (TryEnsureBufferSpace(byteCount + 5))
                {
                    buffer[bufferIndex++] = 1; // Write 16 bit flag
                    UNSAFE_WriteToBuffer_4_Int32(value.Length);

                    if (BitConverter.IsLittleEndian)
                    {
                        fixed (byte* baseToPtr = buffer)
                        fixed (char* baseFromPtr = value)
                        {
                            Struct256Bit* toPtr = (Struct256Bit*)(baseToPtr + bufferIndex);
                            Struct256Bit* fromPtr = (Struct256Bit*)baseFromPtr;

                            byte* toEnd = (byte*)toPtr + byteCount;

                            while ((toPtr + 1) <= toEnd)
                            {
                                *toPtr++ = *fromPtr++;
                            }

                            char* toPtrRest = (char*)toPtr;
                            char* fromPtrRest = (char*)fromPtr;

                            while (toPtrRest < toEnd)
                            {
                                *toPtrRest++ = *fromPtrRest++;
                            }
                        }
                    }
                    else
                    {
                        fixed (byte* baseToPtr = buffer)
                        fixed (char* baseFromPtr = value)
                        {
                            byte* toPtr = baseToPtr + bufferIndex;
                            byte* fromPtr = (byte*)baseFromPtr;

                            for (int i = 0; i < byteCount; i += 2)
                            {
                                *toPtr = *(fromPtr + 1);
                                *(toPtr + 1) = *fromPtr;

                                fromPtr += 2;
                                toPtr += 2;
                            }
                        }
                    }

                    bufferIndex += byteCount;
                }
                else
                {
                    // Our internal buffer doesn't have space for this string - use the stream directly
                    FlushToStream(); // Ensure stream is up to date with buffer before we write directly to it
                    Stream.WriteByte(1); // Write 16 bit flag

                    ProperBitConverter.GetBytes(small_buffer, 0, value.Length);
                    Stream.Write(small_buffer, 0, 4);

                    using (var tempBuffer = Buffer<byte>.Claim(byteCount))
                    {
                        var array = tempBuffer.Array;
                        UnsafeUtilities.StringToBytes(array, value, true);
                        Stream.Write(array, 0, byteCount);
                    }
                }
            }
            else
            {
                byteCount = value.Length;

                if (TryEnsureBufferSpace(byteCount + 5))
                {
                    buffer[bufferIndex++] = 0; // Write 8 bit flag
                    UNSAFE_WriteToBuffer_4_Int32(value.Length);

                    for (int i = 0; i < byteCount; i++)
                    {
                        buffer[bufferIndex++] = (byte)value[i];
                    }
                }
                else
                {
                    // Our internal buffer doesn't have space for this string - use the stream directly
                    FlushToStream(); // Ensure stream is up to date with buffer before we write directly to it
                    Stream.WriteByte(0); // Write 8 bit flag

                    ProperBitConverter.GetBytes(small_buffer, 0, value.Length);
                    Stream.Write(small_buffer, 0, 4);

                    using (var tempBuffer = Buffer<byte>.Claim(value.Length))
                    {
                        var array = tempBuffer.Array;

                        for (int i = 0; i < value.Length; i++)
                        {
                            array[i] = (byte)value[i];
                        }

                        Stream.Write(array, 0, value.Length);
                    }
                }
            }
        }

        public override void FlushToStream()
        {
            if (bufferIndex > 0)
            {
                Stream.Write(buffer, 0, bufferIndex);
                bufferIndex = 0;
            }

            base.FlushToStream();
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private void UNSAFE_WriteToBuffer_2_Char(char value)
        {
            fixed (byte* basePtr = buffer)
            {
                if (BitConverter.IsLittleEndian)
                {
                    *(char*)(basePtr + bufferIndex) = value;
                }
                else
                {
                    byte* ptrTo = basePtr + bufferIndex;
                    byte* ptrFrom = (byte*)&value + 1;

                    *ptrTo++ = *ptrFrom--;
                    *ptrTo = *ptrFrom;
                }
            }

            bufferIndex += 2;
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private void UNSAFE_WriteToBuffer_2_Int16(short value)
        {
            fixed (byte* basePtr = buffer)
            {
                if (BitConverter.IsLittleEndian)
                {
                    *(short*)(basePtr + bufferIndex) = value;
                }
                else
                {
                    byte* ptrTo = basePtr + bufferIndex;
                    byte* ptrFrom = (byte*)&value + 1;

                    *ptrTo++ = *ptrFrom--;
                    *ptrTo = *ptrFrom;
                }
            }

            bufferIndex += 2;
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private void UNSAFE_WriteToBuffer_2_UInt16(ushort value)
        {
            fixed (byte* basePtr = buffer)
            {
                if (BitConverter.IsLittleEndian)
                {
                    *(ushort*)(basePtr + bufferIndex) = value;
                }
                else
                {
                    byte* ptrTo = basePtr + bufferIndex;
                    byte* ptrFrom = (byte*)&value + 1;

                    *ptrTo++ = *ptrFrom--;
                    *ptrTo = *ptrFrom;
                }
            }

            bufferIndex += 2;
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private void UNSAFE_WriteToBuffer_4_Int32(int value)
        {
            fixed (byte* basePtr = buffer)
            {
                if (BitConverter.IsLittleEndian)
                {
                    *(int*)(basePtr + bufferIndex) = value;
                }
                else
                {
                    byte* ptrTo = basePtr + bufferIndex;
                    byte* ptrFrom = (byte*)&value + 3;

                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo = *ptrFrom;
                }
            }

            bufferIndex += 4;
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private void UNSAFE_WriteToBuffer_4_UInt32(uint value)
        {
            fixed (byte* basePtr = buffer)
            {
                if (BitConverter.IsLittleEndian)
                {
                    *(uint*)(basePtr + bufferIndex) = value;
                }
                else
                {
                    byte* ptrTo = basePtr + bufferIndex;
                    byte* ptrFrom = (byte*)&value + 3;

                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo = *ptrFrom;
                }
            }

            bufferIndex += 4;
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private void UNSAFE_WriteToBuffer_4_Float32(float value)
        {
            fixed (byte* basePtr = buffer)
            {
                if (BitConverter.IsLittleEndian)
                {
                    if (ArchitectureInfo.Architecture_Supports_All_Unaligned_ReadWrites)
                    {
                        // We can write directly to the buffer, safe in the knowledge that any potential unaligned writes will work
                        *(float*)(basePtr + bufferIndex) = value;
                    }
                    else
                    {
                        // We do a slower but safer byte-by-byte write instead.
                        // Apparently doing this bit through an int pointer alias can also crash sometimes.
                        // Hence, we just do a byte-by-byte write to be safe.
                        byte* from = (byte*)&value;
                        byte* to = basePtr + bufferIndex;

                        *to++ = *from++;
                        *to++ = *from++;
                        *to++ = *from++;
                        *to = *from;
                    }
                }
                else
                {
                    byte* ptrTo = basePtr + bufferIndex;
                    byte* ptrFrom = (byte*)&value + 3;

                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo = *ptrFrom;
                }
            }

            bufferIndex += 4;
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private void UNSAFE_WriteToBuffer_8_Int64(long value)
        {
            fixed (byte* basePtr = buffer)
            {
                if (BitConverter.IsLittleEndian)
                {
                    if (ArchitectureInfo.Architecture_Supports_All_Unaligned_ReadWrites)
                    {
                        // We can write directly to the buffer, safe in the knowledge that any potential unaligned writes will work
                        *(long*)(basePtr + bufferIndex) = value;
                    }
                    else
                    {
                        // We do a slower but safer int-by-int write instead
                        int* fromPtr = (int*)&value;
                        int* toPtr = (int*)(basePtr + bufferIndex);

                        *toPtr++ = *fromPtr++;
                        *toPtr = *fromPtr;
                    }
                }
                else
                {
                    byte* ptrTo = basePtr + bufferIndex;
                    byte* ptrFrom = (byte*)&value + 7;

                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo = *ptrFrom;
                }
            }

            bufferIndex += 8;
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private void UNSAFE_WriteToBuffer_8_UInt64(ulong value)
        {
            fixed (byte* basePtr = buffer)
            {
                if (BitConverter.IsLittleEndian)
                {
                    if (ArchitectureInfo.Architecture_Supports_All_Unaligned_ReadWrites)
                    {
                        // We can write directly to the buffer, safe in the knowledge that any potential unaligned writes will work
                        *(ulong*)(basePtr + bufferIndex) = value;
                    }
                    else
                    {
                        // We do a slower but safer int-by-int write instead
                        int* fromPtr = (int*)&value;
                        int* toPtr = (int*)(basePtr + bufferIndex);

                        *toPtr++ = *fromPtr++;
                        *toPtr = *fromPtr;
                    }
                }
                else
                {
                    byte* ptrTo = basePtr + bufferIndex;
                    byte* ptrFrom = (byte*)&value + 7;

                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo = *ptrFrom;
                }
            }

            bufferIndex += 8;
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private void UNSAFE_WriteToBuffer_8_Float64(double value)
        {
            fixed (byte* basePtr = buffer)
            {
                if (BitConverter.IsLittleEndian)
                {
                    if (ArchitectureInfo.Architecture_Supports_All_Unaligned_ReadWrites)
                    {
                        // We can write directly to the buffer, safe in the knowledge that any potential unaligned writes will work
                        *(double*)(basePtr + bufferIndex) = value;
                    }
                    else
                    {
                        // We do a slower but safer int-by-int write instead
                        int* fromPtr = (int*)&value;
                        int* toPtr = (int*)(basePtr + bufferIndex);

                        *toPtr++ = *fromPtr++;
                        *toPtr = *fromPtr;
                    }
                }
                else
                {
                    byte* ptrTo = basePtr + bufferIndex;
                    byte* ptrFrom = (byte*)&value + 7;

                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo = *ptrFrom;
                }
            }

            bufferIndex += 8;
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private void UNSAFE_WriteToBuffer_16_Decimal(decimal value)
        {
            fixed (byte* basePtr = buffer)
            {
                if (BitConverter.IsLittleEndian)
                {
                    if (ArchitectureInfo.Architecture_Supports_All_Unaligned_ReadWrites)
                    {
                        // We can write directly to the buffer, safe in the knowledge that any potential unaligned writes will work
                        *(decimal*)(basePtr + bufferIndex) = value;
                    }
                    else
                    {
                        // We do a slower but safer int-by-int write instead
                        int* fromPtr = (int*)&value;
                        int* toPtr = (int*)(basePtr + bufferIndex);

                        *toPtr++ = *fromPtr++;
                        *toPtr++ = *fromPtr++;
                        *toPtr++ = *fromPtr++;
                        *toPtr = *fromPtr;
                    }
                }
                else
                {
                    byte* ptrTo = basePtr + bufferIndex;
                    byte* ptrFrom = (byte*)&value + 15;

                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo = *ptrFrom;
                }
            }

            bufferIndex += 16;
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private void UNSAFE_WriteToBuffer_16_Guid(Guid value)
        {
            // First 10 bytes of a guid are always little endian
            // Last 6 bytes depend on architecture endianness
            // See http://stackoverflow.com/questions/10190817/guid-byte-order-in-net

            // TODO: Test if this actually works on big-endian architecture. Where the hell do we find that?

            fixed (byte* basePtr = buffer)
            {
                if (BitConverter.IsLittleEndian)
                {
                    if (ArchitectureInfo.Architecture_Supports_All_Unaligned_ReadWrites)
                    {
                        // We can write directly to the buffer, safe in the knowledge that any potential unaligned writes will work
                        *(Guid*)(basePtr + bufferIndex) = value;
                    }
                    else
                    {
                        // We do a slower but safer int-by-int write instead
                        int* fromPtr = (int*)&value;
                        int* toPtr = (int*)(basePtr + bufferIndex);

                        *toPtr++ = *fromPtr++;
                        *toPtr++ = *fromPtr++;
                        *toPtr++ = *fromPtr++;
                        *toPtr = *fromPtr;
                    }
                }
                else
                {
                    byte* ptrTo = basePtr + bufferIndex;
                    byte* ptrFrom = (byte*)&value;

                    *ptrTo++ = *ptrFrom++;
                    *ptrTo++ = *ptrFrom++;
                    *ptrTo++ = *ptrFrom++;
                    *ptrTo++ = *ptrFrom++;
                    *ptrTo++ = *ptrFrom++;
                    *ptrTo++ = *ptrFrom++;
                    *ptrTo++ = *ptrFrom++;
                    *ptrTo++ = *ptrFrom++;
                    *ptrTo++ = *ptrFrom++;
                    *ptrTo++ = *ptrFrom;

                    ptrFrom += 6;

                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo++ = *ptrFrom--;
                    *ptrTo = *ptrFrom;
                }
            }

            bufferIndex += 16;
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private void EnsureBufferSpace(int space)
        {
            var length = buffer.Length;

            if (space > length)
            {
                throw new Exception("Insufficient buffer capacity");
            }

            if (bufferIndex + space > length)
            {
                FlushToStream();
            }
        }

        [MethodImpl((MethodImplOptions)0x100)]  // Set aggressive inlining flag, for the runtimes that understand that
        private bool TryEnsureBufferSpace(int space)
        {
            var length = buffer.Length;

            if (space > length)
            {
                return false;
            }

            if (bufferIndex + space > length)
            {
                FlushToStream();
            }

            return true;
        }
    }
}