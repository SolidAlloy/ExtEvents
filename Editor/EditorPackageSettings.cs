namespace ExtEvents.Editor
{
    using UnityEditor;
    using UnityEditor.SettingsManagement;

    public static class EditorPackageSettings
    {
        private static Settings _instance;

        private static UserSetting<bool> _nicifyArgumentNames;
        public static bool NicifyArgumentNames
        {
            get
            {
                InitializeIfNeeded();
                return _nicifyArgumentNames.value;
            }

            set => _nicifyArgumentNames.value = value;
        }

        private static UserSetting<bool> _includeInternalMethods;
        public static bool IncludeInternalMethods
        {
            get
            {
                InitializeIfNeeded();
                return _includeInternalMethods.value;
            }

            set => _includeInternalMethods.value = value;
        }
        
        private static UserSetting<bool> _includePrivateMethods;
        public static bool IncludePrivateMethods
        {
            get
            {
                InitializeIfNeeded();
                return _includePrivateMethods.value;
            }

            set => _includePrivateMethods.value = value;
        }

        private static void InitializeIfNeeded()
        {
            if (_instance != null)
                return;

            _instance = new Settings(PackageSettings.PackageName);

            _nicifyArgumentNames = new UserSetting<bool>(_instance, nameof(_nicifyArgumentNames), true, SettingsScope.User);
            _includeInternalMethods = new UserSetting<bool>(_instance, nameof(_includeInternalMethods), false, SettingsScope.Project);
            _includePrivateMethods = new UserSetting<bool>(_instance, nameof(_includePrivateMethods), false, SettingsScope.Project);
        }
    }
}