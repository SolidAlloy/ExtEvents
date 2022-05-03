namespace ExtEvents
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public class PackageSettings : ScriptableObject
    {
        public const string PackageName = "com.solidalloy.ext-events";
        public const string PluginsPath = "Assets/Plugins/ExtEvents";

        private const string FolderPath = PluginsPath + "/Resources";
        private const string AssetName = "ExtEvents_PackageSettings";

        [Tooltip("Whether a warning should be logged when an event is invoked but the listener property or method is missing")]
        public bool _showInvocationWarning = true;
        public static bool ShowInvocationWarning => Instance._showInvocationWarning;

        private static PackageSettings _instance;
        internal static PackageSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<PackageSettings>(AssetName);

                if (_instance != null)
                    return _instance;

                _instance = CreateInstance<PackageSettings>();

#if UNITY_EDITOR
                Directory.CreateDirectory(FolderPath);
                AssetDatabase.CreateAsset(_instance, $"{FolderPath}/{AssetName}.asset");
#endif

                return _instance;
            }
        }
    }
}