namespace ExtEvents.Editor
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEditor.VersionControl;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class BuildAnalyzer
    {
        public static IEnumerable<SerializedObject> GetAssetsInBuild(FoundObjects foundObjects)
        {
            return EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes)
                .SelectMany(scenePath => GetSerializedObjectsFromScene(scenePath, foundObjects));
        }

        public static IEnumerable<SerializedObject> GetSerializedObjectsFromScene(string scenePath, FoundObjects foundObjects)
        {
            var currentScene = SceneManager.GetActiveScene();

            var scene = currentScene.path == scenePath ? currentScene : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            var rootGameObjects = scene.GetRootGameObjects();

            foreach (GameObject rootGameObject in rootGameObjects)
            {
                foreach (var serializedObject in GetSerializedObjectsFromGameObject(rootGameObject, foundObjects))
                {
                    yield return serializedObject;
                }
            }

            if (scene != currentScene)
                EditorSceneManager.CloseScene(scene, true);
        }

        public static IEnumerable<SerializedObject> GetSerializedObjectsFromGameObject(GameObject gameObject, FoundObjects foundObjects)
        {
            var components = gameObject.GetComponentsInChildren<Component>();

            foreach (Component component in components)
            {
                int instanceId = component.GetInstanceID();
                if (foundObjects.Components.Contains(instanceId))
                    continue;

                foundObjects.Components.Add(instanceId);
                var serializedObject = new SerializedObject(component);
                yield return serializedObject;

                foreach (SerializedObject childSerializedObject in GetSerializedObjectsFromSerializedObject(serializedObject, foundObjects))
                {
                    yield return childSerializedObject;
                }
            }
        }

        public static IEnumerable<SerializedObject> GetSerializedObjectsFromSerializedObject(SerializedObject serializedObject, FoundObjects foundObjects)
        {
            var prop = serializedObject.GetIterator();

            if (!prop.Next(true))
                yield break;

            do
            {
                foreach (SerializedObject propSerializedObject in GetSerializedObjectsFromSerializedProperty(prop, foundObjects))
                {
                    yield return propSerializedObject;
                }
            }
            while (prop.Next(true));
        }

        public static IEnumerable<SerializedObject> GetSerializedObjectsFromSerializedProperty(SerializedProperty property, FoundObjects foundObjects)
        {
            // corresponding source object is a reference to the parent prefab in prefab variants. We don't need to enumerate parent prefabs if they are not referenced directly.
            // m_GameObject is just a reference to the gameobject that contains a prefab. Since we already iterating of the component, we have a reference to the game object, so we don't need to iterate over a prefab twice.
            if (property.propertyType != SerializedPropertyType.ObjectReference || property.name == "m_CorrespondingSourceObject" || property.name == "m_GameObject")
                yield break;

            var value = property.objectReferenceValue;

            if (value is ScriptableObject)
            {
                foreach (var serializedObject in GetSerializedObjectsFromScriptableObject(value, foundObjects))
                {
                    yield return serializedObject;
                }

                yield break;
            }

            if ((value is Component || value is GameObject) && PrefabUtility.IsPartOfPrefabAsset(value))
            {
                foreach (var serializedObject in GetSerializedObjectsFromPrefab(value, foundObjects))
                {
                    yield return serializedObject;
                }
            }
        }

        private static IEnumerable<SerializedObject> GetSerializedObjectsFromPrefab(Object partOfPrefab, FoundObjects foundObjects)
        {
            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(partOfPrefab);

            if (foundObjects.Prefabs.Contains(prefabPath))
                yield break;

            foundObjects.Prefabs.Add(prefabPath);

            var rootGameObject = PrefabUtility.LoadPrefabContents(prefabPath);

            foreach (var serializedObject in GetSerializedObjectsFromGameObject(rootGameObject, foundObjects))
            {
                yield return serializedObject;
            }
        }

        private static IEnumerable<SerializedObject> GetSerializedObjectsFromScriptableObject(Object scriptableObject, FoundObjects foundObjects)
        {
            var instanceId = scriptableObject.GetInstanceID();
            if (foundObjects.ScriptableObjects.Contains(instanceId))
                yield break;

            foundObjects.ScriptableObjects.Add(instanceId);
            foundObjects.ScriptableObjectNames.Add(scriptableObject.name);
            var soSerializedObject = new SerializedObject(scriptableObject);

            yield return soSerializedObject;

            foreach (var childSerializedObject in GetSerializedObjectsFromSerializedObject(soSerializedObject, foundObjects))
            {
                yield return childSerializedObject;
            }
        }

        public class FoundObjects
        {
            public readonly HashSet<int> ScriptableObjects = new HashSet<int>();
            public readonly HashSet<int> Components = new HashSet<int>();
            public readonly HashSet<string> Prefabs = new HashSet<string>();
            public readonly List<string> ScriptableObjectNames = new List<string>();
        }
    }
}