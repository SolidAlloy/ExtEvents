namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class ImplicitConversionsCache
    {
        private static readonly Dictionary<(Type from, Type to), bool> _implicitConversions = new Dictionary<(Type from, Type to), bool>();
        private static readonly Dictionary<(Type from, Type to), MethodInfo> _implicitConversionMethods = new Dictionary<(Type from, Type to), MethodInfo>();

        public static MethodInfo GetImplicitOperatorForTypes(Type fromType, Type toType)
        {
            return HaveImplicitConversion(fromType, toType) ? _implicitConversionMethods[(fromType, toType)] : null;
        }

        public static bool HaveImplicitConversion(Type fromType, Type toType)
        {
            var types = (fromType, toType);
            if (_implicitConversions.TryGetValue(types, out bool exists))
                return exists;

            var method = GetImplicitOperatorForTypesNoCache(fromType, toType);
            exists = method != null;
            _implicitConversions.Add(types, exists);

            if (exists)
                _implicitConversionMethods.Add(types, method);

            return exists;
        }

        private static MethodInfo GetImplicitOperatorForTypesNoCache(Type fromType, Type toType)
        {
            return fromType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(mi => mi.Name == "op_Implicit" && mi.ReturnType == toType)
                .FirstOrDefault(mi =>
                {
                    ParameterInfo pi = mi.GetParameters().FirstOrDefault();
                    return pi != null && pi.ParameterType == fromType;
                });
        }
    }
}