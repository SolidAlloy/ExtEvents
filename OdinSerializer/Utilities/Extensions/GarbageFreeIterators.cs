//-----------------------------------------------------------------------
// <copyright file="GarbageFreeIterators.cs" company="Sirenix IVS">
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
namespace ExtEvents.OdinSerializer.Utilities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Garbage free enumerator methods.
    /// </summary>
    public static class GarbageFreeIterators
    {
        /// <summary>
        /// Garbage free enumerator for dictionaries.
        /// </summary>
        public static DictionaryIterator<T1, T2> GFIterator<T1, T2>(this Dictionary<T1, T2> dictionary)
        {
            return new DictionaryIterator<T1, T2>(dictionary);
        }

        /// <summary>
        /// Dictionary iterator.
        /// </summary>
        public struct DictionaryIterator<T1, T2> : IDisposable
        {
            private Dictionary<T1, T2> dictionary;
            private Dictionary<T1, T2>.Enumerator enumerator;
            private bool isNull;

            /// <summary>
            /// Creates a dictionary iterator.
            /// </summary>
            public DictionaryIterator(Dictionary<T1, T2> dictionary)
            {
                isNull = dictionary == null;

                if (isNull)
                {
                    this.dictionary = null;
                    enumerator = new Dictionary<T1, T2>.Enumerator();
                }
                else
                {
                    this.dictionary = dictionary;
                    enumerator = this.dictionary.GetEnumerator();
                }
            }

            /// <summary>
            /// Gets the enumerator.
            /// </summary>
            public DictionaryIterator<T1, T2> GetEnumerator()
            {
                return this;
            }

            /// <summary>
            /// Gets the current value.
            /// </summary>
            public KeyValuePair<T1, T2> Current => enumerator.Current;

            /// <summary>
            /// Moves to the next value.
            /// </summary>
            public bool MoveNext()
            {
                if (isNull)
                {
                    return false;
                }
                return enumerator.MoveNext();
            }

            /// <summary>
            /// Disposes the iterator.
            /// </summary>
            public void Dispose()
            {
                enumerator.Dispose();
            }
        }
    }
}