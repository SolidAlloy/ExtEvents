//-----------------------------------------------------------------------
// <copyright file="FormatterUtilities.cs" company="Sirenix IVS">
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
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Serialization;
    using Utilities;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Provides an array of utility methods which are commonly used by serialization formatters.
    /// </summary>

#if UNITY_EDITOR

    [InitializeOnLoad]
#endif
    public static class FormatterUtilities
    {
        private static readonly DoubleLookupDictionary<ISerializationPolicy, Type, MemberInfo[]> MemberArrayCache = new DoubleLookupDictionary<ISerializationPolicy, Type, MemberInfo[]>();
        private static readonly DoubleLookupDictionary<ISerializationPolicy, Type, Dictionary<string, MemberInfo>> MemberMapCache = new DoubleLookupDictionary<ISerializationPolicy, Type, Dictionary<string, MemberInfo>>();
        private static readonly object LOCK = new object();

        private static readonly HashSet<Type> PrimitiveArrayTypes = new HashSet<Type>(FastTypeComparer.Instance)
        {
            typeof(char),
            typeof(sbyte),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(byte),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
            typeof(decimal),
            typeof(bool),
            typeof(float),
            typeof(double),
            typeof(Guid)
        };

        static FormatterUtilities()
        {
            // The required field is missing in Unity builds
#if UNITY_EDITOR
            FieldInfo unityObjectRuntimeErrorStringField = typeof(Object).GetField("m_UnityRuntimeErrorString", Flags.InstanceAnyVisibility);

            if (unityObjectRuntimeErrorStringField == null)
            {
                Debug.LogWarning("A change in Unity has hindered the Serialization system's ability to create proper fake Unity null values; the UnityEngine.Object.m_UnityRuntimeErrorString field has been renamed or removed.");
            }
#endif
        }

        /// <summary>
        /// Gets a map of all serializable members on the given type. This will also properly map names extracted from <see cref="UnityEngine.Serialization.FormerlySerializedAsAttribute"/> and <see cref="PreviouslySerializedAsAttribute"/> to their corresponding members.
        /// </summary>
        /// <param name="type">The type to get a map for.</param>
        /// <param name="policy">The serialization policy to use. If null, <see cref="SerializationPolicies.Strict"/> is used.</param>
        /// <returns>A map of all serializable members on the given type.</returns>
        public static Dictionary<string, MemberInfo> GetSerializableMembersMap(Type type, ISerializationPolicy policy)
        {
            Dictionary<string, MemberInfo> result;

            if (policy == null)
            {
                policy = SerializationPolicies.Strict;
            }

            lock (LOCK)
            {
                if (MemberMapCache.TryGetInnerValue(policy, type, out result) == false)
                {
                    result = FindSerializableMembersMap(type, policy);
                    MemberMapCache.AddInner(policy, type, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets an array of all serializable members on the given type.
        /// </summary>
        /// <param name="type">The type to get serializable members for.</param>
        /// <param name="policy">The serialization policy to use. If null, <see cref="SerializationPolicies.Strict"/> is used.</param>
        /// <returns>An array of all serializable members on the given type.</returns>
        public static MemberInfo[] GetSerializableMembers(Type type, ISerializationPolicy policy)
        {
            MemberInfo[] result;

            if (policy == null)
            {
                policy = SerializationPolicies.Strict;
            }

            lock (LOCK)
            {
                if (MemberArrayCache.TryGetInnerValue(policy, type, out result) == false)
                {
                    List<MemberInfo> list = new List<MemberInfo>();
                    FindSerializableMembers(type, list, policy);
                    result = list.ToArray();
                    MemberArrayCache.AddInner(policy, type, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether a given type is a primitive type to the serialization system.
        /// <para />
        /// The following criteria are checked: type.IsPrimitive or type.IsEnum, or type is a <see cref="decimal"/>, <see cref="string"/> or <see cref="Guid"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if the given type is a primitive type; otherwise, <c>false</c>.</returns>
        public static bool IsPrimitiveType(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(decimal)
                || type == typeof(string)
                || type == typeof(Guid);
        }

        /// <summary>
        /// Determines whether a given type is a primitive array type. Namely, arrays with primitive array types as elements are primitive arrays.
        /// <para />
        /// The following types are primitive array types: <see cref="char"/>, <see cref="sbyte"/>, <see cref="short"/>, <see cref="int"/>, <see cref="long"/>, <see cref="byte"/>, <see cref="ushort"/>, <see cref="uint"/>, <see cref="ulong"/>, <see cref="decimal"/>, <see cref="bool"/>, <see cref="float"/>, <see cref="double"/> and <see cref="Guid"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if the given type is a primitive array type; otherwise, <c>false</c>.</returns>
        public static bool IsPrimitiveArrayType(Type type)
        {
            return PrimitiveArrayTypes.Contains(type);
        }

        /// <summary>
        /// Gets the type contained in the given <see cref="MemberInfo"/>. Currently only <see cref="FieldInfo"/> and <see cref="PropertyInfo"/> is supported.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> to get the contained type of.</param>
        /// <returns>The type contained in the given <see cref="MemberInfo"/>.</returns>
        /// <exception cref="System.ArgumentException">Can't get the contained type of the given <see cref="MemberInfo"/> type.</exception>
        public static Type GetContainedType(MemberInfo member)
        {
            if (member is FieldInfo)
            {
                return (member as FieldInfo).FieldType;
            }

            if (member is PropertyInfo)
            {
                return (member as PropertyInfo).PropertyType;
            }

            throw new ArgumentException("Can't get the contained type of a " + member.GetType().Name);
        }

        /// <summary>
        /// Gets the value contained in a given <see cref="MemberInfo"/>. Currently only <see cref="FieldInfo"/> and <see cref="PropertyInfo"/> is supported.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> to get the value of.</param>
        /// <param name="obj">The instance to get the value from.</param>
        /// <returns>The value contained in the given <see cref="MemberInfo"/>.</returns>
        /// <exception cref="System.ArgumentException">Can't get the value of the given <see cref="MemberInfo"/> type.</exception>
        public static object GetMemberValue(MemberInfo member, object obj)
        {
            if (member is FieldInfo)
            {
                return (member as FieldInfo).GetValue(obj);
            }

            if (member is PropertyInfo)
            {
                return (member as PropertyInfo).GetGetMethod(true).Invoke(obj, null);
            }

            throw new ArgumentException("Can't get the value of a " + member.GetType().Name);
        }

        /// <summary>
        /// Sets the value of a given MemberInfo. Currently only <see cref="FieldInfo"/> and <see cref="PropertyInfo"/> is supported.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> to set the value of.</param>
        /// <param name="obj">The object to set the value on.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="System.ArgumentException">
        /// Property has no setter
        /// or
        /// Can't set the value of the given <see cref="MemberInfo"/> type.
        /// </exception>
        public static void SetMemberValue(MemberInfo member, object obj, object value)
        {
            if (member is FieldInfo)
            {
                (member as FieldInfo).SetValue(obj, value);
            }
            else if (member is PropertyInfo)
            {
                var method = (member as PropertyInfo).GetSetMethod(true);

                if (method != null)
                {
                    method.Invoke(obj, new[] { value });
                }
                else
                {
                    throw new ArgumentException("Property " + member.Name + " has no setter");
                }
            }
            else
            {
                throw new ArgumentException("Can't set the value of a " + member.GetType().Name);
            }
        }

        private static Dictionary<string, MemberInfo> FindSerializableMembersMap(Type type, ISerializationPolicy policy)
        {
            var map = GetSerializableMembers(type, policy).ToDictionary(n => n.Name, n => n);

            foreach (var member in map.Values.ToList())
            {
                var serializedAsAttributes = member.GetAttributes<FormerlySerializedAsAttribute>();

                foreach (var attr in serializedAsAttributes)
                {
                    if (map.ContainsKey(attr.oldName) == false)
                    {
                        map.Add(attr.oldName, member);
                    }
                }
            }

            return map;
        }

        private static void FindSerializableMembers(Type type, List<MemberInfo> members, ISerializationPolicy policy)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            if (type.BaseType != typeof(object) && type.BaseType != null)
            {
                FindSerializableMembers(type.BaseType, members, policy);
            }

            foreach (var member in type.GetMembers(Flags).Where(n => n is FieldInfo || n is PropertyInfo))
            {
                if (policy.ShouldSerializeMember(member))
                {
                    bool nameAlreadyExists = members.Any(n => n.Name == member.Name);

                    if (MemberIsPrivate(member) && nameAlreadyExists)
                    {
                        members.Add(GetPrivateMemberAlias(member));
                    }
                    else if (nameAlreadyExists)
                    {
                        members.Add(GetPrivateMemberAlias(member));
                    }
                    else
                    {
                        members.Add(member);
                    }
                }
            }
        }

        /// <summary>
        /// Gets an aliased version of a member, with the declaring type name included in the member name, so that there are no conflicts with private fields and properties with the same name in different classes in the same inheritance hierarchy.
        /// </summary>
        public static MemberInfo GetPrivateMemberAlias(MemberInfo member, string prefixString = null, string separatorString = null)
        {
            if (member is FieldInfo)
            {
                if (separatorString != null)
                {
                    return new MemberAliasFieldInfo(member as FieldInfo, prefixString ?? member.DeclaringType.Name, separatorString);
                }

                return new MemberAliasFieldInfo(member as FieldInfo, prefixString ?? member.DeclaringType.Name);
            }

            if (member is PropertyInfo)
            {
                if (separatorString != null)
                {
                    return new MemberAliasPropertyInfo(member as PropertyInfo, prefixString ?? member.DeclaringType.Name, separatorString);
                }

                return new MemberAliasPropertyInfo(member as PropertyInfo, prefixString ?? member.DeclaringType.Name);
            }

            if (member is MethodInfo)
            {
                if (separatorString != null)
                {
                    return new MemberAliasMethodInfo(member as MethodInfo, prefixString ?? member.DeclaringType.Name, separatorString);
                }

                return new MemberAliasMethodInfo(member as MethodInfo, prefixString ?? member.DeclaringType.Name);
            }

            throw new NotImplementedException();
        }

        private static bool MemberIsPrivate(MemberInfo member)
        {
            if (member is FieldInfo)
            {
                return (member as FieldInfo).IsPrivate;
            }

            if (member is PropertyInfo)
            {
                var prop = member as PropertyInfo;
                var getter = prop.GetGetMethod();
                var setter = prop.GetSetMethod();

                return getter != null && setter != null && getter.IsPrivate && setter.IsPrivate;
            }

            if (member is MethodInfo)
            {
                return (member as MethodInfo).IsPrivate;
            }

            throw new NotImplementedException();
        }
    }
}