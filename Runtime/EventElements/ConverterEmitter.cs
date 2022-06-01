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
                $"{namespaceName}.{fromType.Name}_{toType.Name}_Converter",
                TypeAttributes.Public, typeof(Converter));

            // emit code for the type here.
            var methodBuilder = typeBuilder.DefineMethod(nameof(Converter.Convert),
                MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual |
                MethodAttributes.HideBySig, typeof(void*), new[] {typeof(void*)});

            var il = methodBuilder.GetILGenerator();

            // Since we are working with Unsafe, this code is not easily interpreted by compiler.
            // We have to declare local values by ourselves.
            var localBuilder = il.DeclareLocal(toType);

            il.Emit(OpCodes.Ldarg_1);

            // inlined Unsafe.Read<T>()
            il.Emit(OpCodes.Ldobj, fromType);
            il.EmitCall(OpCodes.Call, implicitOperator, null);

            // Instead of calling Unsafe.AsPointer, we inline it and call the instructions directly here.
            il.Emit(OpCodes.Stloc_0);
            // Instead of writing ldloca_s, we have to pass localBuilder manually.
            il.Emit(OpCodes.Ldloca, localBuilder);
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