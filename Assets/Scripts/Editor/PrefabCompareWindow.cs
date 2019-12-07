// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEditor;
// using System.Linq;
// using System.Reflection;

// [ExecuteInEditMode]
// public class PrefabCompareWindow : EditorWindow
// {
//     public class PNode
//     {
//         GameObject gameObject;
//         public bool show;
//         public bool active;
//         public int level;
//         public PNode(GameObject gameObject)
//         {
//             this.gameObject = gameObject;
//         }
//     }
//     const int SPACE_PIXEL = 20;
//     readonly static Color COLOR_ACTIVE = new Color(1.0f, 1.0f, 1.0f, 1.0f);
//     readonly static Color COLOR_INACTIVE = new Color(1.0f, 1.0f, 1.0f, 0.5f);
//     static GameObject prefab1;
//     static GameObject prefab2;
//     static GameObject[] gameObjects;
//     static EditorWindow inspectorInstance;
//     static Dictionary<GameObject, PNode> dict = new Dictionary<GameObject, PNode>();
//     Vector2 scroll;

//     [MenuItem("Tools/Prefab Compare")]
//     static void Init()
//     {
//         var window = EditorWindow.GetWindow(typeof(PrefabCompareWindow));
//         // window.position = new Rect(50f, 50f, 5000f, 2400f);
//         window.maxSize = new Vector2(1000f, 600f);
//         window.minSize = new Vector2(1000f, 600f);
//         window.Show();
//     }

//     void OnEnable()
//     {
//         titleContent = new GUIContent("Prefab Compare");
//     }

//     void OnGUI()
//     {
//         ShowPrefabHierarchy(ref prefab1);
//         if (GUILayout.Button("Compare"))
//         {
//             if (prefab1 == null || prefab2 == null)
//             {
//                 ShowNotification(new GUIContent("大哥，至少要俩才能比较！"));
//             }
//             // else if (Help.HasHelpForObject(source))
//             //     Help.ShowHelpForObject(source);
//             else
//             {
//                 // Help.BrowseURL("http://forum.unity3d.com/search.php");
//                 // DFS(prefab1);
//             }
//         }
//     }

//     void ShowPrefabHierarchy(ref GameObject prefab)
//     {
//         EditorGUI.BeginChangeCheck();
//         prefab = (GameObject)EditorGUILayout.ObjectField("trunk", prefab, typeof(GameObject), false);
//         if (EditorGUI.EndChangeCheck() && prefab != null)
//         {
//             Debug.LogFormat("{0} changed", prefab.name);
//             gameObjects = null;
//             dict.Clear();
//             DFS(prefab, 0);
//             gameObjects = dict.Keys.ToArray();
//         }
//         if (GUILayout.Button("Show/Hide All"))
//         {
//             ToggleAllNode(prefab, dict);
//         }
//         scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Width(200), GUILayout.Height(500));
//         {
//             // gameObjects = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];
//             if (prefab != null)
//             {
//                 foreach (GameObject go in gameObjects)
//                 {
//                     // Start with game objects without any parents
//                     if (go.transform.parent == null)
//                     {
//                         // Show the object and its children
//                         ShowObject(go, gameObjects);
//                     }
//                 }
//             }
//         }
//         EditorGUILayout.EndScrollView();
//     }

//     void ShowObject(GameObject parent, GameObject[] gameObjects)
//     {
//         // Show entry for parent object
//         GUILayout.BeginHorizontal();
//         GUILayout.Space(SPACE_PIXEL * dict[parent].level);
//         GUI.color = dict[parent].active ? COLOR_ACTIVE : GUI.color = COLOR_INACTIVE;
//         if (parent.transform.childCount > 0)
//         {
//             EditorGUI.BeginChangeCheck();
//             dict[parent].show = EditorGUILayout.Foldout(dict[parent].show, parent.name);
//             if (EditorGUI.EndChangeCheck())
//             {
//                 Debug.LogFormat("{0}.show = {1}", parent.name, dict[parent].show);
//                 InspectTarget(parent);
//             }
//         }
//         else
//         {
//             // GUILayout.Label(parent.name);
//             EditorGUILayout.ObjectField(parent.name, parent, typeof(GameObject), false);
//         }
//         GUILayout.EndHorizontal();
//         if (dict[parent].show && parent.transform.childCount > 0)
//         {
//             foreach (GameObject go in gameObjects)
//             {
//                 // Find children of the parent game object
//                 if (go.transform.parent == parent.transform)
//                 {
//                     ShowObject(go, gameObjects);
//                 }
//             }
//         }
//         GUI.color = COLOR_ACTIVE;
//     }

//     void DFS(GameObject obj, int level)
//     {
//         if (obj == null)
//         {
//             return;
//         }
//         var node = new PNode(obj);
//         node.show = false;
//         node.level = level;
//         node.active = obj.activeSelf && (obj.transform.parent == null || dict[obj.transform.parent.gameObject].active);
//         dict.Add(obj, node);
//         foreach (Transform child in obj.transform)
//         {
//             DFS(child.gameObject, level + 1);
//         }
//     }

//     void ToggleAllNode(GameObject root, Dictionary<GameObject, PNode> dict)
//     {
//         if (dict.ContainsKey(root))
//         {
//             var show = !dict[root].show;
//             foreach (var node in dict)
//             {
//                 node.Value.show = show;
//             }
//         }
//         else
//         {
//             Debug.LogError("GameObject and Dictionary are not match");
//         }
//     }

//     /// <summary>
//     /// Creates a new inspector window instance and locks it to inspect the specified target
//     /// </summary>
//     public static void InspectTarget(GameObject target)
//     {
//         var inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
//         if (inspectorInstance == null)
//         {
//             // Get a reference to the `InspectorWindow` type object
//             // Create an InspectorWindow instance
//             inspectorInstance = ScriptableObject.CreateInstance(inspectorType) as EditorWindow;
//         }
//         // We display it - currently, it will inspect whatever gameObject is currently selected
//         // So we need to find a way to let it inspect/aim at our target GO that we passed
//         // For that we do a simple trick:
//         // 1- Cache the current selected gameObject
//         // 2- Set the current selection to our target GO (so now all inspectors are targeting it)
//         // 3- Lock our created inspector to that target
//         // 4- Fallback to our previous selection
//         // inspectorInstance.Show();
        
//         // // Cache previous selected gameObject
//         // var prevSelection = Selection.activeGameObject;
//         // // Set the selection to GO we want to inspect
//         // Selection.activeGameObject = target;
//         // // Get a ref to the "locked" property, which will lock the state of the inspector to the current inspected target
//         // var isLocked = inspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public);
//         // // Invoke `isLocked` setter method passing 'true' to lock the inspector
//         // isLocked.GetSetMethod().Invoke(inspectorInstance, new object[] { true });
//         // // Finally revert back to the previous selection so that other inspectors continue to inspect whatever they were inspecting...
//         // Selection.activeGameObject = prevSelection;
//     }
//     // Now you just:
//     // InspectTarget(myGO);
// }
