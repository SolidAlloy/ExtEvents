namespace ExtEvents
{
    using System;
    using System.Reflection;
    using TypeReferences;
    using UnityEngine;

    [Serializable]
    public class SerializedStaticMethod
    {
        [SerializeField] private TypeReference _type;
        [SerializeField] private string _methodName;
        [SerializeField] private TypeReference[] _argumentTypes;

        private MethodInfo _method;

        public MethodInfo Method => _method ??= GetMethod();

        private MethodInfo GetMethod()
        {
            if (_type.Type == null || string.IsNullOrEmpty(_methodName))
                return null;

            var argumentTypes = GetArgumentTypes();

            if (AnyTypeNull(argumentTypes))
                return null;

            return GetMethodInfo(_type.Type, _methodName, argumentTypes);
        }

        protected virtual MethodInfo GetMethodInfo(Type type, string methodName, Type[] argumentTypes)
        {
            return type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, argumentTypes, null);
        }

        private bool AnyTypeNull(Type[] types)
        {
            foreach (Type type in types)
            {
                if (type == null)
                    return false;
            }

            return true;
        }

        private Type[] GetArgumentTypes()
        {
            var types = new Type[_argumentTypes.Length];

            for (int i = 0; i < _argumentTypes.Length; i++)
            {
                types[i] = _argumentTypes[i].Type;
            }

            return types;
        }
    }
}