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
    using System.Text;
    using OdinSerializer;
    using OdinSerializer.Editor;
    using SolidUtilities;
    using SolidUtilities.Editor;
    using UnityEditor;
    using UnityEngine;
    using Assert = UnityEngine.Assertions.Assert;

    public static class AOTAssemblyGenerator
    {
        public const string FolderPath = PackageSettings.PluginsPath + "/AOT Generation";
        private const string AssemblyName = "z_ExtEvents_AOTGeneration";

        public static IEnumerable<(Type from, Type to, Type emittedConverterType)> EmitImplicitConverters(ModuleBuilder moduleBuilder, Dictionary<(Type from, Type to), Type> customConverters, IEnumerable<(Type from, Type to)> conversions)
        {
            foreach ((Type from, Type to) types in conversions)
            {
                if (Converter.BuiltInConverters.ContainsKey(types) || customConverters.ContainsKey(types))
                    continue;

                (Type from, Type to) = types;

                // For converters that have to be emitted, emit them and add to the list of converters that have to be added to a dictionary of converter types on start.
                var implicitOperator = ImplicitConversionsCache.GetImplicitOperatorForTypes(from, to);
                if (implicitOperator == null)
                {
                    Debug.LogWarning($"Found an ExtEvent with a generic argument of type {from} and a listener that requires type {to} but neither an implicit operator nor a custom converter was found for these types.");
                    continue;
                }

                var emittedConverterType = ConverterEmitter.EmitConverter(from, to, implicitOperator, moduleBuilder, "ExtEvents.AOTGenerated");
                yield return (from, to, emittedConverterType);
            }
        }

        public static void StartCreatingAssembly(out AssemblyBuilder assemblyBuilder, out ModuleBuilder moduleBuilder, out string dllName)
        {
            dllName = $"{AssemblyName}.dll";

            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(AssemblyName)
                {
                    CultureInfo = CultureInfo.InvariantCulture,
                    Flags = AssemblyNameFlags.None,
                    ProcessorArchitecture = ProcessorArchitecture.MSIL,
                    VersionCompatibility = AssemblyVersionCompatibility.SameDomain
                },
                AssemblyBuilderAccess.RunAndSave, FolderPath);

            // ReSharper disable once AssignNullToNotNullAttribute
            assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(EmittedAssemblyAttribute).GetConstructor(Type.EmptyTypes), Array.Empty<object>()));

            moduleBuilder = assemblyBuilder.DefineDynamicModule(dllName, false);
        }

        public static void FinishCreatingAssembly(AssemblyBuilder assemblyBuilder, string dllName)
        {
            string assemblyPath = $"{FolderPath}/{dllName}";

            // finish creating the assembly
            assemblyBuilder.Save(dllName);

            if (File.Exists($"{assemblyPath}.meta"))
            {
                AssetDatabase.ImportAsset(assemblyPath, ImportAssetOptions.ForceUpdate);
            }
            else
            {
                AssemblyGeneration.ImportAssemblyAsset(assemblyPath, AssetDatabaseHelper.GetUniqueGUID(), excludeEditor: true);
            }
        }

        public static void CreateAssemblyDefinition()
        {
            string asmDefPath = $"{FolderPath}/ExtEvents.AOTGenerated.asmdef";
            File.WriteAllText(asmDefPath, AsmDefContent);
            AssetDatabase.ImportAsset(asmDefPath);
            string scriptPath = $"{FolderPath}/LoadConverterTypes.cs";
            File.WriteAllText(scriptPath, ScriptContent);
            AssetDatabase.ImportAsset(scriptPath);
        }

        private const string AsmDefContent =
