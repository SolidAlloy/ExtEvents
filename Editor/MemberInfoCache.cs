namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using JetBrains.Annotations;

    internal static class MemberInfoCache
    {
        private static readonly Dictionary<(Type declaringType, string memberName), MemberInfo> _cache = new Dictionary<(Type declaringType, string memberName), MemberInfo>();

        public static MemberInfo GetItem(Type type, string memberName, bool isStatic, MemberType memberType, Type[] argTypes, out MemberType newMemberType)
        {
            newMemberType = memberType;

            if (_cache.TryGetValue((type, memberName), out var value))
                return value;

            var item = GetItemDirect(type, memberName, isStatic, memberType, argTypes, out newMemberType);
            _cache.Add((type, memberName), item);
            return item;
        }

        private static MemberInfo GetItemDirect(Type type, string memberName, bool isStatic, MemberType memberType,
            Type[] argTypes, out MemberType newMemberType)
        {
            newMemberType = memberType;
            var flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance | BindingFlags.Static);

            if (memberType is MemberType.Method)
            {
                return type.GetMethod(memberName, flags, null, CallingConventions.Any, argTypes, null);
            }

            var returnType = argTypes[0];

            if (memberType is MemberType.Property)
            {
                var member = GetProperty(type, memberName, flags, returnType, out bool switchedMemberType);

                if (switchedMemberType)
                    newMemberType = MemberType.Field;

                return member;
            }

            if (memberType is MemberType.Field)
            {
                var member = GetField(type, memberName, flags, returnType, out bool switchedMemberType);

                if (switchedMemberType)
                    newMemberType = MemberType.Property;

                return member;
            }

            throw new NotImplementedException();
        }

        private static MemberInfo GetProperty(Type type, string memberName, BindingFlags flags, Type returnType, out bool switchedMemberType)
        {
            switchedMemberType = false;
            MemberInfo member = GetPropertyImpl(type, memberName, flags, returnType);

            if (member != null)
                return member;

            member = GetFieldImpl(type, memberName, flags, returnType);

            if (member == null)
                return null;

            switchedMemberType = true;
            return member;
        }

        private static MemberInfo GetField(Type type, string memberName, BindingFlags flags, Type returnType, out bool switchedMemberType)
        {
            switchedMemberType = false;
            MemberInfo member = GetFieldImpl(type, memberName, flags, returnType);

            if (member != null)
                return member;

            member = GetPropertyImpl(type, memberName, flags, returnType);

            if (member == null)
                return null;

            switchedMemberType = true;
            return member;
        }

        private static PropertyInfo GetPropertyImpl([NotNull] Type declaringType, string name, BindingFlags flags, Type returnType)
        {
            return declaringType.GetProperty(name, flags, null, returnType, Type.EmptyTypes, null);
        }

        private static FieldInfo GetFieldImpl([NotNull] Type declaringType, string name, BindingFlags flags, Type returnType)
        {
            var fieldInfo = declaringType.GetField(name, flags);
            return (fieldInfo != null && fieldInfo.FieldType == returnType) ? fieldInfo : null;
        }
    }
}