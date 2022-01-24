#if UNITY_EDITOR
namespace Test
{
    using ExtEvents;
    using Sirenix.OdinInspector;
    using SolidUtilities;
    using SolidUtilities.Editor;
    using UnityEditor;
    using UnityEngine;

    public class ExtEventsTestBehaviour : MonoBehaviour
    {
        [SerializeField] private int _iterationCount = 1_000_000;

        public ExtEvent VoidEvent;

        public ExtEvent[] Events;

        public string[] EmptyArray;

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
