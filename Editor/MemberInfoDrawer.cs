namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using JetBrains.Annotations;
    using SolidUtilities;
    using SolidUtilities.Editor;
    using TypeReferences;
    using TypeReferences.Editor.Util;
    using UnityDropdown.Editor;
    using UnityEditor;
    using UnityEngine;

    public static class MemberInfoDrawer
    {
        private static readonly Dictionary<string, string> _builtInTypes = new Dictionary<string, string>
        {
            { "Boolean", "bool" },
            { "Byte", "byte" },
            { "SByte", "sbyte" },
            { "Char", "char" },
            { "Decimal", "decimal" },
            { "Double", "double" },
            { "Single", "float" },
            { "Int32", "int" },
            { "UInt32", "uint" },
            { "Int64", "long" },
            { "UInt64", "ulong" },
            { "Int16", "short" },
            { "UInt16", "ushort" },
            { "Object", "object" },
            { "String", "string" }
        };

        public static bool HasMember(SerializedProperty responseProperty)
        {
            var isStatic = responseProperty.FindPropertyRelative(nameof(SerializedResponse._isStatic)).boolValue;
            string currentMemberName = responseProperty.FindPropertyRelative(nameof(SerializedResponse._memberName)).stringValue;
            var declaringType = GetDeclaringType(responseProperty, isStatic);
            return GetMemberInfo(declaringType, responseProperty, isStatic, currentMemberName) != null;
        }

        public static void Draw(Rect rect, SerializedProperty responseProperty, out List<string> argNames)
        {
            var isStatic = responseProperty.FindPropertyRelative(nameof(SerializedResponse._isStatic)).boolValue;
            var declaringType = GetDeclaringType(responseProperty, isStatic);

            var previousGuiColor = GUI.backgroundColor;

            string currentMemberName = responseProperty.FindPropertyRelative(nameof(SerializedResponse._memberName)).stringValue;

            var memberInfo = GetMemberInfo(declaringType, responseProperty, isStatic, currentMemberName);

            if (memberInfo != null)
            {
                argNames = memberInfo is MethodInfo method ? method.GetParameters().Select(param => param.Name).ToList() : new List<string> { currentMemberName };
            }
            else
            {
                argNames = null;
            }

            if (memberInfo == null && ! string.IsNullOrEmpty(currentMemberName))
            {
                GUI.backgroundColor = new Color(1f, 0f, 0f, .5f);
            }

            string popupLabel = string.IsNullOrEmpty(currentMemberName) ? "No Function" : (memberInfo != null ? currentMemberName : currentMemberName + " {Missing}");

            using (new EditorGUI.DisabledGroupScope(declaringType == null))
            {
                if (EditorGUI.DropdownButton(rect, GUIContentHelper.Temp(popupLabel), FocusType.Passive))
                {
                    ShowMenu(declaringType, responseProperty, !isStatic, memberInfo);
                }
            }

            GUI.backgroundColor = previousGuiColor;
        }

        private static Type GetDeclaringType(SerializedProperty responseProperty, bool isStatic)
        {
            if (!isStatic)
            {
                var target = responseProperty.FindPropertyRelative(nameof(SerializedResponse._target)).objectReferenceValue;

                if (target == null)
                    return null;

                return target.GetType();
            }

            var declaringTypeName = responseProperty.FindPropertyRelative($"{nameof(SerializedResponse._type)}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue;
            return Type.GetType(declaringTypeName);
        }

        private static void ShowMenu(Type declaringType, SerializedProperty responseProperty, bool isInstance, MemberInfo currentMember)
        {
            if (declaringType == null)
                return;

            var menuItems = new List<DropdownItem<MemberInfo>>();

            var paramTypes = ExtEventPropertyDrawer.CurrentEventInfo.ParamTypes;

            menuItems.AddRange(FindStaticMethods(declaringType, responseProperty, paramTypes));

            if (isInstance)
                menuItems.AddRange(FindInstanceMethods(declaringType, responseProperty, paramTypes));

            menuItems.AddRange(FindStaticProperties(declaringType, responseProperty, paramTypes));

            if (isInstance)
                menuItems.AddRange(FindInstanceProperties(declaringType, responseProperty, paramTypes));

            menuItems.AddRange(FindStaticFields(declaringType, responseProperty, paramTypes));

            if (isInstance)
                menuItems.AddRange(FindInstanceFields(declaringType, responseProperty, paramTypes));

            var itemToSelect = menuItems.Find(menuItem => menuItem.Value == currentMember);

            if (itemToSelect != null)
                itemToSelect.IsSelected = true;

            var dropdownMenu = new DropdownMenu<MemberInfo>(menuItems, selectedMember => OnMemberChosen(selectedMember, responseProperty), sortItems: true);
            dropdownMenu.ExpandAllFolders();
            dropdownMenu.ShowAsContext();
        }

        private static MemberInfo GetMemberInfo(Type declaringType, SerializedProperty responseProperty, bool isStatic, string currentMemberName)
        {
            if (string.IsNullOrEmpty(currentMemberName) || declaringType == null)
                return null;

            var serializedArgs = responseProperty.FindPropertyRelative(nameof(SerializedResponse._serializedArguments));
            var argTypes = GetTypesFromSerializedArgs(serializedArgs);

            if (argTypes == null)
                return null;

            var memberTypeProp = responseProperty.FindPropertyRelative(nameof(SerializedResponse._memberType));
            var memberType = (MemberType) memberTypeProp.enumValueIndex;

            var memberInfo = MemberInfoCache.GetItem(declaringType, currentMemberName, isStatic, memberType, argTypes, out var newMemberType);

            if (memberType != newMemberType)
                memberTypeProp.enumValueIndex = (int) newMemberType;

            return memberInfo;
        }

        private static Type[] GetTypesFromSerializedArgs(SerializedProperty serializedArgs)
        {
            var types = new Type[serializedArgs.arraySize];

            for (int i = 0; i < types.Length; i++)
            {
                types[i] = Type.GetType(serializedArgs.GetArrayElementAtIndex(i).FindPropertyRelative($"{nameof(SerializedArgument.Type)}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue);
                if (types[i] == null)
                    return null;
            }

            return types;
        }

        private static DropdownItem<MemberInfo>[] FindStaticMethods(Type declaringType, SerializedProperty responseProp, Type[] eventParamTypes)
        { // the method cannot be used if it contains at least one argument that is not serializable nor it is passed from the event.
            var staticMethods = declaringType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method =>
                {
                    return !method.IsSpecialName
                           && !IsMethodPure(method)
                           && method.GetParameters().All(param => ParamCanBeUsed(param.ParameterType, eventParamTypes));
                })
                .ToList();

            return staticMethods.Count == 0
                ? Array.Empty<DropdownItem<MemberInfo>>()
                : GetDropdownItemsFromMemberInfos(staticMethods, responseProp, "Static Methods", GetMethodNames(staticMethods));
        }

        private static DropdownItem<MemberInfo>[] GetDropdownItemsFromMemberInfos(IReadOnlyList<MemberInfo> memberInfos, SerializedProperty responseProp, string folderName, string[] customMemberNames = null)
        {
            var menuElements = new DropdownItem<MemberInfo>[memberInfos.Count];

            for (int i = 0; i < menuElements.Length; i++)
            {
                var memberInfo = memberInfos[i];
                string memberName = customMemberNames?[i] ?? memberInfo.Name;
                menuElements[i] = new DropdownItem<MemberInfo>(memberInfo, folderName + "/" + memberName, searchName: memberName);
            }

            return menuElements;
        }

        private static bool IsMethodPure(MethodInfo method)
        {
            if (method.ReturnType == typeof(void))
                return false;

            return method.HasAttribute<PureAttribute>() ||
                   method.HasAttribute<System.Diagnostics.Contracts.PureAttribute>();
        }

        private static DropdownItem<MemberInfo>[] FindInstanceMethods(Type declaringType, SerializedProperty responseProp, Type[] eventParamTypes)
        {
            var instanceMethods = declaringType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method =>
                {
                    return !method.IsSpecialName
                           && !IsMethodPure(method)
                           && method.GetParameters().All(param => ParamCanBeUsed(param.ParameterType, eventParamTypes));
                })
                .ToList();

            return instanceMethods.Count == 0
                ? Array.Empty<DropdownItem<MemberInfo>>()
                : GetDropdownItemsFromMemberInfos(instanceMethods, responseProp, "Instance Methods", GetMethodNames(instanceMethods));
        }

        private static DropdownItem<MemberInfo>[] FindStaticProperties(Type declaringType, SerializedProperty responseProp, Type[] eventParamTypes)
        {
            var staticProperties = declaringType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(prop => prop.CanWrite && ParamCanBeUsed(prop.PropertyType, eventParamTypes))
                .ToList();

            return staticProperties.Count == 0
                ? Array.Empty<DropdownItem<MemberInfo>>()
                : GetDropdownItemsFromMemberInfos(staticProperties, responseProp, "Static Properties");
        }

        private static bool ParamCanBeUsed(Type paramType, Type[] eventParamTypes)
        {
            return paramType.IsUnitySerializable() || ArgumentTypeIsInList(paramType, eventParamTypes);
        }

        private static DropdownItem<MemberInfo>[] FindInstanceProperties(Type declaringType, SerializedProperty responseProp, Type[] eventParamTypes)
        {
            var instanceProperties = declaringType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.CanWrite && ParamCanBeUsed(prop.PropertyType, eventParamTypes))
                .ToList();

            return instanceProperties.Count == 0
                ? Array.Empty<DropdownItem<MemberInfo>>()
                : GetDropdownItemsFromMemberInfos(instanceProperties, responseProp, "Instance Properties");
        }

        private static DropdownItem<MemberInfo>[] FindStaticFields(Type declaringType, SerializedProperty responseProp, Type[] eventParamTypes)
        {
            var staticFields = declaringType.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => ParamCanBeUsed(field.FieldType, eventParamTypes))
                .ToList();

            return staticFields.Count == 0
                ? Array.Empty<DropdownItem<MemberInfo>>()
                : GetDropdownItemsFromMemberInfos(staticFields, responseProp, "Static Fields");
        }

        private static DropdownItem<MemberInfo>[] FindInstanceFields(Type declaringType, SerializedProperty responseProp, Type[] eventParamTypes)
        {
            var instanceFields = declaringType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(field => ParamCanBeUsed(field.FieldType, eventParamTypes))
                .ToList();

            return instanceFields.Count == 0
                ? Array.Empty<DropdownItem<MemberInfo>>()
                : GetDropdownItemsFromMemberInfos(instanceFields, responseProp, "Instance Fields");
        }

        private static void OnMemberChosen(MemberInfo memberInfo, SerializedProperty responseProperty) // remove reference to responseproperty and leave only memberinfo instead of menuelement
        {
            var memberNameProp = responseProperty.FindPropertyRelative(nameof(SerializedResponse._memberName));
            var serializedArgsProp = responseProperty.FindPropertyRelative(nameof(SerializedResponse._serializedArguments));
            var memberTypeProp = responseProperty.FindPropertyRelative(nameof(SerializedResponse._memberType));

            memberNameProp.stringValue = memberInfo.Name;

            if (memberInfo is MethodInfo method)
            {
                memberTypeProp.enumValueIndex = (int) MemberType.Method;

                var parameters = method.GetParameters();

                serializedArgsProp.arraySize = parameters.Length;

                for (int i = 0; i < parameters.Length; i++)
                {
                    var argProp = serializedArgsProp.GetArrayElementAtIndex(i);
                    InitializeArgumentProperty(argProp, parameters[i].ParameterType);
                }
            }
            else if (memberInfo is PropertyInfo property)
            {
                memberTypeProp.enumValueIndex = (int) MemberType.Property;

                serializedArgsProp.arraySize = 1;
                var argProp = serializedArgsProp.GetArrayElementAtIndex(0);
                InitializeArgumentProperty(argProp, property.PropertyType);
            }
            else if (memberInfo is FieldInfo field)
            {
                memberTypeProp.enumValueIndex = (int) MemberType.Field;

                serializedArgsProp.arraySize = 1;
                var argProp = serializedArgsProp.GetArrayElementAtIndex(0);
                InitializeArgumentProperty(argProp, field.FieldType);
            }

            responseProperty.serializedObject.ApplyModifiedProperties();
            ExtEventPropertyDrawer.ClearListCache(responseProperty.GetParent().GetParent());
        }

        private static void InitializeArgumentProperty(SerializedProperty argumentProp, Type type)
        {
            var serializedTypeRef = new SerializedTypeReference(argumentProp.FindPropertyRelative(nameof(SerializedArgument.Type)));
            serializedTypeRef.SetType(type);

            // Cannot rely on ExtEventPropertyDrawer.CurrentExtEvent because the initialization of argument property occurs
            // not in the middle of drawing ext events but rather after drawing all the events.
            // argument => arguments array => response => response array => ext event.
            var extEventInfo = ExtEventPropertyDrawer.GetExtEventInfo(argumentProp.GetParent().GetParent().GetParent().GetParent());

            int matchingParamIndex = Array.FindIndex(extEventInfo.ParamTypes, eventParamType => eventParamType.IsAssignableFrom(type));
            bool matchingParamFound = matchingParamIndex != -1;

            argumentProp.FindPropertyRelative(nameof(SerializedArgument.IsSerialized)).boolValue = !matchingParamFound;
            argumentProp.FindPropertyRelative(nameof(SerializedArgument._canBeDynamic)).boolValue = matchingParamFound;

            if (matchingParamFound)
                argumentProp.FindPropertyRelative(nameof(SerializedArgument.Index)).intValue = matchingParamIndex;
        }

        private static bool ArgumentTypeIsInList(Type argType, Type[] eventParamTypes)
        {
            return eventParamTypes.Any(eventParamType => eventParamType.IsAssignableFrom(argType));
        }

        private static string[] GetMethodNames(List<MethodInfo> methods)
        {
            int methodsCount = methods.Count;

            var methodNames = new string[methodsCount];

            for (int i = 0; i < methodsCount; i++)
            {
                methodNames[i] = methods[i].Name + GetParamNames(methods[i]);
            }

            return methodNames;
        }

        private static string GetParamNames(MethodInfo methodInfo)
        {
            return $"({string.Join(", ", methodInfo.GetParameters().Select(parameter => parameter.ParameterType.Name.Beautify()))})";
        }

        private static string Beautify(this string typeName)
        {
            return _builtInTypes.TryGetValue(typeName, out string builtInName) ? builtInName : typeName;
        }
    }
}