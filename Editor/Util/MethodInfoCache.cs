namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using JetBrains.Annotations;
    using UnityEngine;

    internal static class MethodInfoCache
    {
        private static readonly Dictionary<(Type declaringType, string methodName, Type[] argTypes), MethodInfo> _cache = new Dictionary<(Type declaringType, string methodName, Type[] argTypes), MethodInfo>();

        public static MethodInfo GetItem(Type type, string methodName, bool isStatic, Type[] argTypes)
        {
            if (_cache.TryGetValue((type, methodName, argTypes), out var value))
                return value;

            var flags = BindingFlags.Public | BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance | BindingFlags.Static);
            var item = type.GetMethod(methodName, flags, null, CallingConventions.Any, argTypes, null); // TODO: check if we need callingconventions.any
            _cache.Add((type, methodName, argTypes), item);
            return item;
        }
    }
}