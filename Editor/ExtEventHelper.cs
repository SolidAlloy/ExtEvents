namespace ExtEvents.Editor
{
    using System.Collections.Generic;
    using GenericUnityObjects.Editor.Util;
    using SolidUtilities.Editor;
    using UnityEditor;
    using UnityEngine;

    public static class ExtEventHelper
    {
        public static IEnumerable<(SerializedProperty extEventProperty, BaseExtEvent extEvent)> FindExtEvents(Object obj)
        {
            var serializedObject = new SerializedObject(obj);
            var prop = serializedObject.GetIterator();

            if (!prop.Next(true))
                yield break;

            do
            {
                if (prop.name != nameof(BaseExtEvent._responses) || prop.GetObjectType() != typeof(SerializedResponse[]))
                    continue;

                var extEventProperty = prop.GetParent();
                var extEvent = extEventProperty.GetObject<BaseExtEvent>();
                yield return (extEventProperty, extEvent);
            }
            while (prop.NextVisible(true));
        }

        public static void BuildResponses(SerializedProperty extEventProperty, BaseExtEvent extEvent)
        {
            // We need to do it before a build, then delete the assemblies
            for (int index = 0; index < extEvent._responses.Length; index++)
            {
                var response = extEvent._responses[index];
                BuildResponse(extEventProperty, response, index);
            }
        }

        private static void BuildResponse(SerializedProperty extEventProperty, SerializedResponse response, int responseIndex)
        {
            // Create an assembly, name it according to the method it invokes
            var methodInfo = response.GetMethod();
            var assemblyPath = BuiltResponseCreator.CreateBuiltResponseAssembly("TestBuiltResponseAssembly", methodInfo);

            PersistentStorage.SaveData("TestBuiltResponseAssembly", assemblyPath);
            PersistentStorage.SaveData("BuiltResponseInstanceId", extEventProperty.serializedObject.targetObject.GetInstanceID());
            PersistentStorage.SaveData("BuiltResponsePath", extEventProperty.propertyPath);
            PersistentStorage.SaveData("ResponseIndex", responseIndex);
            PersistentStorage.DelayActionsOnScriptsReload = true;
            PersistentStorage.ExecuteOnScriptsReload(AfterAssemblyCreated);
            AssetDatabase.Refresh();
        }

        private static void AfterAssemblyCreated()
        {
            try
            {
                var assemblyPath = PersistentStorage.GetData<string>("TestBuiltResponseAssembly");
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(assemblyPath);
                var type = monoScript.GetClassType();
                var scriptableObject = ScriptableObject.CreateInstance(type);
                AssetDatabase.CreateAsset(scriptableObject, "Assets/TestBuiltResponseAssemblyAsset.asset");

                // Assign the scriptable object to the response field (need to find the serialized response again)
                // Make sure the field assignment is saved: setdirty for scriptable objects, save prefabs, etc.

                var targetObjectId = PersistentStorage.GetData<int>("BuiltResponseInstanceId");
                var targetObject = EditorUtility.InstanceIDToObject(targetObjectId);
                var serializedObject = new SerializedObject(targetObject);
                string propertyPath = PersistentStorage.GetData<string>("BuiltResponsePath");
                var property = serializedObject.FindProperty(propertyPath);
                var responses = property.FindPropertyRelative(nameof(BaseExtEvent._responses));
                var response = responses.GetArrayElementAtIndex(PersistentStorage.GetData<int>("ResponseIndex"));
                var builtResponseField = response.FindPropertyRelative("_builtResponse");
                builtResponseField.objectReferenceValue = scriptableObject;
                serializedObject.ApplyModifiedProperties();
            }
            finally
            {
                PersistentStorage.DeleteData("TestBuiltResponseAssembly");
                PersistentStorage.DeleteData("BuiltResponseInstanceId");
                PersistentStorage.DeleteData("BuiltResponsePath");
                PersistentStorage.DeleteData("ResponseIndex");
            }
        }
    }
}