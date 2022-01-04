namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using SolidUtilities.Editor;
    using SolidUtilities.Editor.Extensions;
    using SolidUtilities.Editor.Helpers;
    using SolidUtilities.UnityEditorInternals;
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
            // argument => argumentsArray => serializedResponse => serializedResponseArray => extEvent
            var extEventProp = property.GetParent().GetParent().GetParent().GetParent();

            var argNames = GetArgNames(extEventProp);
            var indexProp = property.FindPropertyRelative(nameof(SerializedArgument.Index));
            var currentArgName = argNames[indexProp.intValue];

            var paramTypes = MemberInfoDrawer.GetEventParamTypes(extEventProp);
            var matchingArgNames = GetMatchingArgNames(argNames, paramTypes, GetTypeFromProperty(property));

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
                menu.AddItem(new GUIContent(name), indexProp.intValue == index, i => indexProp.intValue = (int) i, index);
            }

            menu.ShowAsContext();
        }

        private string[] GetArgNames(SerializedProperty extEventProperty)
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
            var type = GetTypeFromProperty(mainProperty);

            if (_valuePropertyCache.TryGetValue((mainSerializedObject, mainPropertyPath), out var valueProperty))
            {
                if (valueProperty.GetObjectType() == type)
                {
                    return valueProperty;
                }
                else
                {
                    _valuePropertyCache.Remove((mainSerializedObject, mainPropertyPath));
                    return GetValueProperty(mainProperty);
                }
            }

            var serializedValue = mainProperty.FindPropertyRelative(nameof(SerializedArgument._serializedArg)).stringValue;

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

        private static Type GetTypeFromProperty(SerializedProperty argProperty)
        {
            var typeNameAndAssembly = argProperty
                .FindPropertyRelative($"{nameof(SerializedArgument.Type)}.{nameof(TypeReference.TypeNameAndAssembly)}")
                .stringValue;
            var type = Type.GetType(typeNameAndAssembly);
            Assert.IsNotNull(type);
            return type;

        }
    }
}