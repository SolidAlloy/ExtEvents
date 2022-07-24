namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using SolidUtilities;
    using SolidUtilities.Editor;
    using SolidUtilities.UnityEditorInternals;
    using TypeReferences;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assertions;

    [CustomPropertyDrawer(typeof(PersistentArgument))]
    public class PersistentArgumentDrawer : PropertyDrawer
    {
        private static readonly Dictionary<(SerializedObject serializedObject, string propertyPath), SerializedProperty> _valuePropertyCache =
                new Dictionary<(SerializedObject serializedObject, string propertyPath), SerializedProperty>();

        private SerializedProperty _valueProperty;
        private SerializedProperty _isSerialized;
        private SerializedProperty _serializedArgProp;
        private bool _showChoiceButton = true;

        private GUIStyle _buttonStyle;
        private GUIStyle ButtonStyle => _buttonStyle ?? (_buttonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerCenter});

        private SerializedProperty ExposedProperty => _valueProperty;

        private bool ShouldDrawFoldout =>
            _isSerialized.boolValue && _valueProperty.propertyType == SerializedPropertyType.Generic;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            FindProperties(property);

            if (!_isSerialized.boolValue || ! property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            // If a property has a custom property drawer, it will be drown inside a foldout anyway, so we account for
            // it by adding a single line height.
            float additionalHeight = ExposedProperty.HasCustomPropertyDrawer() ? EditorGUIUtility.singleLineHeight : 0f;
            return EditorGUI.GetPropertyHeight(ExposedProperty, GUIContent.none) + additionalHeight;
        }

        public override void OnGUI(Rect fieldRect, SerializedProperty property, GUIContent label)
        {
            FindProperties(property);
            _showChoiceButton = property.FindPropertyRelative(nameof(PersistentArgument._canBeDynamic)).boolValue;

            (Rect labelRect, Rect buttonRect, Rect valueRect) = GetLabelButtonValueRects(fieldRect);

            DrawLabel(property, fieldRect, labelRect, label);

            // The indent level must be made 0 for the button and value to be displayed normally, without any
            // additional indent. Otherwise, the button will not be clickable, and the value will look shifted
            // compared to other fields.
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (_showChoiceButton)
                DrawChoiceButton(buttonRect, _isSerialized);

            DrawValue(property, valueRect, fieldRect, previousIndent);

            EditorGUI.indentLevel = previousIndent;
        }

        private static SerializedProperty GetValueProperty(SerializedProperty argumentProperty)
        {
            var mainSerializedObject = argumentProperty.serializedObject;
            var mainPropertyPath = argumentProperty.propertyPath;
            var type = PersistentArgumentHelper.GetTypeFromProperty(argumentProperty, nameof(PersistentArgument._targetType));
            Assert.IsNotNull(type);

            if (_valuePropertyCache.TryGetValue((mainSerializedObject, mainPropertyPath), out var valueProperty))
            {
                if (valueProperty.GetObjectType() == type)
                    return valueProperty;

                _valuePropertyCache.Remove((mainSerializedObject, mainPropertyPath));
                return GetValueProperty(argumentProperty);
            }

            Type soType = ScriptableObjectCache.GetClass(type);
            var so = ScriptableObject.CreateInstance(soType);
            var serializedObject = new SerializedObject(so);
            var soValueField = soType.GetField(nameof(DeserializedValueHolder<int>.Value));
            var value = argumentProperty.GetObject<PersistentArgument>().SerializedValue;
            soValueField.SetValue(so, value);
            valueProperty = serializedObject.FindProperty(nameof(DeserializedValueHolder<int>.Value));
            _valuePropertyCache.Add((mainSerializedObject, mainPropertyPath), valueProperty);
            return valueProperty;
        }

        private static void SaveValueProperty(SerializedProperty argumentProperty, SerializedProperty valueProperty)
        {
            valueProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            LogHelper.RemoveLogEntriesByMode(LogModes.NoScriptAssetWarning);
            var value = valueProperty.GetObject();
            var argument = argumentProperty.GetObject<PersistentArgument>();
            argument.SerializedValue = value;
        }

        private void DrawValue(SerializedProperty property, Rect valueRect, Rect totalRect, int indentLevel)
        {
            if (_isSerialized.boolValue)
            {
                DrawSerializedValue(property, valueRect, totalRect, indentLevel);
            }
            else
            {
                DrawDynamicValue(property, valueRect);
            }
        }

        private void DrawDynamicValue(SerializedProperty property, Rect valueRect)
        {
            var indexProp = property.FindPropertyRelative(nameof(PersistentArgument._index));
            var argNames = ExtEventDrawer.CurrentEventInfo.ArgNames;
            var currentArgName = argNames[indexProp.intValue];

            // fallback field for backwards compatibility where _fromType didn't exist.
            var matchingArgNames = GetMatchingArgNames(argNames, ExtEventDrawer.CurrentEventInfo.ParamTypes, PersistentArgumentHelper.GetTypeFromProperty(property, nameof(PersistentArgument._fromType), nameof(PersistentArgument._targetType)));

            using (new EditorGUI.DisabledGroupScope(matchingArgNames.Count == 1))
            {
                if (EditorGUI.DropdownButton(valueRect, GUIContentHelper.Temp(currentArgName), FocusType.Keyboard))
                {
                    ShowArgNameDropdown(matchingArgNames, indexProp);
                }
            }
        }

        private static List<(string name, int index)> GetMatchingArgNames(string[] allArgNames, Type[] argTypes, Type argType)
        {
            Assert.IsNotNull(argType);
            var matchingNames = new List<(string name, int index)>();

            for (int i = 0; i < argTypes.Length; i++)
            {
                if (argTypes[i].IsAssignableFrom(argType))
                {
                    matchingNames.Add((allArgNames[i], i));
                }
            }

            return matchingNames;
        }

        private void ShowArgNameDropdown(List<(string name, int index)> argNames, SerializedProperty indexProp)
        {
            var menu = new GenericMenu();

            foreach ((string name, int index) in argNames)
            {
                menu.AddItem(new GUIContent(name), indexProp.intValue == index, i =>
                {
                    indexProp.intValue = (int) i;
                    indexProp.serializedObject.ApplyModifiedProperties();
                }, index);
            }

            menu.ShowAsContext();
        }

        private void DrawSerializedValue(SerializedProperty property, Rect valueRect, Rect totalRect, int indentLevel)
        {
            // When the value mode is changed from dynamic to serialized, FindProperties hasn't executed yet and _valueProperty is null until the next frame.
            if (_valueProperty == null)
            {
                return;
            }

            _valueProperty.serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            DrawValueProperty(property, valueRect, totalRect, indentLevel);
            if (EditorGUI.EndChangeCheck())
            {
                SaveValueProperty(property, _valueProperty);
            }
        }

        private void FindProperties(SerializedProperty property)
        {
            _isSerialized = property.FindPropertyRelative(nameof(PersistentArgument._isSerialized));

            if (_isSerialized.boolValue)
                _valueProperty = GetValueProperty(property);
        }

        private void DrawValueProperty(SerializedProperty mainProperty, Rect valueRect, Rect totalRect, int indentLevel)
        {
            if (ExposedProperty.propertyType == SerializedPropertyType.Generic)
            {
                DrawValueInFoldout(mainProperty, ExposedProperty, totalRect, indentLevel);
            }
            else
            {
                EditorGUI.PropertyField(valueRect, ExposedProperty, GUIContent.none);
            }
        }

        private void DrawLabel(SerializedProperty property, Rect totalRect, Rect labelRect, GUIContent label)
        {
            if (ShouldDrawFoldout)
            {
                property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);
            }
            else
            {
                EditorGUI.HandlePrefixLabel(totalRect, labelRect, label);
            }
        }

        private (Rect label, Rect button, Rect value) GetLabelButtonValueRects(Rect totalRect)
        {
            const float indentWidth = 15f;
            const float valueLeftIndent = 2f;

            totalRect.height = EditorGUIUtility.singleLineHeight;

            (Rect labelAndButtonRect, Rect valueRect) = totalRect.CutVertically(EditorGUIUtility.labelWidth);

            labelAndButtonRect.xMin += EditorGUI.indentLevel * indentWidth;

            const float choiceButtonWidth = 19f;
            (Rect labelRect, Rect buttonRect) = labelAndButtonRect.CutVertically(_showChoiceButton ? choiceButtonWidth : 0f, fromRightSide: true);

            valueRect.xMin += valueLeftIndent;
            return (labelRect, buttonRect, valueRect);
        }

        private static void DrawValueInFoldout(SerializedProperty mainProperty, SerializedProperty valueProperty, Rect totalRect, int indentLevel)
        {
            valueProperty.isExpanded = mainProperty.isExpanded;

            if ( ! mainProperty.isExpanded)
                return;

            var shiftedRect = totalRect.ShiftOneLineDown(indentLevel + 1);

            if (valueProperty.HasCustomPropertyDrawer())
            {
                shiftedRect.height = EditorGUI.GetPropertyHeight(valueProperty);
                EditorGUI.PropertyField(shiftedRect, valueProperty, GUIContent.none);
                return;
            }

            // This draws all child fields of the _constantValue property with indent.
            SerializedProperty iterator = valueProperty.Copy();
            var nextProp = valueProperty.Copy();
            nextProp.NextVisible(false);
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && ! SerializedProperty.EqualContents(iterator, nextProp))
            {
                // enter children only once.
                enterChildren = false;
                shiftedRect.height = EditorGUI.GetPropertyHeight(iterator, true);
                EditorGUI.PropertyField(shiftedRect, iterator, true);
                shiftedRect = shiftedRect.ShiftOneLineDown(lineHeight: shiftedRect.height);
            }
        }

        private void DrawChoiceButton(Rect buttonRect, SerializedProperty isSerializedProperty)
        {
            if (GUI.Button(buttonRect, isSerializedProperty.boolValue ? "s" : "d", ButtonStyle))
            {
                isSerializedProperty.boolValue = !isSerializedProperty.boolValue;
            }
        }
    }
}