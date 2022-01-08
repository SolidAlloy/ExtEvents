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
            return GetMemberName(declaringType, responseProperty, isStatic, currentMemberName, out _) != null;
        }

        public static void Draw(Rect rect, SerializedProperty responseProperty, out List<string> argNames)
        {
            var isStatic = responseProperty.FindPropertyRelative(nameof(SerializedResponse._isStatic)).boolValue;
            var declaringType = GetDeclaringType(responseProperty, isStatic);

            var previousGuiColor = GUI.backgroundColor;

            string currentMemberName = responseProperty.FindPropertyRelative(nameof(SerializedResponse._memberName)).stringValue;

            var memberInfo = GetMemberName(declaringType, responseProperty, isStatic, currentMemberName, out argNames);

            if (memberInfo == null)
            {
                GUI.backgroundColor = Color.red;
            }

            string popupLabel = string.IsNullOrEmpty(currentMemberName) ? "No Function" : (memberInfo != null ? currentMemberName : currentMemberName + " {Missing}");

            using (new EditorGUI.DisabledGroupScope(declaringType == null))
            {
                if (EditorGUI.DropdownButton(rect, GUIContentHelper.Temp(popupLabel), FocusType.Passive))
                {
                    ShowMenu(rect, declaringType, responseProperty, !isStatic, memberInfo);
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

            var declaringTypeName = responseProperty.FindPropertyRelative($"{nameof(SerializedResponse._type)}.{nameof(TypeReference.TypeNameAndAssembly)}").stringValue;
            return Type.GetType(declaringTypeName);
        }

        private static void ShowMenu(Rect rect, Type declaringType, SerializedProperty responseProperty, bool isInstance, MemberInfo memberInfo)
        {
            if (declaringType == null)
                return;

            var eventParamTypes = GetEventParamTypes(responseProperty.GetParent().GetParent());

            var menuItems = new List<DropdownItem<MemberInfo>>();

            menuItems.AddRange(FindStaticMethods(declaringType, responseProperty, eventParamTypes));

            if (isInstance)
                menuItems.AddRange(FindInstanceMethods(declaringType, responseProperty, eventParamTypes));

            menuItems.AddRange(FindStaticProperties(declaringType, responseProperty, eventParamTypes));

            if (isInstance)
                menuItems.AddRange(FindInstanceProperties(declaringType, responseProperty, eventParamTypes));

            menuItems.AddRange(FindStaticFields(declaringType, responseProperty, eventParamTypes));

            if (isInstance)
                menuItems.AddRange(FindInstanceFields(declaringType, responseProperty, eventParamTypes));

            var tree = new DropdownTree<MemberInfo>(menuItems, memberInfo, memberInfo => OnMemberChosen(memberInfo, responseProperty), sortItems: true);
            tree.ExpandAllFolders();
            DropdownWindow.Create(tree, DropdownWindowType.Context);
        }

        public static Type[] GetEventParamTypes(SerializedProperty extEventProperty)
        {
            var eventType = extEventProperty.GetObjectType();

            if (!eventType.IsGenericType)
                return Type.EmptyTypes;

            return eventType.GenericTypeArguments;
        }

        private static MemberInfo GetMemberName(Type declaringType, SerializedProperty responseProperty, bool isStatic, string currentMemberName, out List<string> argNames)
        {
            if (string.IsNullOrEmpty(currentMemberName) || declaringType == null)
            {
                argNames = null;
                return null;
            }

            var serializedArgs = responseProperty.FindPropertyRelative(nameof(SerializedResponse._serializedArguments));
            var argTypes = GetTypesFromSerializedArgs(serializedArgs);

            if (argTypes == null)
            {
                argNames = null;
                return null;
            }

            var flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance | BindingFlags.Static);
            var memberTypeProp = responseProperty.FindPropertyRelative(nameof(SerializedResponse._memberType));

            return GetMember(declaringType, currentMemberName, flags, memberTypeProp, argTypes, out argNames);
        }

        private static Type[] GetTypesFromSerializedArgs(SerializedProperty serializedArgs)
        {
            var types = new Type[serializedArgs.arraySize];

            for (int i = 0; i < types.Length; i++)
            {
                types[i] = Type.GetType(serializedArgs.GetArrayElementAtIndex(i).FindPropertyRelative($"{nameof(SerializedArgument.Type)}.{nameof(TypeReference.TypeNameAndAssembly)}").stringValue);
                if (types[i] == null)
                    return null;
            }

            return types;
        }

        private static MemberInfo GetMember([NotNull] Type declaringType, string name, BindingFlags flags, SerializedProperty memberTypeProp, Type[] argTypes, out List<string> argNames)
        {
            var memberType = (MemberType) memberTypeProp.enumValueIndex;

            if (memberType is MemberType.Method)
            {
                var method = declaringType.GetMethod(name, flags, null, CallingConventions.Any, argTypes, null);
                argNames = method?.GetParameters().Select(param => param.Name).ToList();
                return method;
            }

            var returnType = argTypes[0];

            // The following code tries to search for a field if the property was not found, and vice versa. If the field was switched to property, it changes the member type.
            if (memberType is MemberType.Property)
            {
                MemberInfo member = GetProperty(declaringType, name, flags, returnType);

                if (member != null)
                {
                    argNames = new List<string> { name };
                    return member;
                }

                member = GetField(declaringType, name, flags, returnType);

                if (member != null)
                {
                    memberTypeProp.enumValueIndex = (int) MemberType.Field;
                    argNames = new List<string> { name };
                    return member;
                }

                argNames = null;
                return null;
            }

            if (memberType is MemberType.Field)
            {
                MemberInfo member = GetField(declaringType, name, flags, returnType);

                if (member != null)
                {
                    argNames = new List<string> { name };
                    return member;
                }

                member = GetProperty(declaringType, name, flags, returnType);

                if (member != null)
                {
                    memberTypeProp.enumValueIndex = (int) MemberType.Property;
                    argNames = new List<string> { name };
                    return member;
                }

                argNames = null;
                return null;
            }

            throw new NotImplementedException();
        }

        private static PropertyInfo GetProperty([NotNull] Type declaringType, string name, BindingFlags flags, Type returnType)
        {
            return declaringType.GetProperty(name, flags, null, returnType, Type.EmptyTypes, null);
        }

        private static FieldInfo GetField([NotNull] Type declaringType, string name, BindingFlags flags, Type returnType)
        {
            var fieldInfo = declaringType.GetField(name, flags);
            return (fieldInfo != null && fieldInfo.FieldType == returnType) ? fieldInfo : null;
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

            var eventParamTypes = GetEventParamTypes(responseProperty.GetParent().GetParent());

            if (memberInfo is MethodInfo method)
            {
                memberTypeProp.enumValueIndex = (int) MemberType.Method;

                var parameters = method.GetParameters();

                serializedArgsProp.arraySize = parameters.Length;

                for (int i = 0; i < parameters.Length; i++)
                {
                    var argProp = serializedArgsProp.GetArrayElementAtIndex(i);
                    InitializeArgumentProperty(argProp, parameters[i].ParameterType, eventParamTypes);
                }
            }
            else if (memberInfo is PropertyInfo property)
            {
                memberTypeProp.enumValueIndex = (int) MemberType.Property;

                serializedArgsProp.arraySize = 1;
                var argProp = serializedArgsProp.GetArrayElementAtIndex(0);
                InitializeArgumentProperty(argProp, property.PropertyType, eventParamTypes);
            }
            else if (memberInfo is FieldInfo field)
            {
                memberTypeProp.enumValueIndex = (int) MemberType.Field;

                serializedArgsProp.arraySize = 1;
                var argProp = serializedArgsProp.GetArrayElementAtIndex(0);
                InitializeArgumentProperty(argProp, field.FieldType, eventParamTypes);
            }

            responseProperty.serializedObject.ApplyModifiedProperties();
            ExtEventPropertyDrawer.ClearListCache(responseProperty);
        }

        private static void InitializeArgumentProperty(SerializedProperty argumentProp, Type type, Type[] eventParamTypes)
        {
            var serializedTypeRef = new SerializedTypeReference(argumentProp.FindPropertyRelative(nameof(SerializedArgument.Type)));
            serializedTypeRef.SetType(type);

            int matchingParamIndex = Array.FindIndex(eventParamTypes, eventParamType => eventParamType.IsAssignableFrom(type));
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