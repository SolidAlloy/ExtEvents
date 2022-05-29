namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Configuration.Assemblies;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Security;
    using System.Security.Permissions;
    using UnityEditor;
    using UnityEngine;

    public abstract partial class Converter
    {
#if UNITY_EDITOR
        static Converter()
        {
            var types = TypeCache.GetTypesDerivedFrom<Converter>();

            // Find all inheritors of the Converter<TFrom, TTo> class and add them to ConverterTypes.
            foreach (Type type in types)
            {
                if (type.IsGenericType || type.IsAbstract)
                    continue;

                var baseType = type.BaseType;

                // ReSharper disable once PossibleNullReferenceException
                if (!baseType.IsGenericType)
                    continue;

                var genericArgs = baseType.GetGenericArguments();

                if (genericArgs.Length != 2)
                    continue;

                var fromToTypes = (genericArgs[0], genericArgs[1]);

                if (ConverterTypes.TryGetValue(fromToTypes, out var converterType))
                {
                    Debug.LogWarning($"Two custom converters for the same pair of types: {converterType} and {type}");
                    continue;
                }

                ConverterTypes.Add(fromToTypes, type);
            }
        }
#endif

        private static readonly Dictionary<(Type from, Type to), Converter> _createdConverters =
            new Dictionary<(Type from, Type to), Converter>();

        public static Converter GetForTypes(Type from, Type to)
        {
            var types = (from, to);

            if (_createdConverters.TryGetValue(types, out var converter))
                return converter;

            if (!ConverterTypes.TryGetValue(types, out var converterType))
            {
                // check if the from type has implicit conversion operator to to-type, then emit the type and add it to the dict.
                // implement emit in editor, throw error in AOT builds.
                converterType = ConverterEmitter.EmitConverter(from, to);
                ConverterTypes.Add(types, converterType);
            }

            converter = (Converter) Activator.CreateInstance(converterType);
            _createdConverters.Add(types, converter);
            return converter;
        }

        public abstract unsafe void* Convert(void* sourceTypePointer);

        private static bool TypesCanBeConverted(Type fromType, Type toType)
        {
            return fromType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(mi => mi.Name == "op_Implicit" && mi.ReturnType == toType)
                .Any(mi =>
                {
                    ParameterInfo pi = mi.GetParameters().FirstOrDefault();
                    return pi != null && pi.ParameterType == fromType;
                });
        }

        private static class ConverterEmitter
        {
            private const string AssemblyName = "ExtEvents.Editor.EmittedConverters";

            private static readonly AssemblyBuilder _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(AssemblyName)
                {
                    CultureInfo = CultureInfo.InvariantCulture,
                    Flags = AssemblyNameFlags.None,
                    ProcessorArchitecture = ProcessorArchitecture.MSIL,
                    VersionCompatibility = AssemblyVersionCompatibility.SameDomain
                }, AssemblyBuilderAccess.Run);
                // }, AssemblyBuilderAccess.RunAndSave, "Assets/");

            private static readonly ModuleBuilder _moduleBuilder = _assemblyBuilder.DefineDynamicModule(AssemblyName, true);

            public static Type EmitConverter(Type fromType, Type toType)
            {
                var unverifiableCon = typeof(UnverifiableCodeAttribute).GetConstructor(Type.EmptyTypes);
                _moduleBuilder.SetCustomAttribute(new CustomAttributeBuilder(unverifiableCon, Array.Empty<object>()));

                var securityCon =
                    typeof(SecurityPermissionAttribute).GetConstructor(new Type[] {typeof(SecurityAction)});

                var skipVerificationProp =
                    typeof(SecurityPermissionAttribute).GetProperty(
                        nameof(SecurityPermissionAttribute.SkipVerification), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                var unmanagedProp = typeof(SecurityPermissionAttribute).GetProperty(
                    nameof(SecurityPermissionAttribute.UnmanagedCode),
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                _assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(securityCon, new object[] { SecurityAction.PermitOnly }, new[] { skipVerificationProp, unmanagedProp }, new object[]{ true, true }));

                TypeBuilder typeBuilder = _moduleBuilder.DefineType(
                    $"{AssemblyName}.{fromType.Name}_{toType.Name}_Converter",
                    TypeAttributes.Public, typeof(Converter));

                // emit code for the type here.
                var methodBuilder = typeBuilder.DefineMethod(nameof(Converter.Convert),
                    MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual |
                    MethodAttributes.HideBySig, typeof(void*), new[] {typeof(void*)});

                var il = methodBuilder.GetILGenerator();

                il.Emit(OpCodes.Ldarg_1);

                var unsafeReadMethodBase = typeof(Unsafe).GetMethods(BindingFlags.Static | BindingFlags.Public).First(method => method.Name == nameof(Unsafe.Read));
                var unsafeReadMethod = unsafeReadMethodBase.MakeGenericMethod(fromType);
                il.EmitCall(OpCodes.Call, unsafeReadMethod, null);

                var implicitOperator = fromType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(mi => mi.Name == "op_Implicit" && mi.ReturnType == toType)
                    .First(mi =>
                    {
                        ParameterInfo pi = mi.GetParameters().FirstOrDefault();
                        return pi != null && pi.ParameterType == fromType;
                    });
                il.EmitCall(OpCodes.Call, implicitOperator, null);
                il.Emit(OpCodes.Stloc_0);

                il.Emit(OpCodes.Ldloca_S);

                var unsafePointerMethodBase = typeof(Unsafe).GetMethods(BindingFlags.Static | BindingFlags.Public).First(method => method.Name == nameof(Unsafe.AsPointer));
                var unsafePointerMethod = unsafePointerMethodBase.MakeGenericMethod(toType); // MakeByRef?
                il.EmitCall(OpCodes.Call, unsafePointerMethod, null);
                il.Emit(OpCodes.Stloc_1);
                il.Emit(OpCodes.Br_S);

                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Ret);

                // IL_0001: ldarg.1      // from
                // IL_0002: call         !!0/*valuetype [GenericScriptableArchitecture]GenericScriptableArchitecture.ClampedInt*/ [System.Runtime.CompilerServices.Unsafe]System.Runtime.CompilerServices.Unsafe::Read<valuetype [GenericScriptableArchitecture]GenericScriptableArchitecture.ClampedInt>(void*)
                //     IL_0007: call         int32 [GenericScriptableArchitecture]GenericScriptableArchitecture.ClampedInt::op_Implicit(valuetype [GenericScriptableArchitecture]GenericScriptableArchitecture.ClampedInt)
                // IL_000c: stloc.0      // arg
                //
                // // [100 13 - 100 46]
                // IL_000d: ldloca.s     arg
                // IL_000f: call         void* [System.Runtime.CompilerServices.Unsafe]System.Runtime.CompilerServices.Unsafe::AsPointer<int32>(!!0/*int32*/&)
                // IL_0014: stloc.1      // V_1
                // IL_0015: br.s         IL_0017
                //
                // // [101 9 - 101 10]
                // IL_0017: ldloc.1      // V_1
                // IL_0018: ret

                Type type = typeBuilder.CreateType();
                // _assemblyBuilder.Save("test.dll");
                return type;
            }
        }
    }

    public abstract class Converter<TFrom, TTo> : Converter
    {
        public override unsafe void* Convert(void* sourceTypePointer)
        {
            TTo arg = Convert(Unsafe.Read<TFrom>(sourceTypePointer));
            return Unsafe.AsPointer(ref arg);
        }

        protected abstract TTo Convert(TFrom from);
    }

    // public class ExampleConverter : Converter<float, int>
    // {
    //     protected override int Convert(float from)
    //     {
    //         return (int) from;
    //     }
    // }
}