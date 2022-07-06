namespace ExtEvents.Editor
{
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

    public class BuildPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder { get; }

        public void OnPostprocessBuild(BuildReport report)
        {
            AssetDatabase.DeleteAsset(AOTAssemblyGenerator.FolderPath);
        }
    }
}