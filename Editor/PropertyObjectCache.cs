namespace ExtEvents.Editor
{
    using System.Collections.Generic;
    using SolidUtilities.Editor;
    using UnityEditor;

    internal static class PropertyObjectCache
    {
        private static Dictionary<(SerializedObject serializedObject, string propertyPath), object> _propertyObjects =
            new Dictionary<(SerializedObject serializedObject, string propertyPath), object>();

        public static T GetObject<T>(SerializedProperty serializedProperty)
        {
            var serializedObject = serializedProperty.serializedObject;
            var propertyPath = serializedProperty.propertyPath;

            _propertyObjects.TryGetValue((serializedObject, propertyPath), out object value);

            if (value != null)
            {
                return (T) value;
            }

            value = serializedProperty.GetObject();
            _propertyObjects[(serializedObject, propertyPath)] = value;
            return (T) value;
        }
    }
}