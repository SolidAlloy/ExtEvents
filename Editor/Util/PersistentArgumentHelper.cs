namespace ExtEvents.Editor
{
    using System;
    using TypeReferences;
    using UnityEditor;
    using UnityEngine.Assertions;

    public static class PersistentArgumentHelper
    {
        public static Type GetTypeFromProperty(SerializedProperty argProperty, string typeFieldName, string fallbackTypeFieldName = null)
        {
            var type = GetTypeFromPropertyInternal(argProperty, typeFieldName);

            if (type == null && fallbackTypeFieldName != null)
            {
                type = GetTypeFromPropertyInternal(argProperty, fallbackTypeFieldName);
            }

            return type;
        }

        private static Type GetTypeFromPropertyInternal(SerializedProperty argProperty, string typeFieldName)
        {
            var typeNameAndAssembly = argProperty.FindPropertyRelative($"{typeFieldName}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue;
            return Type.GetType(typeNameAndAssembly);
        }
    }
}