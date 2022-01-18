namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using SolidUtilities;
    using UnityEditor;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

    [InitializeOnLoad]
    public static class BuildProcessor
    {
        static BuildProcessor()
        {
            // Replace the default action of building a player with a custom one.
            // BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayer);
        }

        private static void BuildPlayer(BuildPlayerOptions options)
        {
            // A partial copy of BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options)
            // so that we exit prematurely if the build is known to fail.
            if (EditorApplication.isCompiling)
                return;

            if (!BuildPipeline.IsBuildTargetSupported(options.targetGroup, options.target))
                throw new BuildPlayerWindow.BuildMethodException("Build target is not supported.");

            if (Unsupported.IsBleedingEdgeBuild())
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(
                    "This version of Unity is a BleedingEdge build that has not seen any manual testing.");
                stringBuilder.AppendLine("You should consider this build unstable.");
                stringBuilder.AppendLine("We strongly recommend that you use a normal version of Unity instead.");
                if (EditorUtility.DisplayDialog("BleedingEdge Build", stringBuilder.ToString(), "Cancel", "OK"))
                    throw new BuildPlayerWindow.BuildMethodException();
            }

            var activeBuildTargetGroup = (BuildTargetGroup) typeof(EditorUserBuildSettings).GetProperty("activeBuildTargetGroup",
                BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            if (EditorUserBuildSettings.activeBuildTarget != options.target || activeBuildTargetGroup != options.targetGroup)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTargetAsync(options.targetGroup, options.target))
                {
                    throw new BuildPlayerWindow.BuildMethodException("Could not switch to build target.");
                }

                return;
            }

            var report = GetBuildReport(options);
            CheckResponseAssets(report);
            // BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }

        private static void CheckResponseAssets(BuildReport report)
        {
            foreach (SearchableAsset searchableAsset in GetAssetsEligibleForResponseBuild(report).Where(asset => asset.AssetType == SearchableAsset.AssetTypes.Prefab || asset.AssetType == SearchableAsset.AssetTypes.Scene))
            {
                Debug.Log(searchableAsset.Path);
            }
        }

        private static BuildReport GetBuildReport(BuildPlayerOptions options)
        {
            var playerDataOptionsType = typeof(BuildPipeline).Assembly.GetType("UnityEditor.BuildPlayerDataOptions");
            var runtimeClassRegistryType = typeof(BuildPipeline).Assembly.GetType("UnityEditor.RuntimeClassRegistry");

            var playerDataOptions = GetPlayerDataOptions(options, playerDataOptionsType);
            var registry = Activator.CreateInstance(runtimeClassRegistryType);

            var buildPlayerDataMethod = typeof(BuildPipeline).GetMethod("BuildPlayerData", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { playerDataOptionsType, runtimeClassRegistryType.MakeByRefType() }, null);
            return (BuildReport) buildPlayerDataMethod.Invoke(null, new[] {playerDataOptions, registry });
        }

        private static IEnumerable<SearchableAsset> GetAssetsEligibleForResponseBuild(BuildReport report)
        {
            return report.packedAssets
                .SelectMany(packedAsset => packedAsset.contents)
                .Select(assetInfo => assetInfo.sourceAssetPath)
                .SelectWhere(assetPath =>
                {
                    if (assetPath.EndsWith(".asset"))
                    {
                        var scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                        return scriptableObject is null ? (false, null) : (true, new SearchableAsset(SearchableAsset.AssetTypes.ScriptableObject, assetPath, scriptableObject));
                    }

                    if (assetPath.EndsWith(".scene"))
                    {
                        return (true, new SearchableAsset(SearchableAsset.AssetTypes.Scene, assetPath));
                    }

                    if (assetPath.EndsWith(".prefab"))
                    {
                        return (true, new SearchableAsset(SearchableAsset.AssetTypes.Prefab, assetPath));
                    }

                    return (false, null);
                });
        }

        private static object GetPlayerDataOptions(BuildPlayerOptions options, Type playerDataOptionsType)
        {
            var dataOptions = Activator.CreateInstance(playerDataOptionsType);
            var scenes = playerDataOptionsType.GetProperty("scenes");
            var targetGroup = playerDataOptionsType.GetProperty("targetGroup");
            var target = playerDataOptionsType.GetProperty("target");
            var optionsProperty = playerDataOptionsType.GetProperty("options");

            scenes.SetValue(dataOptions, EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes));
            targetGroup.SetValue(dataOptions, options.targetGroup);
            target.SetValue(dataOptions, options.target);
            optionsProperty.SetValue(dataOptions, options.options);

            return dataOptions;
        }

        private class SearchableAsset
        {
            public enum AssetTypes
            {
                Scene,
                Prefab,
                ScriptableObject
            }

            public AssetTypes AssetType;
            public string Path;
            public ScriptableObject ScriptableObject;

            public SearchableAsset(AssetTypes assetType, string path, ScriptableObject scriptableObject = null)
            {
                AssetType = assetType;
                Path = path;
                ScriptableObject = scriptableObject;
            }
        }
    }
}