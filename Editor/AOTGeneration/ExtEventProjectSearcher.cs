namespace ExtEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using TypeReferences;
    using UnityEditor;
    using UnityEngine.Events;

    public static class ExtEventProjectSearcher
    {
        public static IEnumerable<SerializedProperty> GetListeners(IEnumerable<SerializedProperty> extEventProperties)
        {
            foreach (var extEventProperty in extEventProperties)
            {
                var listeners = extEventProperty.FindPropertyRelative(nameof(BaseExtEvent._persistentListeners));

                int listenersLength = listeners.arraySize;

                for (int i = 0; i < listenersLength; i++)
                {
                    yield return listeners.GetArrayElementAtIndex(i);
                }
            }
        }

        public static IEnumerable<(Type from, Type to)> GetNonMatchingArgumentTypes(SerializedProperty listener)
        {
            var arguments = listener.FindPropertyRelative(nameof(PersistentListener._persistentArguments));
            int argumentsCount = arguments.arraySize;

            for (int i = 0; i < argumentsCount; i++)
            {
                var fromType = PersistentArgumentHelper.GetTypeFromProperty(arguments.GetArrayElementAtIndex(i), nameof(PersistentArgument._fromType));
                var targetType = PersistentArgumentHelper.GetTypeFromProperty(arguments.GetArrayElementAtIndex(i), nameof(PersistentArgument._targetType));

                if (fromType == null || targetType == null || fromType == targetType)
                    continue;

                yield return (fromType, targetType);
            }
        }

        public static MethodInfo GetMethod(SerializedProperty listener)
        {
            if ((UnityEventCallState) listener.FindPropertyRelative(nameof(PersistentListener.CallState)).enumValueIndex == UnityEventCallState.Off)
                return null;

            string methodName = listener.FindPropertyRelative(nameof(PersistentListener._methodName)).stringValue;

            if (string.IsNullOrEmpty(methodName))
                return null;

            bool isStatic = listener.FindPropertyRelative(nameof(PersistentListener._isStatic)).boolValue;

            var declaringType = GetDeclaringType(listener, isStatic);
            if (declaringType == null)
                return null;

            var argumentTypes = GetArgumentTypes(listener);
            if (argumentTypes == null)
                return null;

            return PersistentListener.GetMethod(declaringType, argumentTypes, methodName, PersistentListener.GetFlags(isStatic));
        }

        private static Type GetDeclaringType(SerializedProperty listener, bool isStatic)
        {
            if (isStatic)
            {
                string typeNameAndAssembly = listener.FindPropertyRelative($"{nameof(PersistentListener._staticType)}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue;
                return string.IsNullOrEmpty(typeNameAndAssembly) ? null : Type.GetType(typeNameAndAssembly);
            }

            var target = listener.FindPropertyRelative(nameof(PersistentListener._target)).objectReferenceValue;
            // ReSharper disable once Unity.NoNullPropagation
            return target?.GetType();
        }

        private static Type[] GetArgumentTypes(SerializedProperty listener)
        {
            var arguments = listener.FindPropertyRelative(nameof(PersistentListener._persistentArguments));
            int argumentsCount = arguments.arraySize;
            var types = new Type[argumentsCount];

            for (int i = 0; i < argumentsCount; i++)
            {
                var type = PersistentArgumentHelper.GetTypeFromProperty(arguments.GetArrayElementAtIndex(i), nameof(PersistentArgument._targetType));
                if (type == null)
                    return null;

                types[i] = type;
            }

            return types;
        }
    }
}