namespace ExtEvents
{
    using System;
    using System.Reflection;
    using TypeReferences;
    using UnityEngine;

    public enum MemberType { Field, Property, Method }

    [Serializable]
    public class SerializedMember
    {
        [SerializeField] internal string _memberName;
        [SerializeField] internal MemberType _memberType;

        public EfficientInvoker GetInvokable(Type declaringType, BindingFlags bindingFlags, Type[] argumentTypes)
        {
            MemberInfo member = _memberType switch
            {
                MemberType.Field => GetField(declaringType, bindingFlags, argumentTypes[0]),
                MemberType.Property => GetProperty(declaringType, bindingFlags, argumentTypes[0]),
                MemberType.Method => GetMethod(declaringType, bindingFlags, argumentTypes),
                _ => throw new NotImplementedException()
            };

            return member == null ? null : EfficientInvoker.Create(member);
        }

        public MethodInfo GetMethod(Type declaringType, BindingFlags bindingFlags, Type[] argumentTypes)
        {
            if (string.IsNullOrEmpty(_memberName))
                return null;

            return declaringType.GetMethod(_memberName, bindingFlags, null, CallingConventions.Any, argumentTypes, null);
        }

        private FieldInfo GetField(Type declaringType, BindingFlags bindingFlags, Type returnType)
        {
            if (string.IsNullOrEmpty(_memberName))
                return null;

            var fieldInfo = declaringType.GetField(_memberName, bindingFlags);
            return fieldInfo?.FieldType == returnType ? fieldInfo : null;
        }

        private PropertyInfo GetProperty(Type declaringType, BindingFlags bindingFlags, Type returnType)
        {
            if (string.IsNullOrEmpty(_memberName))
                return null;

            return declaringType.GetProperty(_memberName, bindingFlags, null, returnType, Type.EmptyTypes, null);
        }
    }
}