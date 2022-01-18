namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using GenericUnityObjects.Editor;
    using SolidUtilities;
    using SolidUtilities.Editor;
    using TypeReferences;
    using UnityDropdown.Editor;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Events;
    using Object = UnityEngine.Object;

    [CustomPropertyDrawer(typeof(SerializedResponse))]
    public class SerializedResponsePropertyDrawer : PropertyDrawer
    {
        private const float LinePadding = 2f;

        private static readonly Dictionary<(SerializedObject serializedObject, string propertyPath), SerializedResponseInfo> _previousResponseValues = new Dictionary<(SerializedObject serializedObject, string propertyPath), SerializedResponseInfo>();

        private static GUIStyle _dropdownStyle;
        private static GUIStyle DropdownStyle => _dropdownStyle ??= new GUIStyle(EditorStyles.miniPullDown) { alignment = TextAnchor.MiddleCenter };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            const int constantLinesCount = 2;
            return (EditorGUIUtility.singleLineHeight + LinePadding) * constantLinesCount + GetSerializedArgsHeight(property);
        }

        private static float GetSerializedArgsHeight(SerializedProperty property)
        {
            if (!MethodInfoDrawer.HasMethod(property))
                return 0f;

            float serializedArgumentsHeights = 0f;

            var serializedArgsArray = property.FindPropertyRelative(nameof(SerializedResponse._serializedArguments));

            for (int i = 0; i < serializedArgsArray.arraySize; i++)
            {
                serializedArgumentsHeights += EditorGUI.GetPropertyHeight(serializedArgsArray.GetArrayElementAtIndex(i));
            }

            serializedArgumentsHeights += LinePadding * serializedArgsArray.arraySize;

            return serializedArgumentsHeights;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var currentRect = new Rect(position) { height = EditorGUIUtility.singleLineHeight };
            currentRect.y += LinePadding;
            (var callStateRect, var targetRect) = currentRect.CutVertically(50f);
            callStateRect.width -= 10f;

            DrawCallState(callStateRect, property);

            bool isStatic = property.FindPropertyRelative(nameof(SerializedResponse._isStatic)).boolValue;

            DrawTypeField(property, targetRect, isStatic);

            currentRect.y += EditorGUIUtility.singleLineHeight + LinePadding;

            // When method name is changed, we need to get methodInfo and set types of serialized arguments
            MethodInfoDrawer.Draw(currentRect, property, out var paramNames);

            bool argumentsChanged = DrawArguments(property, paramNames, currentRect);

            ReinitializeIfChanged(property, argumentsChanged);
        }

        private void DrawTypeField(SerializedProperty property, Rect rect, bool isStatic)
        {
            if (isStatic)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(nameof(SerializedResponse._type)), GUIContent.none);
                return;
            }

            var targetProp = property.FindPropertyRelative(nameof(SerializedResponse._target));
            var newTarget = GenericObjectDrawer.ObjectField(rect, GUIContent.none, targetProp.objectReferenceValue, typeof(Object), true);

            if (targetProp.objectReferenceValue != newTarget)
            {
                if (newTarget is GameObject gameObject)
                {
                    DrawComponentDropdown(targetProp, gameObject);
                }
                else
                {
                    targetProp.objectReferenceValue = newTarget;
                }
            }
        }

        private void ReinitializeIfChanged(SerializedProperty responseProperty, bool argumentsChanged)
        {
            if (argumentsChanged || MethodHasChanged(responseProperty))
            {
                responseProperty.serializedObject.ApplyModifiedProperties();
                var response = PropertyObjectCache.GetObject<SerializedResponse>(responseProperty);
                response._initialized = false;
            }
        }

        private bool DrawArguments(SerializedProperty responseProperty, List<string> paramNames, Rect rect)
        {
            var argumentsArray = responseProperty.FindPropertyRelative(nameof(SerializedResponse._serializedArguments));

            if (paramNames == null || paramNames.Count < argumentsArray.arraySize)
                return false;

            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < argumentsArray.arraySize; i++)
            {
                rect.y += EditorGUIUtility.singleLineHeight + LinePadding;
                var argumentProp = argumentsArray.GetArrayElementAtIndex(i);
                EditorGUI.PropertyField(rect, argumentProp, GUIContentHelper.Temp(paramNames[i]));
            }

            return EditorGUI.EndChangeCheck();
        }

        private static bool MethodHasChanged(SerializedProperty responseProperty)
        {
            string currentType = responseProperty.FindPropertyRelative($"{nameof(SerializedResponse._type)}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue;
            Object currentTarget = responseProperty.FindPropertyRelative(nameof(SerializedResponse._target)).objectReferenceValue;
            string currentMethodName = responseProperty.FindPropertyRelative(nameof(SerializedResponse._methodName)).stringValue;

            var serializedObject = responseProperty.serializedObject;
            var propertyPath = responseProperty.propertyPath;

            if (!_previousResponseValues.TryGetValue((serializedObject, propertyPath), out var responseInfo))
            {
                _previousResponseValues.Add((serializedObject, propertyPath), new SerializedResponseInfo(currentType, currentTarget, currentMethodName));
                return false;
            }

            bool infoChanged = false;

            if (currentType != responseInfo.TypeName)
            {
                infoChanged = true;
                responseInfo.TypeName = currentType;
            }

            if (currentTarget != responseInfo.Target)
            {
                infoChanged = true;
                responseInfo.Target = currentTarget;
            }

            if (currentMethodName != responseInfo.MethodName)
            {
                infoChanged = true;
                responseInfo.MethodName = currentMethodName;
            }

            return infoChanged;
        }

        private void DrawComponentDropdown(SerializedProperty targetProperty, GameObject gameObject)
        {
            var components = gameObject.GetComponents<Component>().Where(component => !component.hideFlags.ContainsFlag(HideFlags.HideInInspector));
            var dropdownItems = new List<DropdownItem<Component>>();

            foreach (Component component in components)
            {
                var componentType = component.GetType();
                var componentMenu = componentType.GetCustomAttribute<AddComponentMenu>();

                string componentName;

                if (componentMenu != null && ! string.IsNullOrEmpty(componentMenu.componentMenu))
                {
                    componentName = componentMenu.componentMenu.GetSubstringAfterLast('/');
                }
                else
                {
                    componentName = ObjectNames.NicifyVariableName(componentType.Name);
                }

                dropdownItems.Add(new DropdownItem<Component>(component, componentName, EditorGUIUtility.ObjectContent(component, componentType).image));
            }

            var tree = new DropdownMenu<Component>(dropdownItems, component =>
            {
                targetProperty.objectReferenceValue = component;
                targetProperty.serializedObject.ApplyModifiedProperties();
            });

            tree.ShowAsContext();
        }

        private void DrawCallState(Rect rect, SerializedProperty responseProp)
        {
            var callStateProp = responseProp.FindPropertyRelative(nameof(SerializedResponse._callState));

            if (!EditorGUI.DropdownButton(rect, GUIContentHelper.Temp(GetCallStateShortName((UnityEventCallState) callStateProp.enumValueIndex)), FocusType.Passive, DropdownStyle))
                return;

            var menu = new GenericMenu();

            foreach (UnityEventCallState state in Enum.GetValues(typeof(UnityEventCallState)))
            {
                menu.AddItem(
                    new GUIContent(GetCallStateFullName(state)),
                    callStateProp.enumValueIndex == (int) state,
                    () =>
                    {
                        callStateProp.enumValueIndex = (int) state;
                        callStateProp.serializedObject.ApplyModifiedProperties();
                    });
            }

            menu.ShowAsContext();
        }

        private string GetCallStateShortName(UnityEventCallState callState)
        {
            return callState switch
            {
                UnityEventCallState.EditorAndRuntime => "E|R",
                UnityEventCallState.RuntimeOnly => "R",
                UnityEventCallState.Off => "Off",
                _ => throw new NotImplementedException()
            };
        }

        private string GetCallStateFullName(UnityEventCallState callState)
        {
            return callState switch
            {
                UnityEventCallState.EditorAndRuntime => "Editor and Runtime",
                UnityEventCallState.RuntimeOnly => "Runtime Only",
                UnityEventCallState.Off => "Off",
                _ => throw new NotImplementedException()
            };
        }

        private class SerializedResponseInfo
        {
            public string TypeName;
            public Object Target;
            public string MethodName;

            public SerializedResponseInfo(string typeName, Object target, string methodName)
            {
                TypeName = typeName;
                Target = target;
                MethodName = methodName;
            }
        }
    }
}