namespace ExtEvents
{
    using System;
    using System.Reflection;
    using UnityEngine;

    public partial class SerializedResponse
    {
        [SerializeField] internal string _memberName;
        [SerializeField] internal MemberType _memberType;

        public EfficientInvoker GetInvokable(Type declaringType, Type[] argumentTypes)
        {
            MemberInfo member = _memberType switch
            {
                MemberType.Field => GetField(declaringType, argumentTypes[0]),
                MemberType.Property => GetProperty(declaringType, argumentTypes[0]),
                MemberType.Method => GetMethod(declaringType, argumentTypes),
                _ => throw new NotImplementedException()
            };

            return member == null ? null : EfficientInvoker.Create(member);
        }

        public MethodInfo GetMethod(Type declaringType, Type[] argumentTypes)
        {
            if (string.IsNullOrEmpty(_memberName))
                return null;

            return declaringType.GetMethod(_memberName, Flags, null, CallingConventions.Any, argumentTypes, null);
        }

        private MemberInfo GetField(Type declaringType, Type returnType)
        {
            if (string.IsNullOrEmpty(_memberName))
                return null;

            var fieldInfo = GetFieldImpl(declaringType);

            if (fieldInfo != null)
                return fieldInfo.FieldType == returnType ? fieldInfo : null;

            // Check if the field was changed to property without changing its name.
            var property = GetPropertyImpl(declaringType, returnType);

            if (property != null)
            {
                // This won't be saved in play mode, but we can't do anything with it. This is a serialized POCO, and it has no way to know which UnityEngine Object it belongs to.
                // However, the type will be changed to property when building the project, so it won't affect performance in build.
                _memberType = MemberType.Property;
            }

            return property;
        }

        private MemberInfo GetProperty(Type declaringType, Type returnType)
        {
            if (string.IsNullOrEmpty(_memberName))
                return null;

            var property = GetPropertyImpl(declaringType, returnType);

            if (property != null)
                return property;

            var fieldInfo = GetFieldImpl(declaringType);

            if (fieldInfo != null)
            {
                _memberType = MemberType.Field;
                return fieldInfo.FieldType == returnType ? fieldInfo : null;
            }

            return null;
        }

        private FieldInfo GetFieldImpl(Type declaringType)
        {
            return declaringType.GetField(_memberName, Flags);
        }

        private MemberInfo GetPropertyImpl(Type declaringType, Type returnType)
        {
            return declaringType.GetProperty(_memberName, Flags, null, returnType, Type.EmptyTypes, null);
        }
    }
}