//-----------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Sirenix IVS">
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
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// String method extensions.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Eg MY_INT_VALUE => MyIntValue
        /// </summary>
        public static string ToTitleCase(this string input)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                var current = input[i];
                if (current == '_' && i + 1 < input.Length)
                {
                    var next = input[i + 1];
                    if (char.IsLower(next))
                    {
                        next = char.ToUpper(next, CultureInfo.InvariantCulture);
                    }

                    builder.Append(next);
                    i++;
                }
                else
                {
                    builder.Append(current);
                }
            }

            return builder.ToString();
        }
    }
}