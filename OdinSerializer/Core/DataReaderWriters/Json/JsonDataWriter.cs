//-----------------------------------------------------------------------
// <copyright file="JsonDataWriter.cs" company="Sirenix IVS">
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
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Writes json data to a stream that can be read by a <see cref="JsonDataReader"/>.
    /// </summary>
    /// <seealso cref="BaseDataWriter" />
    public class JsonDataWriter : BaseDataWriter
    {
        private static readonly uint[] ByteToHexCharLookup = CreateByteToHexLookup();
        private static readonly string NEW_LINE = Environment.NewLine;

        private bool justStarted;
        private bool forceNoSeparatorNextLine;

        //private StringBuilder escapeStringBuilder;
        //private StreamWriter writer;

        private Dictionary<Type, Delegate> primitiveTypeWriters;
        private Dictionary<Type, int> seenTypes = new Dictionary<Type, int>(16);

        private byte[] buffer = new byte[1024 * 100];
        private int bufferIndex;

        public JsonDataWriter() : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDataWriter" /> class.
        /// </summary>
        /// <param name="stream">The base stream of the writer.</param>
        /// <param name="context">The serialization context to use.</param>>
        /// <param name="formatAsReadable">Whether the json should be packed, or formatted as human-readable.</param>
        public JsonDataWriter(Stream stream, SerializationContext context, bool formatAsReadable = true) : base(stream, context)
        {
            FormatAsReadable = formatAsReadable;
            justStarted = true;
            EnableTypeOptimization = true;

            primitiveTypeWriters = new Dictionary<Type, Delegate>
            {
                { typeof(char), (Action<string, char>)WriteChar },
                { typeof(sbyte), (Action<string, sbyte>)WriteSByte },
                { typeof(short), (Action<string, short>)WriteInt16 },
                { typeof(int), (Action<string, int>)WriteInt32 },
                { typeof(long), (Action<string, long>)WriteInt64 },
                { typeof(byte), (Action<string, byte>)WriteByte },
                { typeof(ushort), (Action<string, ushort>)WriteUInt16 },
                { typeof(uint),   (Action<string, uint>)WriteUInt32 },
                { typeof(ulong),  (Action<string, ulong>)WriteUInt64 },
                { typeof(decimal),   (Action<string, decimal>)WriteDecimal },
                { typeof(bool),  (Action<string, bool>)WriteBoolean },
                { typeof(float),  (Action<string, float>)WriteSingle },
                { typeof(double),  (Action<string, double>)WriteDouble },
                { typeof(Guid),  (Action<string, Guid>)WriteGuid }
            };
        }

        /// <summary>
        /// Gets or sets a value indicating whether the json should be packed, or formatted as human-readable.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the json should be formatted as human-readable; otherwise, <c>false</c>.
        /// </value>
        public bool FormatAsReadable;

        /// <summary>
        /// Whether to enable an optimization that ensures any given type name is only written once into the json stream, and thereafter kept track of by ID.
        /// </summary>
        public bool EnableTypeOptimization;

        /// <summary>
        /// Enable the "just started" flag, causing the writer to start a new "base" json object container.
        /// </summary>
        public void MarkJustStarted()
        {
            justStarted = true;
        }

        /// <summary>
        /// Flushes everything that has been written so far to the writer's base stream.
        /// </summary>
        public override void FlushToStream()
        {
            if (bufferIndex > 0)
            {
                Stream.Write(buffer, 0, bufferIndex);
                bufferIndex = 0;
            }

            base.FlushToStream();
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
            WriteEntry(name, "{");
            PushNode(name, id, type);
            forceNoSeparatorNextLine = true;
            WriteInt32(JsonConfig.ID_SIG, id);

            if (type != null)
            {
                WriteTypeEntry(type);
            }
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
            WriteEntry(name, "{");
            PushNode(name, -1, type);
            forceNoSeparatorNextLine = true;

            if (type != null)
            {
                WriteTypeEntry(type);
            }
        }

        /// <summary>
        /// Ends the current node with the given name. If the current node has another name, an <see cref="InvalidOperationException" /> is thrown.
        /// </summary>
        /// <param name="name">The name of the node to end. This has to be the name of the current node.</param>
        public override void EndNode(string name)
        {
            PopNode();
            StartNewLine(true);

            EnsureBufferSpace(1);
            buffer[bufferIndex++] = (byte)'}';
        }

        /// <summary>
        /// Begins an array node of the given length.
        /// </summary>
        /// <param name="length">The length of the array to come.</param>
        public override void BeginArrayNode(long length)
        {
            WriteInt64(JsonConfig.REGULAR_ARRAY_LENGTH_SIG, length);
            WriteEntry(JsonConfig.REGULAR_ARRAY_CONTENT_SIG, "[");
            forceNoSeparatorNextLine = true;
            PushArray();
        }

        /// <summary>
        /// Ends the current array node, if the current node is an array node.
        /// </summary>
        public override void EndArrayNode()
        {
            PopArray();
            StartNewLine(true);

            EnsureBufferSpace(1);
            buffer[bufferIndex++] = (byte)']';
        }

        /// <summary>
        /// Writes a primitive array to the stream.
        /// </summary>
        /// <typeparam name="T">The element type of the primitive array. Valid element types can be determined using <see cref="FormatterUtilities.IsPrimitiveArrayType(Type)" />.</typeparam>
        /// <param name="array">The primitive array to write.</param>
        /// <exception cref="System.ArgumentException">Type  + typeof(T).Name +  is not a valid primitive array type.</exception>
        /// <exception cref="System.ArgumentNullException">array</exception>
        public override void WritePrimitiveArray<T>(T[] array)
        {
            if (FormatterUtilities.IsPrimitiveArrayType(typeof(T)) == false)
            {
                throw new ArgumentException("Type " + typeof(T).Name + " is not a valid primitive array type.");
            }

            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            Action<string, T> writer = (Action<string, T>)primitiveTypeWriters[typeof(T)];

            WriteInt64(JsonConfig.PRIMITIVE_ARRAY_LENGTH_SIG, array.Length);
            WriteEntry(JsonConfig.PRIMITIVE_ARRAY_CONTENT_SIG, "[");
            forceNoSeparatorNextLine = true;
            PushArray();

            for (int i = 0; i < array.Length; i++)
            {
                writer(null, array[i]);
            }

            PopArray();
            StartNewLine(true);

            EnsureBufferSpace(1);
            buffer[bufferIndex++] = (byte)']';
        }

        /// <summary>
        /// Writes a <see cref="bool" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteBoolean(string name, bool value)
        {
            WriteEntry(name, value ? "true" : "false");
        }

        /// <summary>
        /// Writes a <see cref="byte" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteByte(string name, byte value)
        {
            WriteUInt64(name, value);
        }

        /// <summary>
        /// Writes a <see cref="char" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteChar(string name, char value)
        {
            WriteString(name, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes a <see cref="decimal" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteDecimal(string name, decimal value)
        {
            WriteEntry(name, value.ToString("G", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes a <see cref="double" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteDouble(string name, double value)
        {
            WriteEntry(name, value.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes an <see cref="int" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteInt32(string name, int value)
        {
            WriteInt64(name, value);
        }

        /// <summary>
        /// Writes a <see cref="long" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteInt64(string name, long value)
        {
            WriteEntry(name, value.ToString("D", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes a null value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        public override void WriteNull(string name)
        {
            WriteEntry(name, "null");
        }

        /// <summary>
        /// Writes an internal reference to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="id">The value to write.</param>
        public override void WriteInternalReference(string name, int id)
        {
            WriteEntry(name, JsonConfig.INTERNAL_REF_SIG + ":" + id.ToString("D", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes an <see cref="sbyte" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteSByte(string name, sbyte value)
        {
            WriteInt64(name, value);
        }

        /// <summary>
        /// Writes a <see cref="short" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteInt16(string name, short value)
        {
            WriteInt64(name, value);
        }

        /// <summary>
        /// Writes a <see cref="float" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteSingle(string name, float value)
        {
            WriteEntry(name, value.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes a <see cref="string" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteString(string name, string value)
        {
            StartNewLine();

            if (name != null)
            {
                EnsureBufferSpace(name.Length + value.Length + 6);

                buffer[bufferIndex++] = (byte)'"';

                for (int i = 0; i < name.Length; i++)
                {
                    buffer[bufferIndex++] = (byte)name[i];
                }

                buffer[bufferIndex++] = (byte)'"';
                buffer[bufferIndex++] = (byte)':';

                if (FormatAsReadable)
                {
                    buffer[bufferIndex++] = (byte)' ';
                }
            }
            else EnsureBufferSpace(value.Length + 2);

            buffer[bufferIndex++] = (byte)'"';

            Buffer_WriteString_WithEscape(value);

            buffer[bufferIndex++] = (byte)'"';
        }

        /// <summary>
        /// Writes a <see cref="Guid" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteGuid(string name, Guid value)
        {
            WriteEntry(name, value.ToString("D", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes an <see cref="uint" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteUInt32(string name, uint value)
        {
            WriteUInt64(name, value);
        }

        /// <summary>
        /// Writes an <see cref="ulong" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteUInt64(string name, ulong value)
        {
            WriteEntry(name, value.ToString("D", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes an external index reference to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="index">The value to write.</param>
        public override void WriteExternalReference(string name, int index)
        {
            WriteEntry(name, JsonConfig.EXTERNAL_INDEX_REF_SIG + ":" + index.ToString("D", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes an external guid reference to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="guid">The value to write.</param>
        public override void WriteExternalReference(string name, Guid guid)
        {
            WriteEntry(name, JsonConfig.EXTERNAL_GUID_REF_SIG + ":" + guid.ToString("D", CultureInfo.InvariantCulture));
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

            WriteEntry(name, JsonConfig.EXTERNAL_STRING_REF_SIG_FIXED);
            EnsureBufferSpace(id.Length + 3);
            buffer[bufferIndex++] = (byte)':';
            buffer[bufferIndex++] = (byte)'"';
            Buffer_WriteString_WithEscape(id);
            buffer[bufferIndex++] = (byte)'"';
        }

        /// <summary>
        /// Writes an <see cref="ushort" /> value to the stream.
        /// </summary>
        /// <param name="name">The name of the value. If this is null, no name will be written.</param>
        /// <param name="value">The value to write.</param>
        public override void WriteUInt16(string name, ushort value)
        {
            WriteUInt64(name, value);
        }

        /// <summary>
        /// Disposes all resources kept by the data writer, except the stream, which can be reused later.
        /// </summary>
        public override void Dispose()
        {
            //this.writer.Dispose();
        }

        /// <summary>
        /// Tells the writer that a new serialization session is about to begin, and that it should clear all cached values left over from any prior serialization sessions.
        /// This method is only relevant when the same writer is used to serialize several different, unrelated values.
        /// </summary>
        public override void PrepareNewSerializationSession()
        {
            base.PrepareNewSerializationSession();
            seenTypes.Clear();
            justStarted = true;
        }

        public override string GetDataDump()
        {
            if (!Stream.CanRead)
            {
                return "Json data stream for writing cannot be read; cannot dump data.";
            }

            if (!Stream.CanSeek)
            {
                return "Json data stream cannot seek; cannot dump data.";
            }

            var oldPosition = Stream.Position;

            var bytes = new byte[oldPosition];

            Stream.Position = 0;
            Stream.Read(bytes, 0, (int)oldPosition);

            Stream.Position = oldPosition;

            return "Json: " + Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        private void WriteEntry(string name, string contents)
        {
            StartNewLine();

            if (name != null)
            {
                EnsureBufferSpace(name.Length + contents.Length + 4);

                buffer[bufferIndex++] = (byte)'"';

                for (int i = 0; i < name.Length; i++)
                {
                    buffer[bufferIndex++] = (byte)name[i];
                }

                buffer[bufferIndex++] = (byte)'"';
                buffer[bufferIndex++] = (byte)':';

                if (FormatAsReadable)
                {
                    buffer[bufferIndex++] = (byte)' ';
                }
            }
            else EnsureBufferSpace(contents.Length);

            for (int i = 0; i < contents.Length; i++)
            {
                buffer[bufferIndex++] = (byte)contents[i];
            }
        }

        private void WriteEntry(string name, string contents, char surroundContentsWith)
        {
            StartNewLine();

            if (name != null)
            {
                EnsureBufferSpace(name.Length + contents.Length + 6);

                buffer[bufferIndex++] = (byte)'"';

                for (int i = 0; i < name.Length; i++)
                {
                    buffer[bufferIndex++] = (byte)name[i];
                }

                buffer[bufferIndex++] = (byte)'"';
                buffer[bufferIndex++] = (byte)':';

                if (FormatAsReadable)
                {
                    buffer[bufferIndex++] = (byte)' ';
                }
            }
            else EnsureBufferSpace(contents.Length + 2);

            buffer[bufferIndex++] = (byte)surroundContentsWith;

            for (int i = 0; i < contents.Length; i++)
            {
                buffer[bufferIndex++] = (byte)contents[i];
            }

            buffer[bufferIndex++] = (byte)surroundContentsWith;
        }

        private void WriteTypeEntry(Type type)
        {
            int id;

            if (EnableTypeOptimization)
            {
                if (seenTypes.TryGetValue(type, out id))
                {
                    WriteInt32(JsonConfig.TYPE_SIG, id);
                }
                else
                {
                    id = seenTypes.Count;
                    seenTypes.Add(type, id);
                    WriteString(JsonConfig.TYPE_SIG, id + "|" + Context.Binder.BindToName(type, Context.Config.DebugContext));
                }
            }
            else
            {
                WriteString(JsonConfig.TYPE_SIG, Context.Binder.BindToName(type, Context.Config.DebugContext));
            }
        }

        private void StartNewLine(bool noSeparator = false)
        {
            if (justStarted)
            {
                justStarted = false;
                return;
            }

            if (noSeparator == false && forceNoSeparatorNextLine == false)
            {
                EnsureBufferSpace(1);
                buffer[bufferIndex++] = (byte)',';
            }

            forceNoSeparatorNextLine = false;

            if (FormatAsReadable)
            {
                int count = NodeDepth * 4;

                EnsureBufferSpace(NEW_LINE.Length + count);

                for (int i = 0; i < NEW_LINE.Length; i++)
                {
                    buffer[bufferIndex++] = (byte)NEW_LINE[i];
                }

                for (int i = 0; i < count; i++)
                {
                    buffer[bufferIndex++] = (byte)' ';
                }
            }
        }


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

        private void Buffer_WriteString_WithEscape(string str)
        {
            EnsureBufferSpace(str.Length);

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (c < 0 || c > 127)
                {
                    // We're outside the "standard" character range - so we write the character as a hexadecimal value instead
                    // This ensures that we don't break the Json formatting.

                    EnsureBufferSpace((str.Length - i) + 6);

                    buffer[bufferIndex++] = (byte)'\\';
                    buffer[bufferIndex++] = (byte)'u';

                    var byte1 = c >> 8;
                    var byte2 = (byte)c;

                    var lookup = ByteToHexCharLookup[byte1];

                    buffer[bufferIndex++] = (byte)lookup;
                    buffer[bufferIndex++] = (byte)(lookup >> 16);

                    lookup = ByteToHexCharLookup[byte2];

                    buffer[bufferIndex++] = (byte)lookup;
                    buffer[bufferIndex++] = (byte)(lookup >> 16);
                    continue;
                }

                EnsureBufferSpace(2);

                // Escape any characters that need to be escaped, default to no escape
                switch (c)
                {
                    case '"':
                        buffer[bufferIndex++] = (byte)'\\';
                        buffer[bufferIndex++] = (byte)'"';
                        break;

                    case '\\':
                        buffer[bufferIndex++] = (byte)'\\';
                        buffer[bufferIndex++] = (byte)'\\';
                        break;

                    case '\a':
                        buffer[bufferIndex++] = (byte)'\\';
                        buffer[bufferIndex++] = (byte)'a';
                        break;

                    case '\b':
                        buffer[bufferIndex++] = (byte)'\\';
                        buffer[bufferIndex++] = (byte)'b';
                        break;

                    case '\f':
                        buffer[bufferIndex++] = (byte)'\\';
                        buffer[bufferIndex++] = (byte)'f';
                        break;

                    case '\n':
                        buffer[bufferIndex++] = (byte)'\\';
                        buffer[bufferIndex++] = (byte)'n';
                        break;

                    case '\r':
                        buffer[bufferIndex++] = (byte)'\\';
                        buffer[bufferIndex++] = (byte)'r';
                        break;

                    case '\t':
                        buffer[bufferIndex++] = (byte)'\\';
                        buffer[bufferIndex++] = (byte)'t';
                        break;

                    case '\0':
                        buffer[bufferIndex++] = (byte)'\\';
                        buffer[bufferIndex++] = (byte)'0';
                        break;

                    default:
                        buffer[bufferIndex++] = (byte)c;
                        break;
                }
            }
        }

        private static uint[] CreateByteToHexLookup()
        {
            var result = new uint[256];

            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("x2", CultureInfo.InvariantCulture);
                result[i] = s[0] + ((uint)s[1] << 16);
            }

            return result;
        }
    }
}