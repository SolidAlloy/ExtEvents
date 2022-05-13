namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using SolidUtilities;
    using SolidUtilities.Editor;
    using TypeReferences;
    using UnityDropdown.Editor;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Events;
    using Object = UnityEngine.Object;

#if GENERIC_UNITY_OBJECTS
    using GenericUnityObjects.Editor;
#endif

    [CustomPropertyDrawer(typeof(PersistentListener))]
    public class PersistentListenerDrawer : PropertyDrawer
    {
        private static readonly Dictionary<(SerializedObject serializedObject, string propertyPath), PersistentListenerInfo> _previousListenerValues = new Dictionary<(SerializedObject serializedObject, string propertyPath), PersistentListenerInfo>();

        private Rect _methodRect;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            const int constantLinesCount = 2;
            return (EditorGUIUtility.singleLineHeight + EditorPackageSettings.LinePadding) * constantLinesCount + GetSerializedArgsHeight(property);
        }

        private static float GetSerializedArgsHeight(SerializedProperty property)
        {
            if (!MethodInfoDrawer.HasMethod(property))
                return 0f;

            float persistentArgumentsHeights = 0f;

            var serializedArgsArray = property.FindPropertyRelative(nameof(PersistentListener._persistentArguments));

            for (int i = 0; i < serializedArgsArray.arraySize; i++)
            {
                persistentArgumentsHeights += EditorGUI.GetPropertyHeight(serializedArgsArray.GetArrayElementAtIndex(i));
            }

            persistentArgumentsHeights += EditorPackageSettings.LinePadding * serializedArgsArray.arraySize;

            return persistentArgumentsHeights;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var currentRect = new Rect(position) { height = EditorGUIUtility.singleLineHeight };
            currentRect.y += EditorPackageSettings.LinePadding;
            _methodRect = new Rect(currentRect) { y = currentRect.y + EditorGUIUtility.singleLineHeight + EditorPackageSettings.LinePadding };

            var callStateProp = property.FindPropertyRelative(nameof(PersistentListener.CallState));
            (var callStateRect, var targetRect) = currentRect.CutVertically(GetCallStateWidth((UnityEventCallState) callStateProp.enumValueIndex));
            callStateRect.width -= 10f;

            DrawCallState(callStateRect, callStateProp);

            bool isStatic = property.FindPropertyRelative(nameof(PersistentListener._isStatic)).boolValue;

            DrawTypeField(property, targetRect, isStatic);

            currentRect = _methodRect;

            // When method name is changed, we need to get methodInfo and set types of serialized arguments
            MethodInfoDrawer.Draw(currentRect, property, out var paramNames);

            bool argumentsChanged = DrawArguments(property, paramNames, currentRect);

            if (argumentsChanged || MethodHasChanged(property))
            {
                Reinitialize(property);
            }
        }

        private static void DrawCallState(Rect rect, SerializedProperty callStateProp)
        {
            if (!EditorGUI.DropdownButton(rect, GetCallStateContent((UnityEventCallState) callStateProp.enumValueIndex), FocusType.Passive, EditorStyles.miniPullDown))
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

        private void DrawTypeField(SerializedProperty property, Rect rect, bool isStatic)
        {
            if (isStatic)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(nameof(PersistentListener._staticType)), GUIContent.none);
                return;
            }

            var targetProp = property.FindPropertyRelative(nameof(PersistentListener._target));

            var newTarget =
#if GENERIC_UNITY_OBJECTS
                GenericObjectDrawer
#else
                EditorGUI
