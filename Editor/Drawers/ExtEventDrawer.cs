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
                return eventInfo;

            eventInfo = new ExtEventInfo(GetArgNames(extEventProperty), GetEventParamTypes(extEventProperty));
            _extEventInfoCache.Add((extEventProperty.serializedObject, extEventProperty.propertyPath), eventInfo);
            return eventInfo;
        }

        private static FoldoutList GetList(SerializedProperty extEventProperty, string label)
        {
            if (_listCache.TryGetValue((extEventProperty.serializedObject, extEventProperty.propertyPath), out var list))
                return list;

            var listenersProperty = extEventProperty.FindPropertyRelative(nameof(ExtEvent._persistentListeners));

            var reorderableList = new FoldoutList(listenersProperty, label, extEventProperty.FindPropertyRelative(nameof(BaseExtEvent.Expanded)))
            {
                DrawElementCallback = (rect, index) => EditorGUI.PropertyField(rect, listenersProperty.GetArrayElementAtIndex(index)),
                ElementHeightCallback = index => EditorGUI.GetPropertyHeight(listenersProperty.GetArrayElementAtIndex(index)),
                OnAddDropdownCallback = () =>
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Instance"), false, () => AddListener(listenersProperty, false));
                    menu.AddItem(new GUIContent("Static"), false, () => AddListener(listenersProperty, true)); 
                    menu.ShowAsContext();
                }
            };

            _listCache.Add((extEventProperty.serializedObject, extEventProperty.propertyPath), reorderableList);
            return reorderableList;
        }

        private static void AddListener(SerializedProperty listenersProperty, bool isStatic)
        {
            listenersProperty.arraySize++;
            var lastElement = listenersProperty.GetArrayElementAtIndex(listenersProperty.arraySize - 1);

            var isStaticProp = lastElement.FindPropertyRelative(nameof(PersistentListener._isStatic));
            isStaticProp.boolValue = isStatic;

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
        public readonly string[] ArgNames;
        public readonly Type[] ParamTypes;

        public ExtEventInfo(string[] argNames, Type[] paramTypes)
        {
            ArgNames = argNames;
            ParamTypes = paramTypes;
        }
    }
}