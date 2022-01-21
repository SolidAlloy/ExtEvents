namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using SolidUtilities.Editor;
    using SolidUtilities.Editor.PropertyDrawers;
    using TypeReferences;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assertions;

    [CustomPropertyDrawer(typeof(SerializedArgument))]
    public class SerializedArgumentPropertyDrawer : DrawerWithModes
    {
        private static Dictionary<(SerializedObject serializedObject, string propertyPath), SerializedProperty> _valuePropertyCache =
                new Dictionary<(SerializedObject serializedObject, string propertyPath), SerializedProperty>();

        private SerializedProperty _valueProperty;
        private SerializedProperty _isSerialized;
        private SerializedProperty _serializedArgProp;

        protected override SerializedProperty ExposedProperty => _valueProperty;

        protected override bool ShouldDrawFoldout =>
            _isSerialized.boolValue && _valueProperty.propertyType == SerializedPropertyType.Generic;

        private static readonly string[] _popupOptions = {"Dynamic", "Serialized"};
        protected override string[] PopupOptions => _popupOptions;

        protected override int PopupValue
        {
            get => _isSerialized.boolValue ? 1 : 0;
            set => _isSerialized.boolValue = value == 1;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            FindProperties(property);

            if (!_isSerialized.boolValue)
                return EditorGUIUtility.singleLineHeight;

            return base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            FindProperties(property);
            ShowChoiceButton = property.FindPropertyRelative(nameof(SerializedArgument._canBeDynamic)).boolValue;
            base.OnGUI(position, property, label);
        }

        public static SerializedProperty GetValueProperty(SerializedProperty argumentProperty)
        {
            var mainSerializedObject = argumentProperty.serializedObject;
            var mainPropertyPath = argumentProperty.propertyPath;
            var type = GetTypeFromProperty(argumentProperty);

            if (_valuePropertyCache.TryGetValue((mainSerializedObject, mainPropertyPath), out var valueProperty))
            {
                if (valueProperty.GetObjectType() == type)
                {
                    return valueProperty;
                }
                else
                {
                    _valuePropertyCache.Remove((mainSerializedObject, mainPropertyPath));
                    return GetValueProperty(argumentProperty);
                }
            }

            var serializedValue = argumentProperty.FindPropertyRelative(nameof(SerializedArgument._serializedArg)).stringValue;

            object value = SerializedArgument.GetValue(serializedValue, type);
            Type soType = ScriptableObjectCache.GetClass(type);
            var so = ScriptableObject.CreateInstance(soType);
            var serializedObject = new SerializedObject(so);
            var soValueField = soType.GetField(nameof(DeserializedValueHolder<int>.Value));
            soValueField.SetValue(so, value);
            valueProperty = serializedObject.FindProperty(nameof(DeserializedValueHolder<int>.Value));
            _valuePropertyCache.Add((mainSerializedObject, mainPropertyPath), valueProperty);
            return valueProperty;
        }

        public static void SaveValueProperty(SerializedProperty argumentProperty, SerializedProperty valueProperty)
        {
            valueProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            var value = valueProperty.GetObject();
            var serializedArgProp = argumentProperty.FindPropertyRelative(nameof(SerializedArgument._serializedArg));
            serializedArgProp.stringValue = SerializedArgument.SerializeValue(value, valueProperty.GetObjectType());
        }

        protected override void DrawValue(SerializedProperty property, Rect valueRect, Rect totalRect, int indentLevel)
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
            var indexProp = property.FindPropertyRelative(nameof(SerializedArgument.Index));
            var argNames = ExtEventPropertyDrawer.CurrentEventInfo.ArgNames;
            var currentArgName = argNames[indexProp.intValue];

            var matchingArgNames = GetMatchingArgNames(argNames, ExtEventPropertyDrawer.CurrentEventInfo.ParamTypes, GetTypeFromProperty(property));

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
            _isSerialized = property.FindPropertyRelative(nameof(SerializedArgument.IsSerialized));

            if (_isSerialized.boolValue)
                _valueProperty = GetValueProperty(property);
        }

        private static Type GetTypeFromProperty(SerializedProperty argProperty)
        {
            var typeNameAndAssembly = argProperty
                .FindPropertyRelative($"{nameof(SerializedArgument.Type)}.{nameof(TypeReference._typeNameAndAssembly)}")
                .stringValue;
            var type = Type.GetType(typeNameAndAssembly);
            Assert.IsNotNull(type);
            return type;
        }
    }
}