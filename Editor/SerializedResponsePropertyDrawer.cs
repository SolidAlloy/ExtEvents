namespace ExtEvents.Editor
{
    using TypeReferences;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(SerializedResponse))]
    public class SerializedResponsePropertyDrawer : PropertyDrawer
    {
        private const float LinePadding = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var linesNum = 3 + property.FindPropertyRelative(nameof(SerializedResponse._serializedArguments)).arraySize;
            return EditorGUIUtility.singleLineHeight * linesNum + LinePadding * (linesNum);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var currentRect = new Rect(position) { height = EditorGUIUtility.singleLineHeight };

            currentRect.y += LinePadding;

            // EditorGUI.DropdownButton(currentRect, GUIContentHelper.Temp("R"), FocusType.Passive);
            EditorGUI.PropertyField(currentRect, property.FindPropertyRelative(nameof(SerializedResponse._callState)));

            currentRect.y += EditorGUIUtility.singleLineHeight + LinePadding;

            bool isStatic = property.FindPropertyRelative(nameof(SerializedResponse._isStatic)).boolValue;

            if (isStatic)
            {
                EditorGUI.PropertyField(currentRect, property.FindPropertyRelative(nameof(SerializedResponse._type)));
            }
            else
            {
                EditorGUI.PropertyField(currentRect, property.FindPropertyRelative(nameof(SerializedResponse._target)));
            }

            currentRect.y += EditorGUIUtility.singleLineHeight + LinePadding;

            // When member name is changed, we need to get memberinfo and set types of serialized arguments
            EditorGUI.PropertyField(currentRect, property.FindPropertyRelative($"{nameof(SerializedResponse._member)}.{nameof(SerializedMember._memberName)}"));

            var argumentsArray = property.FindPropertyRelative(nameof(SerializedResponse._serializedArguments));

            for (int i = 0; i < argumentsArray.arraySize; i++)
            {
                currentRect.y += EditorGUIUtility.singleLineHeight + LinePadding;
                // var argumentProp = argumentsArray.GetArrayElementAtIndex(i);
                EditorGUI.LabelField(currentRect, $"arg {i}");
            }
        }
    }
}