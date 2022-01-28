#if UNITY_EDITOR
using ExtEvents.Editor;
using Sirenix.OdinInspector;
using UnityEngine;

public class ExtEventsBehaviour : MonoBehaviour
{
    [Button]
    public void Test()
    {
        AOTAssemblyGenerator.GenerateCreateMethods();
    }

    [Button]
    public void TestDeletion()
    {
        AOTAssemblyGenerator.DeleteGeneratedFolder();
    }
}
#endif
