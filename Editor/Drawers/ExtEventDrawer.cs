namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using GenericUnityObjects.Editor;
    using SolidUtilities;
    using SolidUtilities.Editor;
    using SolidUtilities.UnityEditorInternals;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Events;
    using Object = UnityEngine.Object;

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
            return reorderableList.GetHeight() + GetDynamicListenersHeight(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CurrentEventInfo = GetExtEventInfo(property);
            var reorderableList = GetList(property, label.text);
            float listHeight = reorderableList.GetHeight();
            reorderableList.DoList(new Rect(position) { height = listHeight });
            
            DrawDynamicListeners(property, position, listHeight);
        }

        private static float GetDynamicListenersHeight(SerializedProperty property)
        {
            bool isEventExpanded = property.FindPropertyRelative(nameof(BaseExtEvent.Expanded)).boolValue;

            if (!isEventExpanded)
                return 0f;

            var eventObject = PropertyObjectCache.GetObject<BaseExtEvent>(property);

            if (eventObject._dynamicListeners == null)
                return 0f;
            
            return (EditorGUIUtility.singleLineHeight + EditorPackageSettings.LinePadding) * (property.isExpanded ? eventObject._dynamicListeners.GetInvocationList().Length + 1 : 1); 
        }

        private static void DrawDynamicListeners(SerializedProperty extEventProperty, Rect totalRect, float listHeight)
        {
            bool isEventExpanded = extEventProperty.FindPropertyRelative(nameof(BaseExtEvent.Expanded)).boolValue;

            if (!isEventExpanded)
                return;
            
            var eventObject = PropertyObjectCache.GetObject<BaseExtEvent>(extEventProperty);
            if (eventObject._dynamicListeners == null)
                return;
            
            Rect currentRect = new Rect(totalRect) { height = EditorGUIUtility.singleLineHeight, y = totalRect.y + listHeight };

            extEventProperty.isExpanded = EditorGUI.Foldout(currentRect, extEventProperty.isExpanded, "Dynamic Listeners");

            if (!extEventProperty.isExpanded)
                return;
            
            foreach (var @delegate in eventObject._dynamicListeners.GetInvocationList())
            {
                currentRect.y += EditorGUIUtility.singleLineHeight + EditorPackageSettings.LinePadding;
                float halfWidth = currentRect.width / 2f;
                var typeRect = new Rect(currentRect) { width = halfWidth - 10f };
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
                    GenericObjectDrawer.ObjectField(rect, GUIContent.none, objectTarget, objectTarget.GetType(), true);

                return;
            }

            string typeFullName = @delegate.Method.DeclaringType.FullName;
            string typeName = typeFullName.EndsWith("<>c") ? typeFullName.Substring(0, typeFullName.Length - 4).GetSubstringAfterLast('.') : typeFullName.GetSubstringAfterLast('.');
            EditorGUI.LabelField(rect, typeName);
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