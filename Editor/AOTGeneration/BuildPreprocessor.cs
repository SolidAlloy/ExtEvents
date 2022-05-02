namespace ExtEvents.Editor
{
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

    public class BuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => EditorPackageSettings.BuildCallbackOrder;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup) !=
                ScriptingImplementation.IL2CPP)
            {
                AOTAssemblyGenerator.DeleteGeneratedFolder();
                return;
            }

#if UNITY_2021_2_OR_NEWER
            switch (EditorUserBuildSettings.il2CppCodeGeneration)
            {
                case Il2CppCodeGeneration.OptimizeSpeed:
                    AOTAssemblyGenerator.GenerateCreateMethods();
                    break;
                case Il2CppCodeGeneration.OptimizeSize:
                    AOTAssemblyGenerator.DeleteGeneratedFolder();
                    break;
                default:
                    Debug.LogWarning($"Unknown value of IL2CPP Code Generation: {EditorUserBuildSettings.il2CppCodeGeneration}");
                    break;
            }
#else
            AOTAssemblyGenerator.GenerateCreateMethods();
#endif
        }
    }
}