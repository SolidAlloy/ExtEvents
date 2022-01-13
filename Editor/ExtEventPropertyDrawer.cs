namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using SolidUtilities.Editor;
    using SolidUtilities.UnityEditorInternals;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.Assertions;
    using UnityEngine.Events;

    [CustomPropertyDrawer(typeof(BaseExtEvent), true)]
    public class ExtEventPropertyDrawer : PropertyDrawer
    {
        private static readonly Dictionary<(SerializedObject, string), ExtEventInfo> _extEventInfoCache =
            new Dictionary<(SerializedObject, string), ExtEventInfo>();

        private static readonly Dictionary<(SerializedObject, string), ReorderableList> _listCache =
            new Dictionary<(SerializedObject, string), ReorderableList>();

        public static ExtEventInfo CurrentEventInfo { get; private set; }

        private static Action<ReorderableList> _clearCache;
        private static Action<ReorderableList> ClearCache
        {
            get
            {
                if (_clearCache == null)
                {
                    var clearCacheMethod = typeof(ReorderableList).GetMethod("ClearCache", BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.IsNotNull(clearCacheMethod);
                    _clearCache = (Action<ReorderableList>) Delegate.CreateDelegate(typeof(Action<ReorderableList>), clearCacheMethod);
                }

                return _clearCache;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var reorderableList = GetList(property, label);
            return reorderableList.GetHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CurrentEventInfo = GetExtEventInfo(property);
            var reorderableList = GetList(property, label);
            reorderableList.DoList(position);
        }

        public static void ClearListCache(SerializedProperty responsesArrayProp)
        {
            var list = GetList(responsesArrayProp, null);
            ClearCache(list);
        }

        private static ExtEventInfo GetExtEventInfo(SerializedProperty extEventProperty)
        {
            var serializedObject = extEventProperty.serializedObject;
            var propertyPath = extEventProperty.propertyPath;

            if (_extEventInfoCache.TryGetValue((serializedObject, propertyPath), out var eventInfo))
                return eventInfo;

            eventInfo = new ExtEventInfo(GetArgNames(extEventProperty), GetEventParamTypes(extEventProperty));
            _extEventInfoCache.Add((extEventProperty.serializedObject, extEventProperty.propertyPath), eventInfo);
            return eventInfo;
        }

        private static ReorderableList GetList(SerializedProperty extEventProperty, GUIContent label)
        {
            if (_listCache.TryGetValue((extEventProperty.serializedObject, extEventProperty.propertyPath), out var list))
                return list;

            var responsesProperty = extEventProperty.FindPropertyRelative(nameof(ExtEvent._responses));

            var reorderableList = new ReorderableList(extEventProperty.serializedObject, responsesProperty)
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

            _listCache.Add((extEventProperty.serializedObject, extEventProperty.propertyPath), reorderableList);
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