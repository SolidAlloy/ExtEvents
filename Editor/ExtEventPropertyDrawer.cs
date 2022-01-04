namespace ExtEvents.Editor
{
    using System.Collections.Generic;
    using System.Reflection;
    using SolidUtilities.Editor.Extensions;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.Events;

    [CustomPropertyDrawer(typeof(BaseExtEvent), true)]
    public class ExtEventPropertyDrawer : PropertyDrawer
    {
        // TODO: check if SerializedProperty is saved between draw calls, so that we can use it in a dictionary
        private static readonly Dictionary<(SerializedObject, string), ReorderableList> _listCache =
            new Dictionary<(SerializedObject, string), ReorderableList>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var reorderableList = GetList(property, label);
            return reorderableList.GetHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var reorderableList = GetList(property, label);
            reorderableList.DoList(position);
        }

        public static void ClearListCache(SerializedProperty responseProperty)
        {
            var extEventProperty = responseProperty.GetParent().GetParent();
            var list = GetList(extEventProperty, null);
            var clearCacheMethod = typeof(ReorderableList).GetMethod("ClearCache", BindingFlags.Instance | BindingFlags.NonPublic);
            clearCacheMethod.Invoke(list, null);
        }

        private static ReorderableList GetList(SerializedProperty property, GUIContent label)
        {
            if (_listCache.TryGetValue((property.serializedObject, property.propertyPath), out var list))
                return list;

            var responsesProperty = property.FindPropertyRelative(nameof(ExtEvent._responses));

            var reorderableList = new ReorderableList(property.serializedObject, responsesProperty)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, label),
                drawElementCallback = (rect, index, _, _) =>
                    EditorGUI.PropertyField(rect, responsesProperty.GetArrayElementAtIndex(index)),
                elementHeightCallback = index =>
                    EditorGUI.GetPropertyHeight(responsesProperty.GetArrayElementAtIndex(index)),
                onAddDropdownCallback = (_, _) =>
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Instance"), false, () => AddResponse(responsesProperty, false));
                    menu.AddItem(new GUIContent("Static"), false, () => AddResponse(responsesProperty, true));
                    menu.ShowAsContext();
                }
            };

            _listCache.Add((property.serializedObject, property.propertyPath), reorderableList);

            return reorderableList;
        }

        private static void AddResponse(SerializedProperty responsesProperty, bool isStatic)
        {
            responsesProperty.arraySize++;
            var lastElement = responsesProperty.GetArrayElementAtIndex(responsesProperty.arraySize - 1);

            var isStaticProp = lastElement.FindPropertyRelative(nameof(SerializedResponse._isStatic));
            isStaticProp.boolValue = isStatic;

            if (responsesProperty.arraySize == 1)
            {
                var callStateProp = lastElement.FindPropertyRelative(nameof(SerializedResponse._callState));

                // This should be set in the class constructor, but it is not called when an element is added through serialized property.
                // We only need this set for the first element in list. All other responses will copy the value of the previous element.
                callStateProp.enumValueIndex = (int) UnityEventCallState.RuntimeOnly;
            }

            responsesProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}