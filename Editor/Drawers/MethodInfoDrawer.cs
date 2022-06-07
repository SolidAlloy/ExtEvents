namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
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

        public static bool HasMethod(SerializedProperty listenerProperty)
        {
            var isStatic = listenerProperty.FindPropertyRelative(nameof(PersistentListener._isStatic)).boolValue;
            string currentMethodName = listenerProperty.FindPropertyRelative(nameof(PersistentListener._methodName)).stringValue;
            var declaringType = GetDeclaringType(listenerProperty, isStatic);
            return GetMethodInfo(declaringType, listenerProperty, isStatic, currentMethodName) != null;
        }

        public static void Draw(Rect rect, SerializedProperty listenerProperty, out List<string> argNames)
        {
            var isStatic = GetIsStatic(listenerProperty);
            var declaringType = GetDeclaringType(listenerProperty, isStatic);

            var previousGuiColor = GUI.backgroundColor;

            string currentMethodName = listenerProperty.FindPropertyRelative(nameof(PersistentListener._methodName)).stringValue;

            var methodInfo = GetMethodInfo(declaringType, listenerProperty, isStatic, currentMethodName);

            argNames = methodInfo != null ? methodInfo.GetParameters().Select(param => param.Name).ToList() : null;

            if (methodInfo == null && ! string.IsNullOrEmpty(currentMethodName))
            {
                GUI.backgroundColor = new Color(1f, 0f, 0f, .5f);
            }

            // ReSharper disable once PossibleNullReferenceException
            if (currentMethodName.IsPropertySetter())
                currentMethodName = currentMethodName.Substring(4);

            string popupLabel = string.IsNullOrEmpty(currentMethodName) ? "No Function" : (methodInfo != null ? currentMethodName : currentMethodName + " {Missing}");

            using (new EditorGUI.DisabledGroupScope(declaringType == null))
            {
                if (EditorGUI.DropdownButton(rect, GUIContentHelper.Temp(popupLabel), FocusType.Passive))
                {
                    ShowMenu(declaringType, listenerProperty, !isStatic, methodInfo);
                }
            }

            GUI.backgroundColor = previousGuiColor;
        }

        private static bool GetIsStatic(SerializedProperty listenerProperty) => listenerProperty.FindPropertyRelative(nameof(PersistentListener._isStatic)).boolValue;

        public static string GetCurrentMethodName(SerializedProperty listenerProperty)
        {
            return listenerProperty.FindPropertyRelative(nameof(PersistentListener._methodName)).stringValue;
        }

        /// <summary>
        /// Shows a method dropdown is no method was previously selected.
        /// Used for decreasing a number of clicks the user must perform to choose a method.
        /// Once a type or object is chosen, a method dropdown is open.
        /// </summary>
        /// <param name="methodRect">The rectangle of the method line.</param>
        /// <param name="listenerProperty">The listener property.</param>
        public static void ShowMethodDropdown(Rect methodRect, SerializedProperty listenerProperty)
        {
            string currentMethodName = GetCurrentMethodName(listenerProperty);

            if ( ! string.IsNullOrEmpty(currentMethodName))
                return;

            bool isStatic = GetIsStatic(listenerProperty);
            Type declaringType = GetDeclaringType(listenerProperty, isStatic);

            if (declaringType == null)
                return;

            ShowMenu(declaringType, listenerProperty, !isStatic, null, GUIUtility.GUIToScreenPoint(methodRect.position));
        }

        private static Type GetDeclaringType(SerializedProperty listenerProperty, bool isStatic)
        {
            if (!isStatic)
            {
                var target = listenerProperty.FindPropertyRelative(nameof(PersistentListener._target)).objectReferenceValue;

                if (target == null)
                    return null;

                return target.GetType();
            }

            var declaringTypeName = listenerProperty.FindPropertyRelative($"{nameof(PersistentListener._staticType)}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue;
            return Type.GetType(declaringTypeName);
        }

        private static void ShowMenu(Type declaringType, SerializedProperty listenerProperty, bool isInstance, MethodInfo currentMethod, Vector2? buttonPos = null)
        {
            if (declaringType == null)
                return;

            var menuItems = new List<DropdownItem<MethodInfo>>();

            var paramTypes = ExtEventDrawer.CurrentEventInfo.ParamTypes;

            if (isInstance)
                menuItems.AddRange(FindInstanceMethods(declaringType, paramTypes));

            menuItems.AddRange(FindStaticMethods(declaringType, paramTypes));

            SortItems(menuItems);

            var itemToSelect = menuItems.Find(menuItem => menuItem.Value == currentMethod);

            if (itemToSelect != null)
                itemToSelect.IsSelected = true;

            var dropdownMenu = new DropdownMenu<MethodInfo>(menuItems, selectedMethod => OnMethodChosen(currentMethod, selectedMethod, listenerProperty));
            dropdownMenu.ExpandAllFolders();

            if (buttonPos == null)
            {
                dropdownMenu.ShowAsContext();
            }
            else
            {
                dropdownMenu.ShowDropdown(buttonPos.Value);
            }
        }

        private static void SortItems(List<DropdownItem<MethodInfo>> items)
        {
            items.Sort((x, y) =>
            {
                // The order of folders is following:
                // - Instance Properties
                // - Instance Methods
                // - Static Properties
                // - Static Methods
                // The method names are sorted alphabetically.

                var xFolder = x.Path.GetSubstringBefore('/');
                var yFolder = y.Path.GetSubstringBefore('/');

                // If folders are the same, run an alphabetic comparison on method names
                if (xFolder == yFolder)
                    return string.Compare(x.Path.GetSubstringAfterLast('/'), y.Path.GetSubstringAfterLast('/'), StringComparison.Ordinal);

                var xFirstWord = xFolder.GetSubstringBefore(' ');
                var yFirstWord = yFolder.GetSubstringBefore(' ');

                // If the first words are different, Static must be lower in the list than Instance.
                if (xFirstWord != yFirstWord)
                    return xFirstWord == "Static" ? 1 : -1;

                var xLastWord = xFolder.GetSubstringAfterLast(' ');
                var yLastWord = yFolder.GetSubstringAfterLast(' ');

                // If the first words are equal, but last words differ, Methods must be lower in the list than Properties.
                if (xLastWord != yLastWord)
                    return xLastWord == "Methods" ? 1 : -1;

                return 0;
            });
        }

        private static MethodInfo GetMethodInfo(Type declaringType, SerializedProperty listenerProperty, bool isStatic, string currentMethodName)
        {
            if (string.IsNullOrEmpty(currentMethodName) || declaringType == null)
                return null;

            var serializedArgs = listenerProperty.FindPropertyRelative(nameof(PersistentListener._persistentArguments));
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
                types[i] = Type.GetType(serializedArgs.GetArrayElementAtIndex(i).FindPropertyRelative($"{nameof(PersistentArgument._targetType)}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue);
                if (types[i] == null)
                    return null;
            }

            return types;
        }

        private static IEnumerable<DropdownItem<MethodInfo>> FindStaticMethods(Type declaringType, Type[] eventParamTypes)
        {
            var staticMethods = GetEligibleMethods(declaringType, eventParamTypes, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return GetDropdownItems(staticMethods, "Static");
        }

        private static IEnumerable<DropdownItem<MethodInfo>> FindInstanceMethods(Type declaringType, Type[] eventParamTypes)
        {
            var instanceMethods = GetEligibleMethods(declaringType, eventParamTypes, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return GetDropdownItems(instanceMethods, "Instance");
        }

        private static IEnumerable<DropdownItem<MethodInfo>> GetDropdownItems(IEnumerable<MethodInfo> methods, string memberDescription)
        {
            foreach (MethodInfo method in methods)
            {
                bool isProperty = method.Name.IsPropertySetter();
                string methodName = GetMethodNameForDropdown(method, isProperty);
                var memberName = isProperty ? "Properties" : "Methods";
                yield return new DropdownItem<MethodInfo>(method, $"{memberDescription} {memberName}/{methodName}", searchName: methodName);
            }
        }

        private static string GetMethodNameForDropdown(MethodInfo method, bool isProperty)
        {
            if (isProperty)
                return $"{method.Name.Substring(4)} ({method.GetParameters()[0].ParameterType.Name.Beautify()})";

            return method.Name + GetParamNames(method);
        }

        private static IEnumerable<MethodInfo> GetEligibleMethods(Type declaringType, Type[] eventParamTypes,
            BindingFlags bindingFlags)
        {
            // the method cannot be used if it contains at least one argument that is not serializable nor it is passed from the event.
            return declaringType.GetMethods(bindingFlags)
                .Where(method => BaseExtEvent.MethodIsEligible(method, eventParamTypes, EditorPackageSettings.IncludeInternalMethods, EditorPackageSettings.IncludePrivateMethods));
        }

        private static void OnMethodChosen(MethodInfo previousMethod, MethodInfo newMethod, SerializedProperty listenerProperty)
        {
            if (previousMethod == newMethod)
                return;

            var methodNameProp = listenerProperty.FindPropertyRelative(nameof(PersistentListener._methodName));
            var serializedArgsProp = listenerProperty.FindPropertyRelative(nameof(PersistentListener._persistentArguments));

            methodNameProp.stringValue = newMethod.Name;
            var parameters = newMethod.GetParameters();
            serializedArgsProp.arraySize = parameters.Length;

            for (int i = 0; i < parameters.Length; i++)
            {
                var argProp = serializedArgsProp.GetArrayElementAtIndex(i);
                InitializeArgumentProperty(argProp, parameters[i].ParameterType);
            }

            PersistentListenerDrawer.Reinitialize(listenerProperty);
            ExtEventDrawer.ResetListCache(listenerProperty.GetParent().GetParent());
        }

        private static void InitializeArgumentProperty(SerializedProperty argumentProp, Type type)
        {
            // set target type
            {
                var serializedTypeRef = new SerializedTypeReference(argumentProp.FindPropertyRelative(nameof(PersistentArgument._targetType)));
                serializedTypeRef.SetType(type);

                // When an argument type is not found, there is no need to report that it's missing because the whole method definition is missing and the warning will only confuse the user.
                serializedTypeRef.SetSuppressLogs(true, false);
            }

            // Cannot rely on ExtEventPropertyDrawer.CurrentExtEvent because the initialization of argument property occurs
            // not in the middle of drawing ext events but rather after drawing all the events.
            // argument => arguments array => listener => listeners array => ext event.
            var extEventInfo = ExtEventDrawer.GetExtEventInfo(argumentProp.GetParent().GetParent().GetParent().GetParent());

            int matchingParamIndex = -1;
            bool exactMatch = false;

            for (int i = 0; i < extEventInfo.ParamTypes.Length; i++)
            {
                var eventParamType = extEventInfo.ParamTypes[i];

                if (eventParamType.IsAssignableFrom(type))
                {
                    exactMatch = true;
                    matchingParamIndex = i;
                    break;
                }

                if (Converter.ExistsForTypes(eventParamType, type))
                {
                    matchingParamIndex = i;
                    break;
                }
            }

            bool matchingParamFound = matchingParamIndex != -1;

            argumentProp.FindPropertyRelative(nameof(PersistentArgument._isSerialized)).boolValue = !matchingParamFound;
            argumentProp.FindPropertyRelative(nameof(PersistentArgument._canBeDynamic)).boolValue = matchingParamFound;

            if (matchingParamFound)
            {
                argumentProp.FindPropertyRelative(nameof(PersistentArgument._index)).intValue = matchingParamIndex;

                // If the type of event is not assignable to the type of response, make persistent argument remember
                // the type of event so that it can implicitly convert the argument when invoking the response.
                var serializedTypeRef = new SerializedTypeReference(argumentProp.FindPropertyRelative(nameof(PersistentArgument._fromType)));
                serializedTypeRef.SetType(exactMatch ? type : extEventInfo.ParamTypes[matchingParamIndex]);
            }
        }

        private static string GetParamNames(MethodInfo methodInfo)
        {
            return $"({string.Join(", ", methodInfo.GetParameters().Select(parameter => $"{parameter.ParameterType.Name.Beautify()} {parameter.Name}"))})";
        }

        private static string Beautify(this string typeName)
        {
            return _builtInTypes.TryGetValue(typeName, out string builtInName) ? builtInName : typeName;
        }
    }
}