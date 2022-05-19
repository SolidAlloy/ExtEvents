namespace ExtEvents.Editor
{
    using System.IO;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

    public class BuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => EditorPackageSettings.BuildCallbackOrder;

        public void OnPreprocessBuild(BuildReport report)
        {
            CreateLinkXml();

            if (PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup) !=
                ScriptingImplementation.IL2CPP)
            {
                AOTAssemblyGenerator.DeleteGeneratedFolder();
                return;
            }

#if UNITY_2021_2_OR_NEWER
            var codeGeneration =
    #if UNITY_2022
                PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
    #else
                EditorUserBuildSettings.il2CppCodeGeneration;
    #endif

            switch (codeGeneration)
            {
                case Il2CppCodeGeneration.OptimizeSpeed:
                    AOTAssemblyGenerator.GenerateCreateMethods();
                    break;
                case Il2CppCodeGeneration.OptimizeSize:
                    AOTAssemblyGenerator.DeleteGeneratedFolder();
                    break;
                default:
                    Debug.LogWarning($"Unknown value of IL2CPP Code Generation: {codeGeneration}");
                    break;
            }
#else
            AOTAssemblyGenerator.GenerateCreateMethods();
#endif
        }

        private void CreateLinkXml()
        {
            if (!Directory.Exists(PackageSettings.PluginsPath))
            {
                Directory.CreateDirectory(PackageSettings.PluginsPath);
            }

            string linkXmlPath = $"{PackageSettings.PluginsPath}/link.xml";

            if (File.Exists(linkXmlPath))
                return;

            // preserve the OdinSerializer assembly because it has a lot of code that is invoked through reflection and we are lazy to write [Preserve] all over the place.
            File.WriteAllText(linkXmlPath, "<linker>\n    <assembly fullname=\"ExtEvents.OdinSerializer\" preserve=\"all\"/>\n</linker>");
            AssetDatabase.ImportAsset(linkXmlPath);
        }
    }
}