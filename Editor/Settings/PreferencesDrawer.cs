namespace ExtEvents.Editor
{
    using System.Collections.Generic;
    using SolidUtilities.Editor;
    using UnityEditor;

    public static class PreferencesDrawer
    {
        private const string NicifyArgumentNamesLabel = "Nicify arguments names";

        private const string NicifyArgumentNamesTooltip = "Replace the original argument names (e.g. \"currentPlayer\") with more readable labels - \"Current Player\"";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Preferences/Packages/Ext Events", SettingsScope.User)
            {
                guiHandler = OnGUI,
                keywords = GetKeywords()
            };
        }

        private static void OnGUI(string searchContext)
        {
            using (EditorGUIUtilityHelper.LabelWidthBlock(180f))
            {
                EditorPackageSettings.NicifyArgumentNames = EditorGUILayout.Toggle(GUIContentHelper.Temp(NicifyArgumentNamesLabel, NicifyArgumentNamesTooltip), EditorPackageSettings.NicifyArgumentNames);
            }
        }

        private static HashSet<string> GetKeywords()
        {
            var keywords = new HashSet<string>();
            keywords.AddWords(NicifyArgumentNamesLabel);
            keywords.AddWords(NicifyArgumentNamesTooltip);
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