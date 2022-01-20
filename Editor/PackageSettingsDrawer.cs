namespace ExtEvents.Editor
{
    using System.Collections.Generic;
    using SolidUtilities.Editor;
    using UnityEditor;

    public static class PackageSettingsDrawer
    {
        private const string IncludeInternalMethodsLabel = "Include internal methods";
        private const string IncludeInternalMethodsTooltip = "Include internal methods and properties in the methods dropdown when choosing a listener in ExtEvent?";
        
        private const string IncludePrivateMethodsLabel = "Include private methods";
        private const string IncludePrivateMethodsTooltip = "Include private and protected methods and properties in the methods dropdown when choosing a listener in ExtEvent?";
        
        private static SerializedObject _serializedObject;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Packages/Ext Events", SettingsScope.Project)
            {
                guiHandler = OnGUI,
                keywords = GetKeywords()
            };
        }

        private static void OnGUI(string searchContext)
        {
            DrawSerializedObject();
            
            EditorPackageSettings.IncludeInternalMethods = EditorGUILayout.Toggle(GUIContentHelper.Temp(IncludeInternalMethodsLabel, IncludeInternalMethodsTooltip), EditorPackageSettings.IncludeInternalMethods);
            EditorPackageSettings.IncludePrivateMethods = EditorGUILayout.Toggle(GUIContentHelper.Temp(IncludePrivateMethodsLabel, IncludePrivateMethodsTooltip), EditorPackageSettings.IncludePrivateMethods);
        }

        private static void DrawSerializedObject()
        {
            _serializedObject ??= new SerializedObject(PackageSettings.Instance);

            _serializedObject.UpdateIfRequiredOrScript();

            foreach (SerializedProperty property in GetVisibleProperties())
            {
                EditorGUILayout.PropertyField(property, true);
            }

            _serializedObject.ApplyModifiedProperties();
        }

        private static IEnumerable<SerializedProperty> GetVisibleProperties()
        {
            SerializedProperty iterator = _serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                if (iterator.propertyPath != "m_Script")
                    yield return iterator;
            }
        }

        private static HashSet<string> GetKeywords()
        {
            _serializedObject ??= new SerializedObject(PackageSettings.Instance);

            var keywords = new HashSet<string>();

            foreach (var property in GetVisibleProperties())
            {
                keywords.AddWords(property.displayName);
            }

            return keywords;
        }

        private static readonly char[] _separators = { ' ' };

        private static void AddWords(this HashSet<string> set, string phrase)
        {
            foreach (string word in phrase.Split(_separators))
            {
                set.Add(word);
            }
        }
    }
}