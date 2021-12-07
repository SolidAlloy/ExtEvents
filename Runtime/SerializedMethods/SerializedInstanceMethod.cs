namespace ExtEvents
{
    using System;
    using System.Reflection;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Serializable]
    public class SerializedInstanceMethod : SerializedStaticMethod
    {
        [SerializeField] private Object _target;

        public Object Target => _target;

        protected override MethodInfo GetMethodInfo(Type type, string methodName, Type[] argumentTypes)
        {
            return type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, argumentTypes, null);
        }
    }
}