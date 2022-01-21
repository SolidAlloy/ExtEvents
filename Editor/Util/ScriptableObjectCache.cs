namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Configuration.Assemblies;
    using System.Globalization;
    using System.Reflection;
    using System.Reflection.Emit;
    using UnityEngine.Assertions;

    internal static class ScriptableObjectCache
    {
        private const string AssemblyName = "ExtEvents.Editor.DynamicAssembly";

        private static readonly AssemblyBuilder _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
            new AssemblyName(AssemblyName)
            {
                CultureInfo = CultureInfo.InvariantCulture,
                Flags = AssemblyNameFlags.None,
                ProcessorArchitecture = ProcessorArchitecture.MSIL,
                VersionCompatibility = AssemblyVersionCompatibility.SameDomain
            }, AssemblyBuilderAccess.Run);

        private static readonly ModuleBuilder _moduleBuilder = _assemblyBuilder.DefineDynamicModule(AssemblyName, true);

        private static readonly Dictionary<Type, Type> _classDict = new Dictionary<Type, Type>();

        public static Type GetClass(Type valueType)
        {
            if (_classDict.TryGetValue(valueType, out Type classType))
                return classType;

            classType = CreateClass(valueType);
            _classDict[valueType] = classType;
            return classType;
        }

        private static Type CreateClass(Type valueType)
        {
            TypeBuilder typeBuilder = _moduleBuilder.DefineType(
                $"{AssemblyName}.{GetClassName(valueType)}",
                TypeAttributes.NotPublic,
                typeof(DeserializedValueHolder<>).MakeGenericType(valueType));

            Type type = typeBuilder.CreateType();
            return type;
        }

        private static string GetClassName(Type fieldType)
        {
            string fullTypeName = fieldType.FullName;

            Assert.IsNotNull(fullTypeName);

            string classSafeTypeName = fullTypeName
                .Replace('.', '_')
                .Replace('`', '_');

            return classSafeTypeName.CapitalizeFirstChar();
        }

        private static string CapitalizeFirstChar(this string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            char firstChar = input[0];

            if (char.IsUpper(firstChar))
                return input;

            var chars = input.ToCharArray();
            chars[0] = char.ToUpper(firstChar);
            return new string(chars);
        }
    }
}