using UnityEngine;
using UnityEditor;
using System.Collections;

public class ExampleWindow : EditorWindow
{
    // public string login = "username";
    // public string login2 = "no action here";

    // [MenuItem("Tools/Example")]
    // static void Init()
    // {
    //     var window = EditorWindow.GetWindow(typeof(ExampleWindow));
    //     // window.position = new Rect(50f, 50f, 5000f, 2400f);
    //     window.maxSize = new Vector2(1000f, 600f);
    //     window.minSize = new Vector2(1000f, 600f);
    //     window.Show();
    // }
    // void OnGUI()
    // {
    //     GUI.SetNextControlName("user");
    //     login = GUI.TextField(new Rect(10, 10, 130, 20), login);

    //     login2 = GUI.TextField(new Rect(10, 40, 130, 20), login2);
    //     if (Event.current.isKey && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "user")
    //         Debug.Log("Login");

    //     if (GUI.Button(new Rect(150, 10, 50, 20), "Login"))
    //         Debug.Log("Login");
    // }
}