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

    public static class MethodInfoDrawer
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

        public static bool HasMethod(SerializedProperty responseProperty)
        {
            var isStatic = responseProperty.FindPropertyRelative(nameof(SerializedResponse._isStatic)).boolValue;
            string currentMethodName = responseProperty.FindPropertyRelative(nameof(SerializedResponse._methodName)).stringValue;
            var declaringType = GetDeclaringType(responseProperty, isStatic);
            return GetMethodInfo(declaringType, responseProperty, isStatic, currentMethodName) != null;
        }

        public static void Draw(Rect rect, SerializedProperty responseProperty, out List<string> argNames)
        {
            var isStatic = responseProperty.FindPropertyRelative(nameof(SerializedResponse._isStatic)).boolValue;
            var declaringType = GetDeclaringType(responseProperty, isStatic);

            var previousGuiColor = GUI.backgroundColor;

            string currentMethodName = responseProperty.FindPropertyRelative(nameof(SerializedResponse._methodName)).stringValue;

            var methodInfo = GetMethodInfo(declaringType, responseProperty, isStatic, currentMethodName);

            if (methodInfo != null)
            {
                // argNames = methodInfo is MethodInfo method ? method.GetParameters().Select(param => param.Name).ToList() : new List<string> { currentMemberName };
                argNames = methodInfo.GetParameters().Select(param => param.Name).ToList();
            }
            else
            {
                argNames = null;
            }

            if (methodInfo == null && ! string.IsNullOrEmpty(currentMethodName))
            {
                GUI.backgroundColor = new Color(1f, 0f, 0f, .5f);
            }

            if (currentMethodName.IsPropertySetter())
                currentMethodName = currentMethodName.Substring(4);
            
            string popupLabel = string.IsNullOrEmpty(currentMethodName) ? "No Function" : (methodInfo != null ? currentMethodName : currentMethodName + " {Missing}");

            using (new EditorGUI.DisabledGroupScope(declaringType == null))
            {
                if (EditorGUI.DropdownButton(rect, GUIContentHelper.Temp(popupLabel), FocusType.Passive))
                {
                    ShowMenu(declaringType, responseProperty, !isStatic, methodInfo);
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

        private static void ShowMenu(Type declaringType, SerializedProperty responseProperty, bool isInstance, MethodInfo currentMethod)
        {
            if (declaringType == null)
                return;

            var menuItems = new List<DropdownItem<MethodInfo>>();

            var paramTypes = ExtEventPropertyDrawer.CurrentEventInfo.ParamTypes;

            menuItems.AddRange(FindStaticMethods(declaringType, paramTypes));

            if (isInstance)
                menuItems.AddRange(FindInstanceMethods(declaringType, paramTypes));
            
            var itemToSelect = menuItems.Find(menuItem => menuItem.Value == currentMethod);

            if (itemToSelect != null)
                itemToSelect.IsSelected = true;

            var dropdownMenu = new DropdownMenu<MethodInfo>(menuItems, selectedMethod => OnMethodChosen(currentMethod, selectedMethod, responseProperty), sortItems: true);
            dropdownMenu.ExpandAllFolders();
            dropdownMenu.ShowAsContext();
        }

        private static MethodInfo GetMethodInfo(Type declaringType, SerializedProperty responseProperty, bool isStatic, string currentMethodName)
        {
            if (string.IsNullOrEmpty(currentMethodName) || declaringType == null)
                return null;

            var serializedArgs = responseProperty.FindPropertyRelative(nameof(SerializedResponse._serializedArguments));
            var argTypes = GetTypesFromSerializedArgs(serializedArgs);

            if (argTypes == null)
                return null;

            return MethodInfoCache.GetItem(declaringType, currentMethodName, isStatic, argTypes);
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

        private static IEnumerable<DropdownItem<MethodInfo>> FindStaticMethods(Type declaringType, Type[] eventParamTypes)
        {
            var staticMethods = GetEligibleMethods(declaringType, eventParamTypes, BindingFlags.Public | BindingFlags.Static);
            return GetDropdownItems(staticMethods, "Static");
        }
        
        private static IEnumerable<DropdownItem<MethodInfo>> FindInstanceMethods(Type declaringType, Type[] eventParamTypes)
        {
            var staticMethods = GetEligibleMethods(declaringType, eventParamTypes, BindingFlags.Public | BindingFlags.Instance);
            return GetDropdownItems(staticMethods, "Instance");
        }

        private static IEnumerable<DropdownItem<MethodInfo>> GetDropdownItems(IEnumerable<MethodInfo> methods, string memberDescription)
        {
            foreach (MethodInfo method in methods)
            {
                bool isProperty = method.Name.IsPropertySetter();
                string methodName = isProperty ? method.Name.Substring(4) : method.Name + GetParamNames(method);
                var memberName = isProperty ? "Properties" : "Methods";
                yield return new DropdownItem<MethodInfo>(method, $"{memberDescription} {memberName}/{methodName}", searchName: methodName);
            }
        }

        private static IEnumerable<MethodInfo> GetEligibleMethods(Type declaringType, Type[] eventParamTypes,
            BindingFlags bindingFlags)
        {
            // the method cannot be used if it contains at least one argument that is not serializable nor it is passed from the event.
            return declaringType.GetMethods(bindingFlags)
                .Where(method =>
                {
                    return !method.Name.IsPropertyGetter()
                           && !IsMethodPure(method) 
                           && method.GetParameters().All(param => ParamCanBeUsed(param.ParameterType, eventParamTypes));
                });
        }

        private static bool IsMethodPure(MethodInfo method)
        {
            if (method.ReturnType == typeof(void))
                return false;

            return method.HasAttribute<PureAttribute>() ||
                   method.HasAttribute<System.Diagnostics.Contracts.PureAttribute>();
        }

        private static bool ParamCanBeUsed(Type paramType, Type[] eventParamTypes)
        {
            return paramType.IsUnitySerializable() || ArgumentTypeIsInList(paramType, eventParamTypes);
        }

        private static void OnMethodChosen(MethodInfo previousMethod, MethodInfo newMethod, SerializedProperty responseProperty)
        {
            if (previousMethod == newMethod)
                return;
            
            var methodNameProp = responseProperty.FindPropertyRelative(nameof(SerializedResponse._methodName));
            var serializedArgsProp = responseProperty.FindPropertyRelative(nameof(SerializedResponse._serializedArguments));

            methodNameProp.stringValue = newMethod.Name;
            var parameters = newMethod.GetParameters();
            serializedArgsProp.arraySize = parameters.Length;

            for (int i = 0; i < parameters.Length; i++)
            {
                var argProp = serializedArgsProp.GetArrayElementAtIndex(i);
                InitializeArgumentProperty(argProp, parameters[i].ParameterType);
            }

            SerializedResponsePropertyDrawer.Reinitialize(responseProperty);
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