#endif
                    .ObjectField(rect, GUIContent.none, targetProp.objectReferenceValue, typeof(Object), true);

            if (targetProp.objectReferenceValue == newTarget)
                return;

            if (newTarget is GameObject gameObject)
            {
                DrawComponentDropdown(property, targetProp, gameObject);
            }
            else if (newTarget is Component || newTarget is ScriptableObject || newTarget is null)
            {
                targetProp.objectReferenceValue = newTarget;
                ExtEventDrawer.ResetListCache(property.GetParent().GetParent());
                MethodInfoDrawer.ShowMethodDropdown(_methodRect, property);
            }
            else
            {
                Debug.LogWarning($"Cannot assign an object of type {newTarget.GetType()} to the target field. Only GameObjects, Components, and ScriptableObjects can be assigned.");
            }
        }

        public static void Reinitialize(SerializedProperty listenerProperty)
        {
            listenerProperty.serializedObject.ApplyModifiedProperties();
            var listener = PropertyObjectCache.GetObject<PersistentListener>(listenerProperty);
            listener._initializationComplete = false;
        }

        private bool DrawArguments(SerializedProperty listenerProperty, List<string> paramNames, Rect rect)
        {
            var argumentsArray = listenerProperty.FindPropertyRelative(nameof(PersistentListener._persistentArguments));

            if (paramNames == null || paramNames.Count < argumentsArray.arraySize)
                return false;

            EditorGUI.BeginChangeCheck();

            float previousPropertyHeight = EditorGUIUtility.singleLineHeight;

            for (int i = 0; i < argumentsArray.arraySize; i++)
            {
                var argumentProp = argumentsArray.GetArrayElementAtIndex(i);

                var propertyHeight = EditorGUI.GetPropertyHeight(argumentProp);
                rect.y += previousPropertyHeight + EditorPackageSettings.LinePadding;
                rect.height = propertyHeight;
                previousPropertyHeight = propertyHeight;

                string label = EditorPackageSettings.NicifyArgumentNames ? ObjectNames.NicifyVariableName(paramNames[i]) : paramNames[i];
                EditorGUI.PropertyField(rect, argumentProp, GUIContentHelper.Temp(label));
            }

            return EditorGUI.EndChangeCheck();
        }

        private bool MethodHasChanged(SerializedProperty listenerProperty)
        {
            string currentType = listenerProperty.FindPropertyRelative($"{nameof(PersistentListener._staticType)}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue;
            Object currentTarget = listenerProperty.FindPropertyRelative(nameof(PersistentListener._target)).objectReferenceValue;
            string currentMethodName = listenerProperty.FindPropertyRelative(nameof(PersistentListener._methodName)).stringValue;

            var serializedObject = listenerProperty.serializedObject;
            var propertyPath = listenerProperty.propertyPath;

            if (!_previousListenerValues.TryGetValue((serializedObject, propertyPath), out var listenerInfo))
            {
                _previousListenerValues.Add((serializedObject, propertyPath), new PersistentListenerInfo(currentType, currentTarget, currentMethodName));
                return false;
            }

            bool infoChanged = false;

            if (currentType != listenerInfo.TypeName)
            {
                infoChanged = true;
                listenerInfo.TypeName = currentType;
                MethodInfoDrawer.ShowMethodDropdown(_methodRect, listenerProperty);
            }

            if (currentTarget != listenerInfo.Target)
            {
                infoChanged = true;
                listenerInfo.Target = currentTarget;
            }

            if (currentMethodName != listenerInfo.MethodName)
            {
                infoChanged = true;
                listenerInfo.MethodName = currentMethodName;
            }

            return infoChanged;
        }

        private void DrawComponentDropdown(SerializedProperty listenerProperty, SerializedProperty targetProperty, GameObject gameObject)
        {
            var components = gameObject
                .GetComponents<Component>()
                .Where(component => component != null && !component.hideFlags.ContainsFlag(HideFlags.HideInInspector))
                .Prepend<Object>(gameObject);

            var dropdownItems = new List<DropdownItem<Object>>();

            foreach (var component in components)
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

                dropdownItems.Add(new DropdownItem<Object>(component, componentName, EditorGUIUtility.ObjectContent(component, componentType).image));
            }

            var tree = new DropdownMenu<Object>(dropdownItems, component =>
            {
                targetProperty.objectReferenceValue = component;
                targetProperty.serializedObject.ApplyModifiedProperties();
                ExtEventDrawer.ResetListCache(listenerProperty.GetParent().GetParent());
                MethodInfoDrawer.ShowMethodDropdown(_methodRect, listenerProperty);
            });

            tree.ShowAsContext();
        }

        private static float GetCallStateWidth(UnityEventCallState callState)
        {
            return callState switch
            {
                UnityEventCallState.EditorAndRuntime => 58f,
                UnityEventCallState.RuntimeOnly => 48f,
                UnityEventCallState.Off => 58f,
                _ => throw new NotImplementedException()
            };
        }

        private static GUIContent GetCallStateContent(UnityEventCallState callState)
        {
            return callState switch
            {
                UnityEventCallState.EditorAndRuntime => GUIContentHelper.Temp("E|R", Icons.EditorRuntime),
                UnityEventCallState.RuntimeOnly => GUIContentHelper.Temp("R", Icons.Runtime),
                UnityEventCallState.Off => GUIContentHelper.Temp("Off", Icons.Off),
                _ => throw new NotImplementedException()
            };
        }

        private static string GetCallStateFullName(UnityEventCallState callState)
        {
            return callState switch
            {
                UnityEventCallState.EditorAndRuntime => "Editor and Runtime",
                UnityEventCallState.RuntimeOnly => "Runtime Only",
                UnityEventCallState.Off => "Off",
                _ => throw new NotImplementedException()
            };
        }

        private class PersistentListenerInfo
        {
            public string TypeName;
            public Object Target;
            public string MethodName;

            public PersistentListenerInfo(string typeName, Object target, string methodName)
            {
                TypeName = typeName;
                Target = target;
                MethodName = methodName;
            }
        }

        private static class Icons
        {
            private static Texture _offIcon;

            public static Texture Off
            {
                get
                {
                    if (_offIcon == null)
                    {
                        _offIcon = EditorGUIUtility.IconContent("sv_icon_dot6_sml").image;
                    }

                    return _offIcon;
                }
            }

            private static Texture _editorRuntimeIcon;

            public static Texture EditorRuntime
            {
                get
                {
                    if (_editorRuntimeIcon == null)
                    {
                        _editorRuntimeIcon = EditorGUIUtility.IconContent("sv_icon_dot4_sml").image;
                    }

                    return _editorRuntimeIcon;
                }
            }

            private static Texture _runtimeIcon;

            public static Texture Runtime
            {
                get
                {
                    if (_runtimeIcon == null)
                    {
                        _runtimeIcon = EditorGUIUtility.IconContent("sv_icon_dot3_sml").image;
                    }

                    return _runtimeIcon;
                }
            }
        }
    }
}