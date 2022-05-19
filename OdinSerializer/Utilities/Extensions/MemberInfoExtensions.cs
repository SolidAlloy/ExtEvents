//-----------------------------------------------------------------------
// <copyright file="MemberInfoExtensions.cs" company="Sirenix IVS">
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
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// MemberInfo method extensions.
    /// </summary>
    public static class MemberInfoExtensions
    {
        /// <summary>
        /// Returns true if the attribute whose type is specified by the generic argument is defined on this member
        /// </summary>
        public static bool IsDefined<T>(this ICustomAttributeProvider member, bool inherit) where T : Attribute
        {
            try
            {
                return member.IsDefined(typeof(T), inherit);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the attribute whose type is specified by the generic argument is defined on this member
        /// </summary>
        public static bool IsDefined<T>(this ICustomAttributeProvider member) where T : Attribute
        {
            return IsDefined<T>(member, false);
        }

        /// <summary>
        /// Gets all attributes of the specified generic type.
        /// </summary>
        /// <param name="member">The member.</param>
        public static IEnumerable<T> GetAttributes<T>(this ICustomAttributeProvider member) where T : Attribute
        {
            return GetAttributes<T>(member, false);
        }

        /// <summary>
        /// Gets all attributes of the specified generic type.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="inherit">If true, specifies to also search the ancestors of element for custom attributes.</param>
        public static IEnumerable<T> GetAttributes<T>(this ICustomAttributeProvider member, bool inherit) where T : Attribute
        {
            try
            {
                return member.GetCustomAttributes(typeof(T), inherit).Cast<T>();
            }
            catch
            {
                return new T[0];
            }
        }

        /// <summary>
        /// If this member is a method, returns the full method name (name + params) otherwise the member name paskal splitted
        /// </summary>
        public static string GetNiceName(this MemberInfo member)
        {
            var method = member as MethodBase;
            string result;
            if (method != null)
            {
                result = method.GetFullName();
            }
            else
            {
                result = member.Name;
            }

            return result.ToTitleCase();
        }
    }
}