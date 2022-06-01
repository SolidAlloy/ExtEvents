namespace ExtEvents.Editor
{
    using System;
    using TypeReferences;
    using UnityEditor;
    using UnityEngine.Assertions;

    public static class PersistentArgumentHelper
    {
        public static Type GetTypeFromProperty(SerializedProperty argProperty, string typeFieldName)
        {
            var typeNameAndAssembly = argProperty.FindPropertyRelative($"{typeFieldName}.{nameof(TypeReference._typeNameAndAssembly)}").stringValue;
            var type = Type.GetType(typeNameAndAssembly);
            return type;
        }
    }
}