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
    using UnityEngine.UI;

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
            var isStatic = listenerProperty.FindPropertyRelative(nameof(PersistentListener._isStatic)).boolValue;
            var declaringType = GetDeclaringType(listenerProperty, isStatic);

            var previousGuiColor = GUI.backgroundColor;

            string currentMethodName = listenerProperty.FindPropertyRelative(nameof(PersistentListener._methodName)).stringValue;

            var methodInfo = GetMethodInfo(declaringType, listenerProperty, isStatic, currentMethodName);

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
                    ShowMenu(declaringType, listenerProperty, !isStatic, methodInfo);
                }
            }

            GUI.backgroundColor = previousGuiColor;
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

        private static void ShowMenu(Type declaringType, SerializedProperty listenerProperty, bool isInstance, MethodInfo currentMethod)
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
            dropdownMenu.ShowAsContext();
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
                types[i] = Type.GetType(serializedArgs.GetArrayElementAtIndex(i).FindPropertyRelative($"{nameof(PersistentArgument._type)}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue);
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
            var staticMethods = GetEligibleMethods(declaringType, eventParamTypes, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
                .Where(method => ExtEventHelper.MethodIsEligible(method, eventParamTypes, EditorPackageSettings.IncludeInternalMethods, EditorPackageSettings.IncludePrivateMethods));
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
            var serializedTypeRef = new SerializedTypeReference(argumentProp.FindPropertyRelative(nameof(PersistentArgument._type)));
            serializedTypeRef.SetType(type);

            // Cannot rely on ExtEventPropertyDrawer.CurrentExtEvent because the initialization of argument property occurs
            // not in the middle of drawing ext events but rather after drawing all the events.
            // argument => arguments array => listener => listeners array => ext event.
            var extEventInfo = ExtEventDrawer.GetExtEventInfo(argumentProp.GetParent().GetParent().GetParent().GetParent());

            int matchingParamIndex = Array.FindIndex(extEventInfo.ParamTypes, eventParamType => eventParamType.IsAssignableFrom(type));
            bool matchingParamFound = matchingParamIndex != -1;

            argumentProp.FindPropertyRelative(nameof(PersistentArgument._isSerialized)).boolValue = !matchingParamFound;
            argumentProp.FindPropertyRelative(nameof(PersistentArgument._canBeDynamic)).boolValue = matchingParamFound;

            if (matchingParamFound)
            {
                argumentProp.FindPropertyRelative(nameof(PersistentArgument._index)).intValue = matchingParamIndex;
            }
            else
            {
                // Save the default instance of a value to the string field so that the field is not empty.
                var valueProperty = PersistentArgumentDrawer.GetValueProperty(argumentProp);
                PersistentArgumentDrawer.SaveValueProperty(argumentProp, valueProperty);
            }
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