namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using SolidUtilities;
    using SolidUtilities.Editor;
    using TypeReferences;
    using UnityEditor;
    using UnityEngine.Events;

    public static class ExtEventProjectSearcher
    {
        public static IEnumerable<MethodInfo> GetMethods(IEnumerable<SerializedProperty> extEventProperties)
        {
            foreach (var extEventProperty in extEventProperties)
            {
                var responses = extEventProperty.FindPropertyRelative(nameof(BaseExtEvent._persistentListeners));

                int responsesLength = responses.arraySize;

                for (int i = 0; i < responsesLength; i++)
                {
                    var method = ResponseHelper.GetMethod(responses.GetArrayElementAtIndex(i));

                    if (method != null)
                        yield return method;
                }
            }
        }

        private static class ResponseHelper
        {
            public static MethodInfo GetMethod(SerializedProperty response)
            {
                if ((UnityEventCallState) response.FindPropertyRelative(nameof(PersistentListener.CallState)).enumValueIndex == UnityEventCallState.Off)
                    return null;
                
                string methodName = response.FindPropertyRelative(nameof(PersistentListener._methodName)).stringValue;

                if (string.IsNullOrEmpty(methodName))
                    return null;

                bool isStatic = response.FindPropertyRelative(nameof(PersistentListener._isStatic)).boolValue;
                
                var declaringType = GetDeclaringType(response, isStatic);
                if (declaringType == null)
                    return null;

                var argumentTypes = GetArgumentTypes(response);
                if (argumentTypes == null)
                    return null;

                return PersistentListener.GetMethod(declaringType, argumentTypes, methodName, PersistentListener.GetFlags(isStatic));
            }
            
            private static Type GetDeclaringType(SerializedProperty response, bool isStatic)
            {
                if (isStatic)
                {
                    string typeNameAndAssembly = response.FindPropertyRelative($"{nameof(PersistentListener._staticType)}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue;
                    return string.IsNullOrEmpty(typeNameAndAssembly) ? null : Type.GetType(typeNameAndAssembly);
                }

                var target = response.FindPropertyRelative(nameof(PersistentListener._target)).objectReferenceValue;
                // ReSharper disable once Unity.NoNullPropagation
                return target?.GetType();
            }

            private static Type[] GetArgumentTypes(SerializedProperty response)
            {
                var arguments = response.FindPropertyRelative(nameof(PersistentListener._persistentArguments));
                int argumentsCount = arguments.arraySize;
                var types = new Type[argumentsCount];

                for (int i = 0; i < argumentsCount; i++)
                {
                    var typeNameAndAssembly = arguments.GetArrayElementAtIndex(i).FindPropertyRelative($"{nameof(PersistentArgument._type)}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue;
                    var type = Type.GetType(typeNameAndAssembly);
                    if (type == null)
                        return null;

                    types[i] = type;
                }

                return types;
            }
        }
    }
}