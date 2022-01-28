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
    using GenericUnityObjects.Editor.Util;
    using SolidUtilities;
    using SolidUtilities.Editor;
    using UnityEditor;

    public static class AOTAssemblyGenerator
    {
        private const string FolderPath = PackageSettings.PluginsPath + "/" + "AOT Generation";
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
                CreateAssemblyDefinition(FolderPath);
            }

            CreateAssembly(dllName, FolderPath);

            if (folderExists)
            {
                AssetDatabase.ImportAsset(assemblyPath, ImportAssetOptions.ForceUpdate);
            }
            else
            {
                AssemblyGeneration.ImportAssemblyAsset(assemblyPath, AssemblyGeneration.GetUniqueGUID());
            }
        }

        public static void DeleteGeneratedFolder()
        {
            if (Directory.Exists(FolderPath))
                AssetDatabase.DeleteAsset(FolderPath);
        }

        private static void CreateAssemblyDefinition(string folderPath)
        {
            string asmDefPath = $"{folderPath}/ExtEvents.AOTGeneration.asmdef";
            File.WriteAllText(asmDefPath, AsmDefContent);
            AssetDatabase.ImportAsset(asmDefPath);
            
            string scriptPath = $"{folderPath}/AOTGeneration.cs";
            File.WriteAllText(scriptPath, ScriptContent);
            AssetDatabase.ImportAsset(scriptPath);
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
            
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(dllName, false);
            
            var typeBuilder = moduleBuilder.DefineType("ExtEvents.GeneratedCreateMethods", TypeAttributes.Public);
            
            CreateType(typeBuilder, GetCreateMethods());
            
            typeBuilder.CreateType();
            assemblyBuilder.Save(dllName);
        }

        private static void CreateType(TypeBuilder typeBuilder, IEnumerable<CreateMethod> createMethods)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                "AOTGeneration",
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                typeof(void),
                Type.EmptyTypes);
            
            ILGenerator ilGenerator = methodBuilder.GetILGenerator();

            foreach (var createMethod in createMethods)
            {
                ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.EmitCall(OpCodes.Call, PersistentListener.InvokableCallCreator.GetCreateMethod(createMethod.Args, createMethod.IsVoid), null);
                ilGenerator.Emit(OpCodes.Pop);
            }
            
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static IEnumerable<CreateMethod> GetCreateMethods()
        {
            var methods = new HashSet<CreateMethod>();

            var serializedObjects = SerializedObjectFinder.GetSerializedObjects();
            var extEventProperties = ExtEventProjectSearcher.FindExtEventProperties(serializedObjects);
            var methodInfos = ExtEventProjectSearcher.GetMethods(extEventProperties);

            foreach (var methodInfo in methodInfos)
            {
                var args = GetArgumentTypes(methodInfo.GetParameters());
                bool isVoid = methodInfo.ReturnType == typeof(void);

                if (!isVoid)
                {
                    ArrayHelper.Add(ref args, methodInfo.ReturnType);
                }

                if (args.Length == 0 || args.Any(type => type.IsNotPublic))
                    continue;

                methods.Add(new CreateMethod(isVoid, args));
            }

            return methods;
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
        
        private const string AsmDefContent = @"{
    ""name"": ""ExtEvents.AOTGeneration"",
    ""rootNamespace"": ""ExtEvents.AOTGeneration"",
    ""references"": [],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
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

        private const string ScriptContent = @"namespace ExtEvents.AOTGeneration
{
    using UnityEngine.Scripting;

    public static class AOTGeneration
    {
        [Preserve]
        public static void Generation()
        {
            GeneratedCreateMethods.AOTGeneration();
        }
    }
}
";

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