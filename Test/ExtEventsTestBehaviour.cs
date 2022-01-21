#if UNITY_EDITOR
namespace Test
{
    using System.IO;
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

        [SerializeField] private string _linkXmlPath = "Assets/test-link.xml";

        public ExtEvent VoidEvent;

        public ExtEvent[] Events;

        public string[] EmptyArray;

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
        public void TestLinkXml()
        {
            var serializedObjects = BuildAnalyzer.GetAssetsInBuild(new BuildAnalyzer.FoundObjects());
            var properties = ExtEventHelper.FindExtEventProperties(serializedObjects);
            var methods = ExtEventHelper.GetMethods(properties);
            var linkXml = new LinkXML();
            linkXml.AddMethods(methods);
            string fileContent = linkXml.Generate();
            File.WriteAllText(_linkXmlPath, fileContent);
            AssetDatabase.Refresh();
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
