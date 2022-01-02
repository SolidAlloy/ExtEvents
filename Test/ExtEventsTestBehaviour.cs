namespace Test
{
    using ExtEvents;
    using ExtEvents.Editor;
    using Sirenix.OdinInspector;
    using SolidUtilities.Helpers;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;

    public class ExtEventsTestBehaviour : MonoBehaviour
    {
        [SerializeField] private int _iterationCount = 1_000_000;
        [SerializeField] private SceneAsset _scene;

        public ExtEvent VoidEvent;

        [Button]
        public void Build()
        {
            var serializedObject = new SerializedObject(this);
            ExtEventHelper.BuildResponses(serializedObject.FindProperty(nameof(VoidEvent)), VoidEvent);
        }

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
                    foreach ((var prop, var extEvent) in BuiltResponsesCreator.FindExtEvents(component))
                    {
                        Debug.Log($"found prop {prop.propertyPath}, extEvent not null {extEvent != null}");
                    }
                }
            }

            EditorSceneManager.CloseScene(scene, true);
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
    }
}
