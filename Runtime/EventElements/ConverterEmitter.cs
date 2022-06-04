#if (UNITY_EDITOR || UNITY_STANDALONE) && !ENABLE_IL2CPP
#define CAN_EMIT
#endif

#if CAN_EMIT
namespace ExtEvents
{
    using System;
    using System.Configuration.Assemblies;
    using System.Globalization;
    using System.Reflection;
    using System.Reflection.Emit;
    using SolidUtilities;

    public static class ConverterEmitter
    {
        private const string AssemblyName = "ExtEvents.Editor.EmittedConverters";

        private static readonly AssemblyBuilder _assemblyBuilder =
#if NET_STANDARD
            AssemblyBuilder
#else
                    AppDomain.CurrentDomain
#endif
                .DefineDynamicAssembly(
                    new AssemblyName(AssemblyName)
                    {
                        CultureInfo = CultureInfo.InvariantCulture,
                        Flags = AssemblyNameFlags.None,
                        ProcessorArchitecture = ProcessorArchitecture.MSIL,
                        VersionCompatibility = AssemblyVersionCompatibility.SameDomain
                    }, AssemblyBuilderAccess.Run);

        private static readonly ModuleBuilder _moduleBuilder = _assemblyBuilder.DefineDynamicModule(AssemblyName
#if !NET_STANDARD
                , false
#endif
        );

        public static Type EmitConverter(Type fromType, Type toType, MethodInfo implicitOperator, ModuleBuilder moduleBuilder, string namespaceName)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                $"{namespaceName}.{fromType.FullName.MakeClassFriendly()}_{toType.FullName.MakeClassFriendly()}_Converter",
                TypeAttributes.Public, typeof(Converter));

            var fieldBuilder = typeBuilder.DefineField("_arg", toType, FieldAttributes.Private);

            // emit code for the type here.
            var methodBuilder = typeBuilder.DefineMethod(nameof(Converter.Convert),
                MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual |
                MethodAttributes.HideBySig, typeof(void*), new[] { typeof(void*) });

            var il = methodBuilder.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0); // this
            il.Emit(OpCodes.Ldarg_1); // from-type pointer

            il.Emit(OpCodes.Ldobj, fromType); // inlined Unsafe.Read<T>()
            il.EmitCall(OpCodes.Call, implicitOperator, null);

            // save to a field. This is necessary so that when we get a pointer to the object, it isn't destroyed.
            il.Emit(OpCodes.Stfld, fieldBuilder);

            il.Emit(OpCodes.Ldarg_0); // this
            il.Emit(OpCodes.Ldflda, fieldBuilder); // ref _arg

            il.Emit(OpCodes.Conv_U); // inlined Unsafe.AsPointer()
            il.Emit(OpCodes.Ret);
            Type type = typeBuilder.CreateType();
            return type;
        }

        public static Type EmitConverter(Type fromType, Type toType, MethodInfo implicitOperator)
        {
            return EmitConverter(fromType, toType, implicitOperator, _moduleBuilder, AssemblyName);
        }
    }
}
#endif