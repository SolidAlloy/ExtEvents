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

        private FieldInfo GetField(Type declaringType, Type returnType)
        {
            if (string.IsNullOrEmpty(_memberName))
                return null;

            var fieldInfo = declaringType.GetField(_memberName, Flags);
            return fieldInfo?.FieldType == returnType ? fieldInfo : null;
        }

        private PropertyInfo GetProperty(Type declaringType, Type returnType)
        {
            if (string.IsNullOrEmpty(_memberName))
                return null;

            return declaringType.GetProperty(_memberName, Flags, null, returnType, Type.EmptyTypes, null);
        }
    }
}