namespace ExtEvents.Editor
{
    using System;
    using SolidUtilities;
    using SolidUtilities.Editor;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

#if GENERIC_UNITY_OBJECTS
    using GenericUnityObjects.Editor;
#endif

    public static class DynamicListenersDrawer
    {
        public static float GetHeight(SerializedProperty extEventProperty)
        {
            bool isEventExpanded = extEventProperty.FindPropertyRelative(nameof(BaseExtEvent.Expanded)).boolValue;

            if (!isEventExpanded)
                return 0f;

            var eventObject = PropertyObjectCache.GetObject<BaseExtEvent>(extEventProperty);

            if (eventObject._dynamicListeners == null)
                return 0f;

            return (EditorGUIUtility.singleLineHeight + EditorPackageSettings.LinePadding) * (extEventProperty.isExpanded ? eventObject._dynamicListeners.GetInvocationList().Length : 0);
        }

        public static void DrawListeners(SerializedProperty extEventProperty, Rect totalRect, float listHeight)
        {
            using var _ = EditorGUIHelper.IndentLevelBlock(EditorGUI.indentLevel + 2);

            bool isEventExpanded = extEventProperty.FindPropertyRelative(nameof(BaseExtEvent.Expanded)).boolValue;

            if (!isEventExpanded)
                return;

            var eventObject = PropertyObjectCache.GetObject<BaseExtEvent>(extEventProperty);
            if (eventObject._dynamicListeners == null)
                return;

            Rect currentRect = new Rect(totalRect) { height = EditorGUIUtility.singleLineHeight, y = totalRect.y + listHeight - EditorGUIUtility.singleLineHeight - EditorPackageSettings.LinePadding };

            extEventProperty.isExpanded = EditorGUI.Foldout(currentRect, extEventProperty.isExpanded, "Dynamic Listeners", true);

            if (!extEventProperty.isExpanded)
                return;

            foreach (var @delegate in eventObject._dynamicListeners.GetInvocationList())
            {
                currentRect.y += EditorGUIUtility.singleLineHeight + EditorPackageSettings.LinePadding;
                float halfWidth = currentRect.width / 2f;
                var typeRect = new Rect(currentRect) { width = halfWidth };
                var methodRect = new Rect(currentRect) { x = currentRect.x + halfWidth, width = halfWidth };

                string methodName = $"{@delegate.Method.Name}()";
                if (methodName.StartsWith("<"))
                    methodName = "Lambda Expression";

                DrawDynamicType(typeRect, @delegate);
                EditorGUI.LabelField(methodRect, methodName);
            }
        }

        private static void DrawDynamicType(Rect rect, Delegate @delegate)
        {
            if (@delegate.Target is Object objectTarget)
            {
                using (new EditorGUI.DisabledScope(true))
                {
#if GENERIC_UNITY_OBJECTS
                    GenericObjectDrawer
#else
                    EditorGUI
#endif
                        .ObjectField(rect, GUIContent.none, objectTarget, objectTarget.GetType(), true);
                }

                return;
            }

            string typeFullName = @delegate.Method.DeclaringType.FullName;
            string typeName = typeFullName.EndsWith("<>c") ? typeFullName.Substring(0, typeFullName.Length - 4).GetSubstringAfterLast('.') : typeFullName.GetSubstringAfterLast('.');
            EditorGUI.LabelField(rect, typeName);
        }
    }
}