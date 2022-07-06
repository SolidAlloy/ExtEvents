namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using SolidUtilities.Editor;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

    public class BuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => EditorPackageSettings.BuildCallbackOrder;

        public void OnPreprocessBuild(BuildReport report)
        {
            var scriptingBackend = PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup);
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;

            using var _ = AssetDatabaseHelper.DisabledScope();
            AOTAssemblyGenerator.StartCreatingAssembly(out var assemblyBuilder, out var moduleBuilder, out string dllName);

            // emit custom converters and link.xml for them.
            var customConverters = AOTAssemblyGenerator.GetCustomConverters();

            // all converters except for built-in need to be added to a dictionary of converter types.
            // To do that, we emit a method that uses the RuntimeInitializeOnLoad attribute and adds all the converters when the game starts.
            var runtimeInitializeConverters = new List<(Type from, Type to, Type converterType)>();

            // Built-in converters already have the Preserve attribute.
            // Emitted converters are added to the assembly which is preserved through link.xml
            // For custom converters, however, we can't guarantee that they are preserved, so they have to be added to link.xml separately.
            // A key for the dictionary is the assembly name, the value is all the types that need to be preserved in that assembly.
            var typesToPreserve = new Dictionary<string, List<string>>();

            // Add custom converter type to runtimeInitializeConverters, so that they are added to a dictionary of converter types on start,
            // and add them to typesToPreserve so that they are added to link.xml
            foreach (var keyValue in customConverters)
            {
                var customConverterType = keyValue.Value;

                runtimeInitializeConverters.Add((keyValue.Key.fromType, keyValue.Key.toType, customConverterType));

                string assemblyName = customConverterType.Assembly.GetName().Name;
                if (!typesToPreserve.TryGetValue(assemblyName, out var preservedTypes))
                {
                    preservedTypes = new List<string>();
                    typesToPreserve.Add(assemblyName, preservedTypes);
                }

                preservedTypes.Add(customConverterType.FullName);
            }

            AOTAssemblyGenerator.CreateLinkXml(typesToPreserve);

            var methods = new HashSet<AOTAssemblyGenerator.CreateMethod>();
            var argumentTypes = new HashSet<Type>();

            // If we can't emit code in builds, emit all the types we will need ahead of time.
            // For Mono stripping level of medium and above, implicit operators that are not used will be stripped.
            // We must therefore find all the types with implicit operators and add them to link.xml or reference in an emitted method so that they are not stripped.
            // If we go to such extents, why not just emit the converters AOT then? We already have the code that does it.
            if (scriptingBackend == ScriptingImplementation.IL2CPP || buildTarget != BuildTargetGroup.Standalone || PlayerSettings.GetManagedStrippingLevel(buildTarget) >= ManagedStrippingLevel.Medium)
            {
                var conversions = new HashSet<(Type from, Type to)>();

                var serializedObjects = ProjectWideSearcher.GetSerializedObjectsInProject();
                var extEventProperties = SerializedPropertyHelper.FindPropertiesOfType(serializedObjects, "ExtEvent");

                foreach (var listenerProperty in ExtEventProjectSearcher.GetListeners(extEventProperties))
                {
                    // Although we don't use argumentTypes and methods yet and might not use at all, we gather them here so that we have to go through listener only once.
                    // Listeners are returned lazily and iterating through them again would require to search through the whole project one more time.
                    // It's cheaper to gather these details and discard if we don't need them.
                    var methodInfo = ExtEventProjectSearcher.GetMethod(listenerProperty);
                    AOTAssemblyGenerator.GetMethodDetails(methodInfo, ref argumentTypes, ref methods);

                    foreach (var types in ExtEventProjectSearcher.GetNonMatchingArgumentTypes(listenerProperty))
                    {
                        conversions.Add(types);
                    }
                }

                foreach (var types in AOTAssemblyGenerator.EmitImplicitConverters(moduleBuilder, customConverters, conversions))
                {
                    runtimeInitializeConverters.Add(types);
                }
            }

            // Create a type that adds all the converter types to a dictionary on start.
            AOTAssemblyGenerator.CreateRuntimeInitializedType(moduleBuilder, runtimeInitializeConverters);
            // Create a script that will call a method from the emitted type because if we declare RuntimeInitializeOnLoad attribute inside the emitted assembly, it will have no effect.
            AOTAssemblyGenerator.CreateAssemblyDefinition();

            if (scriptingBackend != ScriptingImplementation.IL2CPP)
            {
                AOTAssemblyGenerator.FinishCreatingAssembly(assemblyBuilder, dllName);
                return;
            }

            // if IL2CPP setting and OptimizeSpeed, emit generic types usage.
#if UNITY_2021_2_OR_NEWER
            var codeGeneration =
    #if UNITY_2022
                PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
    #else
                EditorUserBuildSettings.il2CppCodeGeneration;
    #endif

            // listenerProperties will be initialized for sure here because we initialized inside an if statement that always runs if the scripting backend is IL2CPP.
            if (codeGeneration == Il2CppCodeGeneration.OptimizeSpeed)
                AOTAssemblyGenerator.CreateUsageType(moduleBuilder, methods, argumentTypes); // create a type where all the generic classes are used to save them for IL2CPP.
#else
            AOTAssemblyGenerator.EmitGenericTypesUsage(moduleBuilder, listenerProperties);
#endif

            AOTAssemblyGenerator.FinishCreatingAssembly(assemblyBuilder, dllName);
        }
    }
}