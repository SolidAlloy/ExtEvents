#if UNITY_EDITOR
namespace Test
{
    using ExtEvents;
    using ExtEvents.Editor;
    using Sirenix.OdinInspector;
    using SolidUtilities;
    using SolidUtilities.Editor;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;

    public class ExtEventsTestBehaviour : MonoBehaviour
    {
        [SerializeField] private int _iterationCount = 1_000_000;
        [SerializeField] private SceneAsset _scene;
        [SerializeField] private GameObject _prefab;

        public ExtEvent VoidEvent;

        public ExtEvent[] Events;

        public string[] EmptyArray;

        [Button]
        public void FindOnScene()
        {
            var scenePath = AssetDatabase.GetAssetPath(_scene);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            var rootGameObjects = scene.GetRootGameObjects();

            foreach (GameObject rootGameObject in rootGameObjects)
            {
                var components = rootGameObject.GetComponentsInChildren<Component>();

                foreach (Component component in components)
                {
                    foreach ((var prop, var extEvent) in ExtEventHelper.FindExtEvents(component))
                    {
                        Debug.Log($"found prop {prop.propertyPath}, extEvent not null {extEvent != null}");
                    }
                }
            }

            EditorSceneManager.CloseScene(scene, true);
        }

        [Button]
        public void TestBuildAnalyzer()
        {
            var foundObjects = new BuildAnalyzer.FoundObjects();

            foreach (SerializedObject serializedObject in BuildAnalyzer.GetAssetsInBuild(foundObjects))
            {
                // Debug.Log(serializedObject.targetObject.name);
            }

            foreach (string prefabPath in foundObjects.Prefabs)
            {
                Debug.Log(prefabPath);
            }

            foreach (string scriptableObjectName in foundObjects.ScriptableObjectNames)
            {
                Debug.Log(scriptableObjectName);
            }
        }

        [Button]
        public void TestCrossReference()
        {
            var foundObjects = new BuildAnalyzer.FoundObjects();
            foreach (SerializedObject serializedObject in BuildAnalyzer.GetSerializedObjectsFromGameObject(_prefab, foundObjects))
            {
                //
            }

            foreach (string prefabPath in foundObjects.Prefabs)
            {
                Debug.Log(prefabPath);
            }

            foreach (string scriptableObjectName in foundObjects.ScriptableObjectNames)
            {
                Debug.Log(scriptableObjectName);
            }
        }

        [Button]
        public void Test()
        {
            VoidEvent.Invoke();

            using (Timer.CheckInMilliseconds("built"))
            {
                for (int i = 0; i < _iterationCount; i++)
                {
                    VoidEvent.Invoke();
                }
            }
        }

        [Button]
        public void PrintPropertyPaths()
        {
            var serializedObject = new SerializedObject(this);
            var prop = serializedObject.GetIterator();

            if (prop.Next(true))
            {
                do
                {
                    Debug.Log(prop.propertyPath);
                } while (prop.NextVisible(true));
            }
        }

        [Button]
        public void TestGettingObject(string propertyPath)
        {
            var serializedObject = new SerializedObject(this);
            var prop = serializedObject.FindProperty(propertyPath);
            Debug.Log(prop.GetObject().GetType());
        }
    }
}
#endif
