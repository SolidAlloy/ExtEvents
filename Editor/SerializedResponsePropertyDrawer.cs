namespace ExtEvents.Editor
{
    using System;
    using SolidUtilities.Editor.Helpers;
    using SolidUtilities.Extensions;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Events;
    using Object = UnityEngine.Object;

    [CustomPropertyDrawer(typeof(SerializedResponse))]
    public class SerializedResponsePropertyDrawer : PropertyDrawer
    {
        private const float LinePadding = 2f;

        private static GUIStyle _dropdownStyle;
        private static GUIStyle DropdownStyle => _dropdownStyle ??= new GUIStyle(EditorStyles.miniPullDown) { alignment = TextAnchor.MiddleCenter };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var linesNum = 2 + property.FindPropertyRelative(nameof(SerializedResponse._serializedArguments)).arraySize;
            return EditorGUIUtility.singleLineHeight * linesNum + LinePadding * (linesNum);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var currentRect = new Rect(position) { height = EditorGUIUtility.singleLineHeight };

            currentRect.y += LinePadding;

            (var callStateRect, var targetRect) = currentRect.CutVertically(50f);

            callStateRect.width -= 10f;

            DrawCallState(callStateRect, property);

            bool isStatic = property.FindPropertyRelative(nameof(SerializedResponse._isStatic)).boolValue;

            if (isStatic)
            {
                EditorGUI.PropertyField(targetRect, property.FindPropertyRelative(nameof(SerializedResponse._type)), GUIContent.none);
            }
            else
            {
                var targetProp = property.FindPropertyRelative(nameof(SerializedResponse._target));
                var newTarget = EditorGUI.ObjectField(targetRect, targetProp.objectReferenceValue, typeof(Object), true);

                if (targetProp.objectReferenceValue != newTarget)
                {
                    targetProp.objectReferenceValue = newTarget;
                    Debug.Log(newTarget is GameObject);
                    // TODO if gameObject, open a generic menu to specify the component.
                }
            }

            currentRect.y += EditorGUIUtility.singleLineHeight + LinePadding;

            // When member name is changed, we need to get memberinfo and set types of serialized arguments
            MemberInfoDrawer.Draw(currentRect, property, out var paramNames);

            var argumentsArray = property.FindPropertyRelative(nameof(SerializedResponse._serializedArguments));

            if (paramNames == null || paramNames.Count < argumentsArray.arraySize)
                return;

            for (int i = 0; i < argumentsArray.arraySize; i++)
            {
                currentRect.y += EditorGUIUtility.singleLineHeight + LinePadding;
                var argumentProp = argumentsArray.GetArrayElementAtIndex(i);
                EditorGUI.PropertyField(currentRect, argumentProp, GUIContentHelper.Temp(paramNames[i]));
            }
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
    }
}