@"{
    ""name"": ""ExtEvents.AOTGenerated"",
        ""rootNamespace"": """",
        ""references"": [],
        ""includePlatforms"": [],
        ""excludePlatforms"": [
        ""Editor""
        ],
        ""allowUnsafeCode"": false,
        ""overrideReferences"": true,
        ""precompiledReferences"": [
        ""z_ExtEvents_AOTGeneration.dll""
        ],
        ""autoReferenced"": false,
        ""defineConstraints"": [],
        ""versionDefines"": [],
        ""noEngineReferences"": false
    }";

        private const string ScriptContent =
@"#if !UNITY_EDITOR
using ExtEvents;
using UnityEngine;
using UnityEngine.Scripting;

public static class LoadConverterTypes
{
    // AfterAssembliesLoaded should be early enough that no one invokes ExtEvent. But if it's not early enough, we might try SubsystemRegistration, it runs even before that.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded), Preserve]
    public static void OnLoad()
    {
        AOTGeneratedType.OnLoad();
    }
}
#endif
";

        public static void CreateLinkXml(Dictionary<string, List<string>> typesToPreserve)
        {
            const string tab = "    ";

            var stringBuilder = new StringBuilder(string.Empty, 81);
            stringBuilder.AppendLine("<linker>");
            // preserve the OdinSerializer assembly because it has a lot of code that is invoked through reflection and we are lazy to write [Preserve] all over the place.
            stringBuilder.AppendLine($"{tab}<assembly fullname=\"ExtEvents.OdinSerializer\" preserve=\"all\"/>");
            stringBuilder.AppendLine($"{tab}<assembly fullname=\"{AssemblyName}\" preserve=\"all\"/>");

            foreach (var assemblyTypes in typesToPreserve)
            {
                stringBuilder.AppendLine($"{tab}<assembly fullname=\"{assemblyTypes.Key}\">");

                foreach (string typeName in assemblyTypes.Value)
                {
                    stringBuilder.AppendLine($"{tab}{tab}<type fullname=\"{typeName}\" preserve=\"all\"/>");
                }

                stringBuilder.AppendLine($"{tab}</assembly>");
            }

            stringBuilder.AppendLine("</linker>");

            string linkXmlPath = $"{FolderPath}/link.xml";
            File.WriteAllText(linkXmlPath, stringBuilder.ToString());
            AssetDatabase.ImportAsset(linkXmlPath);
        }

        public static void CreateRuntimeInitializedType(ModuleBuilder moduleBuilder, List<(Type from, Type to, Type converterType)> converterTypes)
        {
            var typeBuilder = moduleBuilder.DefineType("ExtEvents.AOTGeneratedType", TypeAttributes.Public);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                "OnLoad",
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                typeof(void),
                Type.EmptyTypes);

            var getTypeFromHandle = new Func<RuntimeTypeHandle, Type>(Type.GetTypeFromHandle).Method;

            var tupleConstructor = typeof(ValueTuple<Type, Type>).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type), typeof(Type) }, null);
            Assert.IsNotNull(tupleConstructor);

            var il = methodBuilder.GetILGenerator();

            if (converterTypes.Count != 0)
            {
                var converterTypesField = typeof(Converter).GetField(nameof(Converter.ConverterTypes),
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                Assert.IsNotNull(converterTypesField);

                var addMethod = new Action<(Type, Type), Type>(new Dictionary<(Type, Type), Type>().Add).Method;

                il.Emit(OpCodes.Ldsfld, converterTypesField);

                if (converterTypes.Count > 1)
                    il.Emit(OpCodes.Dup);

                foreach ((Type from, Type to, Type converterType) in converterTypes)
                {
                    il.Emit(OpCodes.Ldtoken, from);
                    il.Emit(OpCodes.Call, getTypeFromHandle);

                    il.Emit(OpCodes.Ldtoken, to);
                    il.Emit(OpCodes.Call, getTypeFromHandle);

                    il.Emit(OpCodes.Newobj, tupleConstructor);

                    il.Emit(OpCodes.Ldtoken, converterType);
                    il.Emit(OpCodes.Call, getTypeFromHandle);
                    il.Emit(OpCodes.Callvirt, addMethod);
                }
            }

            il.Emit(OpCodes.Ret);

            typeBuilder.CreateType();
        }

        public static Dictionary<(Type fromType, Type toType), Type> GetCustomConverters()
        {
            var dictionary = new Dictionary<(Type fromType, Type toType), Type>();

            foreach (((Type from, Type to) fromToTypes, Type customConverter) in Converter.GetCustomConverters())
            {
                if (!Converter.BuiltInConverters.ContainsKey(fromToTypes))
                    dictionary.Add(fromToTypes, customConverter);
            }

            return dictionary;
        }

        public static void CreateUsageType(ModuleBuilder moduleBuilder, HashSet<CreateMethod> createMethods, HashSet<Type> argumentTypes)
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

        public static void GetMethodDetails(MethodInfo methodInfo, ref HashSet<Type> argumentTypes, ref HashSet<CreateMethod> methods)
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

        public readonly struct CreateMethod : IEquatable<CreateMethod>
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