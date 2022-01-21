namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using SolidUtilities;
    using TypeReferences;
    using UnityEditor;
    using UnityEngine.Events;

    public static class ExtEventHelper
    {
        public static IEnumerable<SerializedProperty> FindExtEventProperties(
            IEnumerable<SerializedObject> serializedObjects)
        {
            foreach (var serializedObject in serializedObjects)
            {
                foreach (var serializedProperty in FindExtEventProperties(serializedObject))
                {
                    yield return serializedProperty;
                }
            }
        }
        
        public static IEnumerable<SerializedProperty> FindExtEventProperties(SerializedObject serializedObject)
        {
            var prop = serializedObject.GetIterator();

            if (!prop.Next(true))
                yield break;

            do
            {
                if (prop.type.GetSubstringBefore('`') != "ExtEvent") 
                    continue;
                
                if (prop.isArray)
                {
                    int arrayLength = prop.arraySize;

                    for (int i = 0; i < arrayLength; i++)
                    {
                        yield return prop.GetArrayElementAtIndex(i);
                    }
                }
                else
                {
                    yield return prop;
                }
            }
            while (prop.NextVisible(true));
        }

        public static IEnumerable<MethodInfo> GetMethods(IEnumerable<SerializedProperty> extEventProperties)
        {
            foreach (var extEventProperty in extEventProperties)
            {
                var responses = extEventProperty.FindPropertyRelative(nameof(BaseExtEvent._responses));

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
                if ((UnityEventCallState) response.FindPropertyRelative(nameof(SerializedResponse._callState)).enumValueIndex == UnityEventCallState.Off)
                    return null;
                
                string methodName = response.FindPropertyRelative(nameof(SerializedResponse._methodName)).stringValue;

                if (string.IsNullOrEmpty(methodName))
                    return null;

                bool isStatic = response.FindPropertyRelative(nameof(SerializedResponse._isStatic)).boolValue;
                
                var declaringType = GetDeclaringType(response, isStatic);
                if (declaringType == null)
                    return null;

                var argumentTypes = GetArgumentTypes(response);
                if (argumentTypes == null)
                    return null;

                return SerializedResponse.GetMethod(declaringType, argumentTypes, methodName, SerializedResponse.GetFlags(isStatic));
            }
            
            private static Type GetDeclaringType(SerializedProperty response, bool isStatic)
            {
                if (isStatic)
                {
                    string typeNameAndAssembly = response.FindPropertyRelative($"{nameof(SerializedResponse._type)}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue;
                    return string.IsNullOrEmpty(typeNameAndAssembly) ? null : Type.GetType(typeNameAndAssembly);
                }

                var target = response.FindPropertyRelative(nameof(SerializedResponse._target)).objectReferenceValue;
                return target?.GetType();
            }

            private static Type[] GetArgumentTypes(SerializedProperty response)
            {
                var arguments = response.FindPropertyRelative(nameof(SerializedResponse._serializedArguments));
                int argumentsCount = arguments.arraySize;
                var types = new Type[argumentsCount];

                for (int i = 0; i < argumentsCount; i++)
                {
                    var typeNameAndAssembly = arguments.GetArrayElementAtIndex(i).FindPropertyRelative($"{nameof(SerializedArgument.Type)}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue;
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