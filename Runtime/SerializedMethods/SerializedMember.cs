namespace ExtEvents
{
    using System;
    using System.Reflection;
    using TypeReferences;
    using UnityEngine;

    [Serializable]
    public class SerializedMember
    {
        [SerializeField, TypeOptions(IncludeAdditionalAssemblies = new []{ "Assembly-CSharp" })] private TypeReference _type; // TODO: remove includeAdditionalAssemblies
        [SerializeField] private string _memberName;
        [SerializeField] private TypeReference[] _argumentTypeReferences;
        [SerializeField] private MemberType _memberType;

        public Invokable GetInvokable(BindingFlags bindingFlags)
        {
            switch (_memberType)
            {
                case MemberType.Field:
                    var field = GetField(bindingFlags);
                    return new Invokable(field, _memberType);
                case MemberType.Property:
                    var property = GetProperty(bindingFlags);
                    return new Invokable(property, _memberType);
                case MemberType.Method:
                    var method = GetMethod(bindingFlags);
                    return new Invokable(method, _memberType);
                default:
                    throw new NotImplementedException();
            }
        }

        public MethodInfo GetMethod(BindingFlags bindingFlags)
        {
            if (_type.Type == null || string.IsNullOrEmpty(_memberName))
                return null;

            var argumentTypes = GetArgumentTypes();

            if (AnyTypeNull(argumentTypes))
                return null;

            return _type.Type.GetMethod(_memberName, bindingFlags, null, CallingConventions.Any, argumentTypes, null);
        }

        private FieldInfo GetField(BindingFlags bindingFlags)
        {
            if (_type.Type == null || string.IsNullOrEmpty(_memberName))
                return null;

            return _type.Type.GetField(_memberName, bindingFlags);
        }

        private PropertyInfo GetProperty(BindingFlags bindingFlags)
        {
            if (_type.Type == null || string.IsNullOrEmpty(_memberName))
                return null;

            return _type.Type.GetProperty(_memberName, bindingFlags);
        }

        public bool AnyTypeNull(Type[] types)
        {
            foreach (Type type in types)
            {
                if (type == null)
                    return true;
            }

            return false;
        }

        private Type[] _argumentTypes;
        public Type[] ArgumentTypes => _argumentTypes ??= GetArgumentTypes();

        private Type[] GetArgumentTypes()
        {
            var types = new Type[_argumentTypeReferences.Length];

            for (int i = 0; i < _argumentTypeReferences.Length; i++)
            {
                types[i] = _argumentTypeReferences[i].Type;
            }

            return types;
        }
    }
}