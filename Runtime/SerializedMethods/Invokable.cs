namespace ExtEvents
{
    using System.Reflection;

    public class Invokable
    {
        private readonly FieldInfo _fieldInfo;
        private readonly PropertyInfo _propertyInfo;
        private readonly MemberType _memberType;

        private readonly EfficientInvoker _efficientInvoker;

        public Invokable(MemberInfo memberInfo, MemberType memberType)
        {
            _efficientInvoker = EfficientInvoker.ForMethod((MethodInfo) memberInfo);

            // switch (memberType)
            // {
            //     case MemberType.Field:
            //         _fieldInfo = (FieldInfo) memberInfo;
            //         break;
            //     case MemberType.Property:
            //         _propertyInfo = (PropertyInfo) memberInfo;
            //         break;
            //     case MemberType.Method:
            //         _efficientInvoker = EfficientInvoker.ForMethod((MethodInfo) memberInfo);
            //         break;
            // }
            //
            // _memberType = memberType;
        }

        public void Invoke(object obj, object[] args)
        {
            _efficientInvoker.Invoke(obj, args);

            // switch (_memberType)
            // {
            //     case MemberType.Field:
            //         _fieldInfo.SetValue(obj, args[0]);
            //         break;
            //     case MemberType.Property:
            //         _propertyInfo.SetValue(obj, args[0]);
            //         break;
            //     case MemberType.Method:
            //         _efficientInvoker.Invoke(obj, args);
            //         break;
            // }
        }
    }

    public enum MemberType { Field, Property, Method }
}