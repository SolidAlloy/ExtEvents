namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using JetBrains.Annotations;
    using SolidUtilities.Editor.Extensions;
    using SolidUtilities.Editor.Helpers;
    using SolidUtilities.Extensions;
    using TypeReferences;
    using TypeReferences.Editor.Util;
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

        public static void Draw(Rect rect, SerializedProperty responseProperty, out List<string> argNames)
        {
            var isStatic = responseProperty.FindPropertyRelative(nameof(SerializedResponse._isStatic)).boolValue;
            var declaringType = GetDeclaringType(responseProperty, isStatic);

            var previousGuiColor = GUI.backgroundColor;

            if (!TryGetMemberName(declaringType, responseProperty, isStatic, out string memberName, out argNames))
            {
                GUI.backgroundColor = Color.red;
            }

            using (new EditorGUI.DisabledGroupScope(declaringType == null))
            {
                if (EditorGUI.DropdownButton(rect, GUIContentHelper.Temp(memberName), FocusType.Passive))
                {
                    ShowMenu(rect, declaringType, responseProperty, isStatic);
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

        private static void ShowMenu(Rect rect, Type declaringType, SerializedProperty responseProperty, bool isStatic)
        {
            if (declaringType == null)
                return;

            var eventParamTypes = GetEventParamTypes(responseProperty.GetParent().GetParent());

            var menu = new GenericMenu();

            AddStaticMethods(menu, declaringType, responseProperty, eventParamTypes);

            if (!isStatic)
                AddInstanceMethods(menu, declaringType, responseProperty, eventParamTypes);

            AddStaticProperties(menu, declaringType, responseProperty, eventParamTypes);

            if (!isStatic)
                AddInstanceProperties(menu, declaringType, responseProperty, eventParamTypes);

            AddStaticFields(menu, declaringType, responseProperty, eventParamTypes);

            if (!isStatic)
                AddInstanceFields(menu, declaringType, responseProperty, eventParamTypes);

            // menu.DropDown(rect);
            menu.ShowAsContext();
        }

        private static Type[] GetEventParamTypes(SerializedProperty extEventProperty)
        {
            var eventType = extEventProperty.GetObjectType();

            if (!eventType.IsGenericType)
                return Type.EmptyTypes;

            return eventType.GenericTypeArguments;
        }

        private static bool TryGetMemberName(Type declaringType, SerializedProperty responseProperty, bool isStatic, out string memberName, out List<string> argNames)
        {
            string currentMemberName = responseProperty.FindPropertyRelative(nameof(SerializedResponse._memberName)).stringValue;

            if (string.IsNullOrEmpty(currentMemberName))
            {
                memberName = "No Function";
                argNames = null;
                return true;
            }

            if (declaringType == null)
            {
                memberName = currentMemberName + " {Missing}";
                argNames = null;
                return false;
            }

            var serializedArgs = responseProperty.FindPropertyRelative(nameof(SerializedResponse._serializedArguments));
            var argTypes = GetTypesFromSerializedArgs(serializedArgs);

            if (argTypes == null)
            {
                memberName = currentMemberName + " {Missing}";
                argNames = null;
                return false;
            }

            var flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            var memberTypeProp = responseProperty.FindPropertyRelative(nameof(SerializedResponse._memberType));

            if (!HasMember(declaringType, currentMemberName, flags, memberTypeProp, argTypes, out argNames))
            {
                memberName = currentMemberName + " {Missing}";
                return false;
            }

            memberName = currentMemberName;
            return true;
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

        private static bool HasMember([NotNull] Type declaringType, string name, BindingFlags flags, SerializedProperty memberTypeProp, Type[] argTypes, out List<string> argNames)
        {
            var memberType = (MemberType) memberTypeProp.enumValueIndex;

            if (memberType is MemberType.Method)
            {
                var method = declaringType.GetMethod(name, flags, null, CallingConventions.Any, argTypes, null);
                argNames = method?.GetParameters().Select(param => param.Name).ToList();
                return method != null;
            }

            var returnType = argTypes[0];

            // The following code tries to search for a field if the property was not found, and vice versa. If the field was switched to property, it changes the member type.
            if (memberType is MemberType.Property)
            {
                if (HasProperty(declaringType, name, flags, returnType))
                {
                    argNames = new List<string> { name };
                    return true;
                }

                if (HasField(declaringType, name, flags, returnType))
                {
                    memberTypeProp.enumValueIndex = (int) MemberType.Field;
                    argNames = new List<string> { name };
                    return true;
                }

                argNames = null;
                return false;
            }

            if (memberType is MemberType.Field)
            {
                if (HasField(declaringType, name, flags, returnType))
                {
                    argNames = new List<string> { name };
                    return true;
                }

                if (HasProperty(declaringType, name, flags, returnType))
                {
                    memberTypeProp.enumValueIndex = (int) MemberType.Property;
                    argNames = new List<string> { name };
                    return true;
                }

                argNames = null;
                return false;
            }

            throw new NotImplementedException();
        }

        private static bool HasProperty([NotNull] Type declaringType, string name, BindingFlags flags, Type returnType)
        {
            return declaringType.GetProperty(name, flags, null, returnType, Type.EmptyTypes, null) != null;
        }

        private static bool HasField([NotNull] Type declaringType, string name, BindingFlags flags, Type returnType)
        {
            var fieldInfo = declaringType.GetField(name, flags);
            return fieldInfo != null && fieldInfo.FieldType == returnType;
        }

        private static void AddStaticMethods(GenericMenu menu, Type declaringType, SerializedProperty responseProp, Type[] eventParamTypes)
        { // the method cannot be used if it contains at least one argument that is not serializable nor it is passed from the event.
            var staticMethods = declaringType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method =>
                {
                    return !method.IsSpecialName
                           && !IsMethodPure(method)
                           && method.GetParameters().All(param => ParamCanBeUsed(param.ParameterType, eventParamTypes));
                })
                .ToList();

            if (staticMethods.Count == 0)
                return;

            const string folderName = "Static Methods";

            string[] names = GetMethodNames(staticMethods);
            Array.Sort(names, StringComparer.Ordinal);

            for (int i = 0; i < names.Length; i++)
            {
                string methodName = names[i];
                menu.AddItem(new GUIContent(folderName + "/" + methodName), false, OnMemberChosen, new MenuElement(responseProp, staticMethods[i]));
            }
        }

        private static bool IsMethodPure(MethodInfo method)
        {
            if (method.ReturnType == typeof(void))
                return false;

            return method.HasAttribute<PureAttribute>() ||
                   method.HasAttribute<System.Diagnostics.Contracts.PureAttribute>();
        }

        private static void AddInstanceMethods(GenericMenu menu, Type declaringType, SerializedProperty responseProp, Type[] eventParamTypes)
        {
            var instanceMethods = declaringType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method =>
                {
                    return !method.IsSpecialName
                           && !IsMethodPure(method)
                           && method.GetParameters().All(param => ParamCanBeUsed(param.ParameterType, eventParamTypes));
                })
                .ToList();

            if (instanceMethods.Count == 0)
                return;

            const string folderName = "Instance Methods";

            var methodsAndNames = instanceMethods
                .Zip(GetMethodNames(instanceMethods), (method, name) => (method, name))
                .OrderBy(methodAndName => methodAndName.name, StringComparer.Ordinal).ToList();

            foreach ((var method, var name) in methodsAndNames)
            {
                menu.AddItem(new GUIContent(folderName + "/" + name), false, OnMemberChosen, new MenuElement(responseProp, method));
            }
        }

        private static void AddStaticProperties(GenericMenu menu, Type declaringType, SerializedProperty responseProp, Type[] eventParamTypes)
        {
            var staticProperties = declaringType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(prop => prop.CanWrite && ParamCanBeUsed(prop.PropertyType, eventParamTypes))
                .Sort().ToList();

            if (staticProperties.Count == 0)
                return;

            const string folderName = "Static Properties";

            foreach (PropertyInfo property in staticProperties)
            {
                menu.AddItem(new GUIContent(folderName + "/" + property.Name), false, OnMemberChosen, new MenuElement(responseProp, property));
            }
        }

        private static bool ParamCanBeUsed(Type paramType, Type[] eventParamTypes)
        {
            return paramType.IsUnitySerializable() || ArgumentTypeIsInList(paramType, eventParamTypes);
        }

        private static void AddInstanceProperties(GenericMenu menu, Type declaringType, SerializedProperty responseProp, Type[] eventParamTypes)
        {
            var instanceProperties = declaringType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.CanWrite && ParamCanBeUsed(prop.PropertyType, eventParamTypes))
                .Sort().ToList();

            if (instanceProperties.Count == 0)
                return;

            const string folderName = "Instance Properties";

            foreach (PropertyInfo property in instanceProperties)
            {
                menu.AddItem(new GUIContent(folderName + "/" + property.Name), false, OnMemberChosen, new MenuElement(responseProp, property));
            }
        }

        private static void AddStaticFields(GenericMenu menu, Type declaringType, SerializedProperty responseProp, Type[] eventParamTypes)
        {
            var staticFields = declaringType.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => ParamCanBeUsed(field.FieldType, eventParamTypes))
                .Sort().ToList();

            if (staticFields.Count == 0)
                return;

            const string folderName = "Static Fields";

            foreach (var field in staticFields)
            {
                menu.AddItem(new GUIContent(folderName + "/" + field.Name), false, OnMemberChosen, new MenuElement(responseProp, field));
            }
        }

        private static void AddInstanceFields(GenericMenu menu, Type declaringType, SerializedProperty responseProp, Type[] eventParamTypes)
        {
            var instanceFields = declaringType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(field => ParamCanBeUsed(field.FieldType, eventParamTypes))
                .Sort().ToList();

            if (instanceFields.Count == 0)
                return;

            const string folderName = "Instance Fields";

            foreach (var field in instanceFields)
            {
                menu.AddItem(new GUIContent(folderName + "/" + field.Name), false, OnMemberChosen, new MenuElement(responseProp, field));
            }
        }

        private static void OnMemberChosen(object menuElement)
        {
            var typedElement = (MenuElement) menuElement;
            var memberInfo = typedElement.MemberInfo;
            var responseProperty = typedElement.ResponseProperty;

            var memberNameProp = responseProperty.FindPropertyRelative(nameof(SerializedResponse._memberName));
            var serializedArgsProp = responseProperty.FindPropertyRelative(nameof(SerializedResponse._serializedArguments));
            var memberTypeProp = responseProperty.FindPropertyRelative(nameof(SerializedResponse._memberType));

            memberNameProp.stringValue = memberInfo.Name;

            var eventParamTypes = GetEventParamTypes(responseProperty);

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

            if (matchingParamFound)
                argumentProp.FindPropertyRelative(nameof(SerializedArgument.Index)).intValue = matchingParamIndex;
        }

        private static bool ArgumentTypeIsInList(Type argType, Type[] eventParamTypes)
        {
            return eventParamTypes.Any(eventParamType => eventParamType.IsAssignableFrom(argType));
        }

        private static IEnumerable<T> Sort<T>(this IEnumerable<T> members)
            where T : MemberInfo
        {
            return members.OrderBy(member => member.Name, StringComparer.Ordinal);
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

        private class MenuElement
        {
            public readonly SerializedProperty ResponseProperty;
            public readonly MemberInfo MemberInfo;

            public MenuElement(SerializedProperty responseProperty, MemberInfo memberInfo)
            {
                ResponseProperty = responseProperty;
                MemberInfo = memberInfo;
            }
        }
    }
}