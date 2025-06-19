using UnityEditor;
using UnityEngine;

public class TestMenuScript
{
    [MenuItem("Test/Hello World")]
    static void TestHello()
    {
        Debug.Log("✅ Les menus fonctionnent !");
        EditorUtility.DisplayDialog("Test", "Le menu fonctionne!", "OK");
    }
}
