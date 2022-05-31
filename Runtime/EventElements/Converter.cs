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

            public static Type EmitConverter(Type fromType, Type toType)
            {
                TypeBuilder typeBuilder = _moduleBuilder.DefineType(
                    $"{AssemblyName}.{fromType.Name}_{toType.Name}_Converter",
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

                var implicitOperator = fromType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(mi => mi.Name == "op_Implicit" && mi.ReturnType == toType)
                    .First(mi =>
                    {
                        ParameterInfo pi = mi.GetParameters().FirstOrDefault();
                        return pi != null && pi.ParameterType == fromType;
                    });

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