#if UNITY_EDITOR
using ExtEvents.Editor;
using Sirenix.OdinInspector;
using UnityEngine;

public class ExtEventsBehaviour : MonoBehaviour
{
    [Button]
    public void Test()
    {
        CreateMethodsGenerator.GenerateCreateMethodsAssembly();
    }
}
#endif
