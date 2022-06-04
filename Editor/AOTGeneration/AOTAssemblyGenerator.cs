namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Configuration.Assemblies;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using OdinSerializer;
    using OdinSerializer.Editor;
    using SolidUtilities;
    using SolidUtilities.Editor;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assertions;

    public static class AOTAssemblyGenerator
    {
        private const string FolderPath = PackageSettings.PluginsPath + "/AOT Generation";
        private const string AssemblyName = "z_ExtEvents_AOTGeneration";

        public static void GenerateCreateMethods()
        {
            string dllName = $"{AssemblyName}.dll";
            string assemblyPath = $"{FolderPath}/{dllName}";
            bool folderExists = Directory.Exists(FolderPath);

            using var _ = AssetDatabaseHelper.DisabledScope();

            if (!folderExists)
            {
                Directory.CreateDirectory(FolderPath);
                CreateLinkXml(FolderPath);
            }

            CreateAssembly(dllName, FolderPath);

            if (folderExists)
            {
                AssetDatabase.ImportAsset(assemblyPath, ImportAssetOptions.ForceUpdate);
            }
            else
            {
                AssemblyGeneration.ImportAssemblyAsset(assemblyPath, AssetDatabaseHelper.GetUniqueGUID());
            }
        }

        public static void DeleteGeneratedFolder()
        {
            if (Directory.Exists(FolderPath))
                AssetDatabase.DeleteAsset(FolderPath);
        }

        private static void CreateLinkXml(string folderPath)
        {
            string linkXmlPath = $"{folderPath}/link.xml";
            File.WriteAllText(linkXmlPath, $"<linker>\n    <assembly fullname=\"{AssemblyName}\" preserve=\"all\"/>\n</linker>");
            AssetDatabase.ImportAsset(linkXmlPath);
        }

        private static void CreateAssembly(string dllName, string folderPath)
        {
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(AssemblyName)
                {
                    CultureInfo = CultureInfo.InvariantCulture,
                    Flags = AssemblyNameFlags.None,
                    ProcessorArchitecture = ProcessorArchitecture.MSIL,
                    VersionCompatibility = AssemblyVersionCompatibility.SameDomain
                },
                AssemblyBuilderAccess.RunAndSave, folderPath);

            // ReSharper disable once AssignNullToNotNullAttribute
            assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(EmittedAssemblyAttribute).GetConstructor(Type.EmptyTypes), Array.Empty<object>()));

            var moduleBuilder = assemblyBuilder.DefineDynamicModule(dllName, false);

            GetCodeToGenerate(out var methods, out var argumentTypes, out var conversions);
            CreateUsageType(moduleBuilder, methods, argumentTypes); // create a type where all the generic classes are used to save them for IL2CPP.

            var runtimeInitializeConverters = CollectRuntimeInitializeConverters(conversions, moduleBuilder);
            CreateRuntimeInitializedType(moduleBuilder, runtimeInitializeConverters);

            assemblyBuilder.Save(dllName);
        }

        private static List<(Type from, Type to, Type converterType)> CollectRuntimeInitializeConverters(HashSet<(Type from, Type to)> conversions, ModuleBuilder moduleBuilder)
        {
            var customConverters = GetCustomConverters();
            var runtimeInitializeConverters = new List<(Type from, Type to, Type converterType)>();

            foreach ((Type from, Type to) types in conversions)
            {
                if (Converter.BuiltInConverters.ContainsKey(types))
                    continue;

                (Type from, Type to) = types;

                if (customConverters.TryGetValue(types, out var customConverterType))
                {
                    runtimeInitializeConverters.Add((from, to, customConverterType));
                    continue;
                }

                var implicitOperator = ImplicitConversionsCache.GetImplicitOperatorForTypes(from, to);
                if (implicitOperator == null)
                {
                    Debug.LogWarning($"Found an ExtEvent with a generic argument of type {from} and a listener that requires type {to} but neither an implicit operator nor a custom converter was found for these types.");
                    continue;
                }

                var emittedConverterType = ConverterEmitter.EmitConverter(from, to, implicitOperator, moduleBuilder, "ExtEvents.AOTGenerated");
                runtimeInitializeConverters.Add((from, to, emittedConverterType));
            }

            return runtimeInitializeConverters;
        }

        private static void CreateRuntimeInitializedType(ModuleBuilder moduleBuilder, List<(Type from, Type to, Type converterType)> converterTypes)
        {
            var typeBuilder = moduleBuilder.DefineType("ExtEvents.AOTGeneratedType", TypeAttributes.Public);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                "OnLoad",
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                typeof(void),
                Type.EmptyTypes);

            var initializeOnLoadConstructor = typeof(RuntimeInitializeOnLoadMethodAttribute).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(RuntimeInitializeLoadType) }, null);
            // AfterAssembliesLoaded should be early enough that no one invokes ExtEvent. But if it's not early enough, we might try SubsystemRegistration, it runs even before that.
            // ReSharper disable once AssignNullToNotNullAttribute
            methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(initializeOnLoadConstructor, new object[] { RuntimeInitializeLoadType.AfterAssembliesLoaded }));

            var getTypeFromHandle = new Func<RuntimeTypeHandle, Type>(Type.GetTypeFromHandle).Method;

            var tupleConstructor = typeof(ValueTuple<Type, Type>).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type), typeof(Type) }, null);
            Assert.IsNotNull(tupleConstructor);

            var il = methodBuilder.GetILGenerator();

            var converterTypesField = typeof(Converter).GetField(nameof(Converter.ConverterTypes),
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            Assert.IsNotNull(converterTypesField);

            var addMethod = new Action<(Type, Type), Type>(new Dictionary<(Type, Type), Type>().Add).Method;

            il.Emit(OpCodes.Ldsfld, converterTypesField);
            il.Emit(OpCodes.Dup);

            foreach ((Type from, Type to, Type converterType) in converterTypes)
            {
                il.Emit(OpCodes.Ldtoken, from);
                il.EmitCall(OpCodes.Call, getTypeFromHandle, null);

                il.Emit(OpCodes.Ldtoken, to);
                il.EmitCall(OpCodes.Call, getTypeFromHandle, null);

                il.Emit(OpCodes.Newobj, tupleConstructor);

                il.Emit(OpCodes.Ldtoken, converterType);
                il.EmitCall(OpCodes.Call, getTypeFromHandle, null);
                il.EmitCall(OpCodes.Callvirt, addMethod, null);
            }

            il.Emit(OpCodes.Ret);

            typeBuilder.CreateType();
        }

        private static Dictionary<(Type fromType, Type toType), Type> GetCustomConverters()
        {
            var dictionary = new Dictionary<(Type fromType, Type toType), Type>();

            foreach (((Type from, Type to) fromToTypes, Type customConverter) in Converter.GetCustomConverters())
            {
                if (!Converter.BuiltInConverters.ContainsKey(fromToTypes))
                    dictionary.Add(fromToTypes, customConverter);
            }

            return dictionary;
        }

        private static void CreateUsageType(ModuleBuilder moduleBuilder, HashSet<CreateMethod> createMethods, HashSet<Type> argumentTypes)
        {
            var typeBuilder = moduleBuilder.DefineType("ExtEvents.GeneratedCreateMethods", TypeAttributes.Public);
            var ilGenerator = CreateStaticMethod(typeBuilder, "AOTGeneration");
            EmitCreateMethodUsages(ilGenerator, createMethods);
            EmitArgumentHolderUsages(ilGenerator, argumentTypes);
            AOTSupportUtilities.GenerateCode(ilGenerator, argumentTypes);
            ilGenerator.Emit(OpCodes.Ret);
            typeBuilder.CreateType();
        }

        private static ILGenerator CreateStaticMethod(TypeBuilder typeBuilder, string methodName)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                methodName,
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                typeof(void),
                Type.EmptyTypes);

            return methodBuilder.GetILGenerator();
        }

        private static void EmitCreateMethodUsages(ILGenerator ilGenerator, HashSet<CreateMethod> createMethods)
        {
            foreach (var createMethod in createMethods)
            {
                ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.EmitCall(OpCodes.Call, PersistentListener.InvokableCallCreator.GetCreateMethod(createMethod.Args, createMethod.IsVoid), null);
                ilGenerator.Emit(OpCodes.Pop);
            }
        }

        private static void EmitArgumentHolderUsages(ILGenerator ilGenerator, HashSet<Type> argumentTypes)
        {
            var genericArgs = new Type[1];
            var throwAwayVariable = ilGenerator.DeclareLocal(typeof(ArgumentHolder));

            foreach (Type argumentType in argumentTypes.Where(argumentType => argumentType.IsValueType))
            {
                genericArgs[0] = argumentType;
                var holderType = typeof(ArgumentHolder<>).MakeGenericType(genericArgs);
                var constructor = holderType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                // ReSharper disable once AssignNullToNotNullAttribute
                ilGenerator.Emit(OpCodes.Newobj, constructor);
                ilGenerator.Emit(OpCodes.Stloc, throwAwayVariable);
            }
        }

        private static void GetCodeToGenerate(out HashSet<CreateMethod> methods, out HashSet<Type> argumentTypes, out HashSet<(Type from, Type to)> conversions)
        {
            methods = new HashSet<CreateMethod>();
            argumentTypes = new HashSet<Type>();
            conversions = new HashSet<(Type from, Type to)>();

            var serializedObjects = ProjectWideSearcher.GetSerializedObjectsInProject();
            var extEventProperties = SerializedPropertyHelper.FindPropertiesOfType(serializedObjects, "ExtEvent");
            var listenerProperties = ExtEventProjectSearcher.GetListeners(extEventProperties);

            foreach (var listenerProperty in listenerProperties)
            {
                var methodInfo = ExtEventProjectSearcher.GetMethod(listenerProperty);
                GetMethodDetails(methodInfo, ref argumentTypes, ref methods);

                foreach (var types in ExtEventProjectSearcher.GetNonMatchingArgumentTypes(listenerProperty))
                {
                    conversions.Add(types);
                }
            }
        }

        private static void GetMethodDetails(MethodInfo methodInfo, ref HashSet<Type> argumentTypes, ref HashSet<CreateMethod> methods)
        {
            if (methodInfo == null)
                return;

            var args = GetArgumentTypes(methodInfo.GetParameters());
            bool isVoid = methodInfo.ReturnType == typeof(void);

            if (!isVoid)
            {
                ArrayHelper.Add(ref args, methodInfo.ReturnType);
            }

            if (args.Length == 0)
                return;

            bool hasValueType = false;

            foreach (Type argType in args)
            {
                if (argType.IsValueType)
                    hasValueType = true;

                argumentTypes.Add(argType);
            }

            if (hasValueType)
                methods.Add(new CreateMethod(isVoid, args));
        }

        private static Type[] GetArgumentTypes(ParameterInfo[] parameters)
        {
            var types = new Type[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                types[i] = parameters[i].ParameterType;
            }

            return types;
        }

        private readonly struct CreateMethod : IEquatable<CreateMethod>
        {
            public readonly bool IsVoid;
            public readonly Type[] Args;

            public CreateMethod(bool isVoid, Type[] args)
            {
                IsVoid = isVoid;
                Args = args;
            }

            public override bool Equals(object obj) => obj is CreateMethod other && this.Equals(other);

            public bool Equals(CreateMethod p) => IsVoid == p.IsVoid && Args.SequenceEqual(p.Args);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;

                    foreach (var element in Args)
                    {
                        hash = hash * 31 + element.GetHashCode();
                    }

                    hash = hash * 31 + IsVoid.GetHashCode();

                    return hash;
                }
            }

            public static bool operator ==(CreateMethod lhs, CreateMethod rhs) => lhs.Equals(rhs);

            public static bool operator !=(CreateMethod lhs, CreateMethod rhs) => !(lhs == rhs);
        }
    }
}