using UnityEngine;

[CreateAssetMenu(menuName = "Test/Test Object", fileName = "TestObject")]

public class TestScrObj : ScriptableObject
{
    [SerializeField] private Texture _image;
}
