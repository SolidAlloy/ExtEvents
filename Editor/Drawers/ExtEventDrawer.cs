namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using SolidUtilities.Editor;
    using SolidUtilities.UnityEditorInternals;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Events;

    [CustomPropertyDrawer(typeof(BaseExtEvent), true)]
    public class ExtEventDrawer : PropertyDrawer
    {
        private static readonly Dictionary<(SerializedObject, string), ExtEventInfo> _extEventInfoCache =
            new Dictionary<(SerializedObject, string), ExtEventInfo>();

        private static readonly Dictionary<(SerializedObject, string), FoldoutList> _listCache =
            new Dictionary<(SerializedObject, string), FoldoutList>();

        public static ExtEventInfo CurrentEventInfo { get; private set; }

        private static string[] _overrideArgNames;

        public static void SetOverrideArgNames(string[] overrideArgNames) => _overrideArgNames = overrideArgNames;

        public static void ResetOverrideArgNames() => _overrideArgNames = null;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var reorderableList = GetList(property, label.text);
            return reorderableList.GetHeight() + DynamicListenersDrawer.GetHeight(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CurrentEventInfo = GetExtEventInfo(property);
            var reorderableList = GetList(property, label.text);
            float listHeight = reorderableList.GetHeight();
            reorderableList.DoList(new Rect(position) { height = listHeight });

            DynamicListenersDrawer.DrawListeners(property, position, listHeight);
        }

        public static void ResetListCache(SerializedProperty extEventProp) => GetList(extEventProp, null).ResetCache();

        public static ExtEventInfo GetExtEventInfo(SerializedProperty extEventProperty)
        {
            var serializedObject = extEventProperty.serializedObject;
            var propertyPath = extEventProperty.propertyPath;

            if (_extEventInfoCache.TryGetValue((serializedObject, propertyPath), out var eventInfo))
            {
                if (_overrideArgNames != null)
                    eventInfo.ArgNames = _overrideArgNames;

                return eventInfo;
            }

            eventInfo = new ExtEventInfo(GetArgNames(extEventProperty), GetEventParamTypes(extEventProperty));
            _extEventInfoCache.Add((extEventProperty.serializedObject, extEventProperty.propertyPath), eventInfo);

            if (_overrideArgNames != null)
                eventInfo.ArgNames = _overrideArgNames;

            return eventInfo;
        }

        private static FoldoutList.ButtonData GetStaticButton(SerializedProperty listenersProperty)
        {
            return new FoldoutList.ButtonData(new Vector2(29f, 16f),
                new GUIContent(EditorIcons.AddButtonS.Default, "Add static listener"), true,
                (rect, list) => AddListener(listenersProperty, true));
        }

        private static FoldoutList.ButtonData GetInstanceButton(SerializedProperty listenersProperty)
        {
            return new FoldoutList.ButtonData(new Vector2(25f, 16f),
                new GUIContent(EditorIcons.AddButtonI.Default, "Add instance listener"), true,
                (rect, list) => AddListener(listenersProperty, false));
        }

        private static FoldoutList GetList(SerializedProperty extEventProperty, string label)
        {
            if (_listCache.TryGetValue((extEventProperty.serializedObject, extEventProperty.propertyPath), out var list))
                return list;

            var listenersProperty = extEventProperty.FindPropertyRelative(nameof(ExtEvent._persistentListeners));

            var reorderableList = new FoldoutList(listenersProperty, label, extEventProperty.FindPropertyRelative(nameof(BaseExtEvent.Expanded)))
            {
                DrawElementCallback = (rect, index) => EditorGUI.PropertyField(rect, listenersProperty.GetArrayElementAtIndex(index)),
                ElementHeightCallback = index =>
                {
                    // A fix for a bug in ReorderableList where it calls ElementHeightCallback with index 0 even though there are no elements in the list.
                    if (listenersProperty.arraySize == 0)
                        return 21f;

                    return EditorGUI.GetPropertyHeight(listenersProperty.GetArrayElementAtIndex(index));
                },
                DrawFooterCallback = (rect, list) =>
                {
                    // ReorderableList.defaultBehaviours.DrawFooter(rect, list._list);
                    FoldoutList.DrawFooter(rect, list, GetStaticButton(listenersProperty), GetInstanceButton(listenersProperty), FoldoutList.DefaultRemoveButton);
                }
            };

            _listCache.Add((extEventProperty.serializedObject, extEventProperty.propertyPath), reorderableList);
            return reorderableList;
        }

        private static void AddListener(SerializedProperty listenersProperty, bool isStatic)
        {
            listenersProperty.arraySize++;
            var prevElement = listenersProperty.arraySize == 1f ? null : listenersProperty.GetArrayElementAtIndex(listenersProperty.arraySize - 2);
            var lastElement = listenersProperty.GetArrayElementAtIndex(listenersProperty.arraySize - 1);

            bool? isPrevStatic = prevElement?.FindPropertyRelative(nameof(PersistentListener._isStatic)).boolValue;
            var isStaticProp = lastElement.FindPropertyRelative(nameof(PersistentListener._isStatic));
            isStaticProp.boolValue = isStatic;

            // if the previous and new listeners are both static, the new listener will just be a copy of the previous one: with the same type and method.
            // But if the two listeners have different static values, the method name will show up as missing in the new listener and we don't want that,
            // so we just set the method to empty so that "No Function" appears in the UI.
            if (isPrevStatic != null && isPrevStatic.Value != isStatic)
                lastElement.FindPropertyRelative(nameof(PersistentListener._methodName)).stringValue = string.Empty;

            if (listenersProperty.arraySize == 1)
            {
                var callStateProp = lastElement.FindPropertyRelative(nameof(PersistentListener.CallState));

                // This should be set in the class constructor, but it is not called when an element is added through serialized property.
                // We only need this set for the first element in list. All other listeners will copy the value of the previous element.
                callStateProp.enumValueIndex = (int) UnityEventCallState.RuntimeOnly;
            }

            listenersProperty.serializedObject.ApplyModifiedProperties();
        }

        private static string[] GetArgNames(SerializedProperty extEventProperty)
        {
            (var fieldInfo, var extEventType) = extEventProperty.GetFieldInfoAndType();
            int argumentsCount = extEventType.GenericTypeArguments.Length;
            string[] attributeArgNames = fieldInfo.GetCustomAttribute<EventArgumentsAttribute>()?.ArgumentNames ??
                                         Array.Empty<string>();
            var argNames = new string[argumentsCount];

            Array.Copy(attributeArgNames, argNames, Mathf.Min(attributeArgNames.Length, argNames.Length));

            if (argNames.Length > attributeArgNames.Length)
            {
                for (int i = attributeArgNames.Length; i < argNames.Length; i++)
                {
                    argNames[i] = $"Arg{i+1}";
                }
            }

            return argNames;
        }

        private static Type[] GetEventParamTypes(SerializedProperty extEventProperty)
        {
            var eventType = extEventProperty.GetObjectType();

            if (!eventType.IsGenericType)
                return Type.EmptyTypes;

            return eventType.GenericTypeArguments;
        }
    }

    public class ExtEventInfo
    {
        public string[] ArgNames;
        public readonly Type[] ParamTypes;

        public ExtEventInfo(string[] argNames, Type[] paramTypes)
        {
            ArgNames = argNames;
            ParamTypes = paramTypes;
        }
    }
}