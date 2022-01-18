namespace ExtEvents
{
    using System;
    using System.Reflection;
    using UnityEngine;

    public partial class SerializedResponse
    {
        [SerializeField] internal string _methodName;

        public EfficientInvoker GetInvokable(Type declaringType, Type[] argumentTypes)
        {
            var method = GetMethod(declaringType, argumentTypes);
            return method == null ? null : EfficientInvoker.Create(method);
        }

        public MethodInfo GetMethod(Type declaringType, Type[] argumentTypes)
        {
            if (string.IsNullOrEmpty(_methodName))
                return null;

            return declaringType.GetMethod(_methodName, Flags, null, CallingConventions.Any, argumentTypes, null);
        }
    }
}