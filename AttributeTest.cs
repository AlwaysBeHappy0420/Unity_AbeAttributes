using AbeAttributes;
using UnityEngine;

public class AttributeTest : MonoBehaviour
{
    [PopToConsole(PopToConsoleMode.Invoke)]
    [PopToConsole(PopToConsoleMode.Manual)]
    public static void Foo()
    {
        Debug.Log("Foo Body");
    }
}