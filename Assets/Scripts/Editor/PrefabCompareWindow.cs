using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;

[ExecuteInEditMode]
public class PrefabCompareWindow : EditorWindow
{
    public class PNode
    {
        GameObject gameObject;
        public bool show;
        public bool active;
        public int level;
        public string path;
        public PNode(GameObject gameObject)
        {
            this.gameObject = gameObject;
        }
    }
    const int SPACE_PIXEL = 20;
    readonly static Color COLOR_ACTIVE = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    readonly static Color COLOR_INACTIVE = new Color(1.0f, 1.0f, 1.0f, 0.5f);
    static GameObject prefab1;
    static GameObject prefab2;
    static GameObject[] gameObjects;
    static EditorWindow inspectorInstance;
    static Dictionary<GameObject, PNode> dict1 = new Dictionary<GameObject, PNode>();
    static Dictionary<GameObject, PNode> dict2 = new Dictionary<GameObject, PNode>();
    static List<string> addList = new List<string>();
    static List<string> delList = new List<string>();
    static List<string> modList = new List<string>();
    static List<string> comList = new List<string>();
    static Vector2 scroll;

    [MenuItem("Tools/Prefab Compare")]
    static void Init()
    {
        var window = EditorWindow.GetWindow(typeof(PrefabCompareWindow));
        // window.position = new Rect(50f, 50f, 5000f, 2400f);
        window.maxSize = new Vector2(1000f, 600f);
        window.minSize = new Vector2(1000f, 600f);
        window.Show();
    }

    void OnEnable()
    {
        titleContent = new GUIContent("Prefab Compare");
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        {
            EditorGUI.BeginChangeCheck();
            prefab1 = (GameObject)EditorGUILayout.ObjectField("trunk", prefab1, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck() && prefab1 != null)
            {
                Debug.LogFormat("{0} changed", prefab1.name);
                InitDict(ref prefab1, ref dict1);
                // gameObjects = dict1.Keys.ToArray();
            }

            if (GUILayout.Button("Update") && prefab1 != null)
            {
                InitDict(ref prefab1, ref dict1);
            }

            EditorGUI.BeginChangeCheck();
            prefab2 = (GameObject)EditorGUILayout.ObjectField("dev", prefab2, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck() && prefab2 != null)
            {
                Debug.LogFormat("{0} changed", prefab2.name);
                InitDict(ref prefab2, ref dict2);
                // gameObjects = dict2.Keys.ToArray();
            }

            if (GUILayout.Button("Update") && prefab2 != null)
            {
                InitDict(ref prefab2, ref dict2);
            }
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Compare"))
        {
            if (prefab1 == null || prefab2 == null)
            {
                ShowNotification(new GUIContent("至少需要两个prefab才能比较"));
            }
            else
            {
                addList.Clear();
                delList.Clear();
                modList.Clear();
                comList.Clear();
                CompareAll();
                OutputLog();
            }
        }
    }

    static void CompareAll()
    {
        CompareDFS(prefab1, prefab2);
    }

    static void CompareDFS(GameObject obj1, GameObject obj2)
    {
        CompareComp(obj1, obj2);
        if (obj1.transform.childCount == 0 && obj2.transform.childCount == 0)
        {
            return;
        }
        if (obj1.transform.childCount == 0 && obj2.transform.childCount > 0)
        {
            foreach (GameObject c2 in obj2.transform)
            {
                addList.Add(string.Format("<color=lime>dev:</color>   {0}", dict2[c2].path));
            }
        }
        else if (obj1.transform.childCount > 0 && obj2.transform.childCount == 0)
        {
            foreach (Transform c1 in obj1.transform)
            {
                delList.Add(string.Format("<color=red>trunk:</color> {0}", dict1[c1.gameObject].path));
            }
        }
        else
        {
            // var common = new Dictionary<GameObject, PNode>();
            var common = new List<GameObject>();
            var start2 = 0;     // use to keep order
            for (var i = 0; i < obj1.transform.childCount; i++)
            {
                var c1 = obj1.transform.GetChild(i).gameObject;
                for (var j = start2; j < obj2.transform.childCount; j++)
                {
                    var c2 = obj2.transform.GetChild(j).gameObject;
                    if (CompareBasic(c1, c2))
                    {
                        start2 = j + 1;
                        common.Add(c1);
                        common.Add(c2);
                        break;
                    }
                }

            }
            foreach (Transform child in obj1.transform)
            {
                if (!common.Contains(child.gameObject))
                {
                    delList.Add(string.Format("<color=red>trunk:</color> {0}", dict1[child.gameObject].path));
                }
            }
            foreach (Transform child in obj2.transform)
            {
                if (!common.Contains(child.gameObject))
                {
                    addList.Add(string.Format("<color=lime>dev:</color>   {0}", dict2[child.gameObject].path));
                }
            }
            var commonArr = common.ToArray();
            for (var i = 0; i < commonArr.Length; i += 2)
            {
                CompareDFS(commonArr[i], commonArr[i + 1]);
            }
        }
    }

    static bool CompareBasic(GameObject obj1, GameObject obj2)
    {
        var path1 = dict1[obj1].path;
        var index1 = path1.IndexOf('/', 1);
        var relPath1 = path1.Substring(index1);
        var path2 = dict2[obj2].path;
        var index2 = path2.IndexOf('/', 1);
        var relPath2 = path2.Substring(index2);
        return obj1.name == obj2.name && relPath1 == relPath2;
    }

    static void CompareComp(GameObject obj1, GameObject obj2)
    {
        return;
    }

    static void OutputLog()
    {
        Debug.LogFormat("<color=lime>Add new game objects in dev ({1})</color>\n{0}", string.Join("\n", addList.ToArray()), addList.Count);
        Debug.LogFormat("<color=red>Delete from trunk ({1})</color>\n{0}", string.Join("\n", delList.ToArray()), delList.Count);
        Debug.LogFormat("<color=orange>Modified game objects ({1})</color>\n{0}", string.Join("\n", modList.ToArray()), modList.Count);
        Debug.LogFormat("<color=yellow>Changed components ({1})</color>\n{0}", string.Join("\n", comList.ToArray()), comList.Count);
    }

    static void InitDict(ref GameObject prefab, ref Dictionary<GameObject, PNode> dict)
    {
        gameObjects = null;
        dict.Clear();
        DFS(prefab, dict, 0, "");
    }

    static void DFS(GameObject obj, Dictionary<GameObject, PNode> dict, int level, string path)
    {
        if (obj == null)
        {
            return;
        }
        var node = new PNode(obj);
        node.show = false;
        node.level = level;
        node.active = obj.activeSelf && (obj.transform.parent == null || dict[obj.transform.parent.gameObject].active);
        node.path = string.Format("{0}/{1}", path, obj.name);
        dict.Add(obj, node);
        foreach (Transform child in obj.transform)
        {
            DFS(child.gameObject, dict, level + 1, node.path);
        }
    }

    public static long GetLocalID(GameObject go)
    {
        SerializedObject so = new SerializedObject(go);
        SerializedProperty localIDProp = so.FindProperty("m_LocalIdentfierInFile");
        return localIDProp.longValue;
    }
}
