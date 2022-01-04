namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using SolidUtilities.Editor;
    using SolidUtilities.Editor.Extensions;
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
            base.OnGUI(position, property, label);
        }

        protected override void DrawValue(SerializedProperty property, Rect valueRect, Rect totalRect, int indentLevel)
        {
            if (_isSerialized.boolValue)
            {
                DrawSerializedValue(property, valueRect, totalRect, indentLevel);
            }
            else
            {
                // TODO: draw dropdown with arguments choice
            }
        }

        private void DrawSerializedValue(SerializedProperty property, Rect valueRect, Rect totalRect, int indentLevel)
        {
            _valueProperty.serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            DrawValueProperty(property, valueRect, totalRect, indentLevel);
            if (EditorGUI.EndChangeCheck())
            {
                _valueProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                var value = _valueProperty.GetObject();
                var serializedArgProp = property.FindPropertyRelative(nameof(SerializedArgument._serializedArg));
                serializedArgProp.stringValue = SerializedArgument.SerializeValue(value, _valueProperty.GetObjectType());
            }
        }

        private void FindProperties(SerializedProperty property)
        {
            _isSerialized = property.FindPropertyRelative(nameof(SerializedArgument.IsSerialized));

            if (_isSerialized.boolValue)
                _valueProperty = GetValueProperty(property);
        }

        private static SerializedProperty GetValueProperty(SerializedProperty mainProperty)
        {
            var mainSerializedObject = mainProperty.serializedObject;
            var mainPropertyPath = mainProperty.propertyPath;

            if (_valuePropertyCache.TryGetValue((mainSerializedObject, mainPropertyPath), out var valueProperty))
                return valueProperty;

            var serializedValue = mainProperty.FindPropertyRelative(nameof(SerializedArgument._serializedArg))
                .stringValue;
            var typeNameAndAssembly = mainProperty
                .FindPropertyRelative($"{nameof(SerializedArgument.Type)}.{nameof(TypeReference.TypeNameAndAssembly)}")
                .stringValue;
            var type = Type.GetType(typeNameAndAssembly);
            Assert.IsNotNull(type);

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
    }
}