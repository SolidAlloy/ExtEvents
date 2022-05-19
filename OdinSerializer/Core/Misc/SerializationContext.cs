//-----------------------------------------------------------------------
// <copyright file="SerializationContext.cs" company="Sirenix IVS">
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
    using System.Runtime.Serialization;
    using Utilities;

    /// <summary>
    /// The context of a given serialization session. This class maintains all internal and external references during serialization.
    /// </summary>
    /// <seealso cref="ICacheNotificationReceiver" />
    public sealed class SerializationContext : ICacheNotificationReceiver
    {
        private SerializationConfig config;
        private Dictionary<object, int> internalReferenceIdMap = new Dictionary<object, int>(128, ReferenceEqualityComparer<object>.Default);
        private StreamingContext streamingContext;
        private IFormatterConverter formatterConverter;
        private TwoWaySerializationBinder binder;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationContext"/> class.
        /// </summary>
        public SerializationContext()
            : this(new StreamingContext(), new FormatterConverter())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationContext"/> class.
        /// </summary>
        /// <param name="context">The streaming context to use.</param>
        /// <param name="formatterConverter">The formatter converter to use.</param>
        /// <exception cref="System.ArgumentNullException">The formatterConverter parameter is null.</exception>
        public SerializationContext(StreamingContext context, FormatterConverter formatterConverter)
        {
            if (formatterConverter == null)
            {
                throw new ArgumentNullException("formatterConverter");
            }

            streamingContext = context;
            this.formatterConverter = formatterConverter;

            ResetToDefault();
        }

        /// <summary>
        /// Gets or sets the context's type binder.
        /// </summary>
        /// <value>
        /// The context's serialization binder.
        /// </value>
        public TwoWaySerializationBinder Binder
        {
            get
            {
                if (binder == null)
                {
                    binder = DefaultSerializationBinder.Default;
                }

                return binder;
            }

            set => binder = value;
        }

        /// <summary>
        /// Gets the streaming context.
        /// </summary>
        /// <value>
        /// The streaming context.
        /// </value>
        public StreamingContext StreamingContext => streamingContext;

        /// <summary>
        /// Gets the formatter converter.
        /// </summary>
        /// <value>
        /// The formatter converter.
        /// </value>
        public IFormatterConverter FormatterConverter => formatterConverter;

        /// <summary>
        /// Gets or sets the index reference resolver.
        /// </summary>
        /// <value>
        /// The index reference resolver.
        /// </value>
        public IExternalIndexReferenceResolver IndexReferenceResolver { get; set; }

        /// <summary>
        /// Gets or sets the string reference resolver.
        /// </summary>
        /// <value>
        /// The string reference resolver.
        /// </value>
        public IExternalStringReferenceResolver StringReferenceResolver { get; set; }

        /// <summary>
        /// Gets or sets the Guid reference resolver.
        /// </summary>
        /// <value>
        /// The Guid reference resolver.
        /// </value>
        public IExternalGuidReferenceResolver GuidReferenceResolver { get; set; }

        /// <summary>
        /// Gets or sets the serialization configuration.
        /// </summary>
        /// <value>
        /// The serialization configuration.
        /// </value>
        public SerializationConfig Config
        {
            get
            {
                if (config == null)
                {
                    config = new SerializationConfig();
                }

                return config;
            }
        }

        /// <summary>
        /// Tries to register an internal reference. Returns <c>true</c> if the reference was registered, otherwise, <c>false</c> when the reference has already been registered.
        /// </summary>
        /// <param name="reference">The reference to register.</param>
        /// <param name="id">The id of the registered reference.</param>
        /// <returns><c>true</c> if the reference was registered, otherwise, <c>false</c> when the reference has already been registered.</returns>
        public bool TryRegisterInternalReference(object reference, out int id)
        {
            if (internalReferenceIdMap.TryGetValue(reference, out id) == false)
            {
                id = internalReferenceIdMap.Count;
                internalReferenceIdMap.Add(reference, id);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to register an external index reference.
        /// </summary>
        /// <param name="obj">The object to reference.</param>
        /// <param name="index">The index of the referenced object.</param>
        /// <returns><c>true</c> if the object could be referenced by index; otherwise, <c>false</c>.</returns>
        public bool TryRegisterExternalReference(object obj, out int index)
        {
            if (IndexReferenceResolver == null)
            {
                index = -1;
                return false;
            }

            if (IndexReferenceResolver.CanReference(obj, out index))
            {
                return true;
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// Tries to register an external guid reference.
        /// </summary>
        /// <param name="obj">The object to reference.</param>
        /// <param name="guid">The guid of the referenced object.</param>
        /// <returns><c>true</c> if the object could be referenced by guid; otherwise, <c>false</c>.</returns>
        public bool TryRegisterExternalReference(object obj, out Guid guid)
        {
            if (GuidReferenceResolver == null)
            {
                guid = Guid.Empty;
                return false;
            }

            var resolver = GuidReferenceResolver;

            while (resolver != null)
            {
                if (resolver.CanReference(obj, out guid))
                {
                    return true;
                }

                resolver = resolver.NextResolver;
            }

            guid = Guid.Empty;
            return false;
        }

        /// <summary>
        /// Tries to register an external string reference.
        /// </summary>
        /// <param name="obj">The object to reference.</param>
        /// <param name="id">The id string of the referenced object.</param>
        /// <returns><c>true</c> if the object could be referenced by string; otherwise, <c>false</c>.</returns>
        public bool TryRegisterExternalReference(object obj, out string id)
        {
            if (StringReferenceResolver == null)
            {
                id = null;
                return false;
            }

            var resolver = StringReferenceResolver;

            while (resolver != null)
            {
                if (resolver.CanReference(obj, out id))
                {
                    return true;
                }

                resolver = resolver.NextResolver;
            }

            id = null;
            return false;
        }

        /// <summary>
        /// Resets the serialization context completely to baseline status, as if its constructor has just been called.
        /// This allows complete reuse of a serialization context, with all of its internal reference buffers.
        /// </summary>
        public void ResetToDefault()
        {
            if (!ReferenceEquals(config, null))
            {
                config.ResetToDefault();
            }

            internalReferenceIdMap.Clear();
            IndexReferenceResolver = null;
            GuidReferenceResolver = null;
            StringReferenceResolver = null;
            binder = null;
        }

        void ICacheNotificationReceiver.OnFreed()
        {
            ResetToDefault();
        }

        void ICacheNotificationReceiver.OnClaimed()
        {
        }
    }
}