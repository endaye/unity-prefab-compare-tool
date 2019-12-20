using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

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
        public long localId;
        public PNode(GameObject gameObject)
        {
            this.gameObject = gameObject;
            this.show = false;
            this.active = this.gameObject.activeSelf;
        }
    }
    static GameObject prefab1;
    static GameObject prefab2;
    static Dictionary<GameObject, PNode> dict1 = new Dictionary<GameObject, PNode>();
    static Dictionary<GameObject, PNode> dict2 = new Dictionary<GameObject, PNode>();
    static List<string> addList = new List<string>();
    static List<string> delList = new List<string>();
    static List<string> modList = new List<string>();
    static List<string> comAddList = new List<string>();
    static List<string> comDelList = new List<string>();
    static List<string> comModList = new List<string>();

    static string[] ignoreComps = { "rigidbody", "rigidbody2D", "camera", "mesh", "materials", "material", "light", "animation", "constantForce", "renderer", "audio" };

    [MenuItem("Tools/Prefab Compare")]
    static void Init()
    {
        var window = EditorWindow.GetWindow(typeof(PrefabCompareWindow));
        // window.position = new Rect(50f, 50f, 5000f, 2400f);
        window.minSize = new Vector2(300f, 70f);
        window.maxSize = new Vector2(300f, 70f);
        window.Show();
    }

    void OnEnable()
    {
        titleContent = new GUIContent("Prefab Compare");
    }

    void OnGUI()
    {
        EditorGUIUtility.labelWidth = 50;

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        prefab1 = (GameObject)EditorGUILayout.ObjectField("trunk", prefab1, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck() && prefab1 != null)
        {
            // Debug.LogFormat("{0} changed", prefab1.name);
            InitDict(ref prefab1, ref dict1);
        }

        if (GUILayout.Button("Update", GUILayout.MaxWidth(50)) && prefab1 != null)
        {
            InitDict(ref prefab1, ref dict1);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        prefab2 = (GameObject)EditorGUILayout.ObjectField("dev", prefab2, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck() && prefab2 != null)
        {
            // Debug.LogFormat("{0} changed", prefab2.name);
            InitDict(ref prefab2, ref dict2);
        }

        if (GUILayout.Button("Update", GUILayout.MaxWidth(50)) && prefab2 != null)
        {
            InitDict(ref prefab2, ref dict2);
        }
        EditorGUILayout.EndHorizontal();

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
                comAddList.Clear();
                comDelList.Clear();
                comModList.Clear();
                CompareAll();
                OutputLog();
            }
        }
    }

    static void CompareAll()
    {
        CompareDFS(prefab1, prefab2);
    }

    // 1. Compare two game objects' file id
    // 2. Compare two game objects' attributes
    // 3. Compare components
    // 4. Compare components attributs
    // 5. To children
    static void CompareDFS(GameObject obj1, GameObject obj2)
    {
        CompareBasicAttr(obj1, obj2);
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
                    if (dict1[c1].localId == dict2[c2].localId)
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

    static bool CompareBasicAttr(GameObject obj1, GameObject obj2)
    {
        var path1 = dict1[obj1].path;
        var path2 = dict2[obj2].path;
        var index1 = path1.IndexOf('/', 1);
        var index2 = path2.IndexOf('/', 1);
        var relPath1 = index1 > 0 ? path1.Substring(index1) : "";
        var relPath2 = index2 > 0 ? path2.Substring(index2) : "";
        var activeSelf1 = obj1.activeSelf;
        var activeSelf2 = obj2.activeSelf;
        var transform1 = obj1.transform;
        var transform2 = obj2.transform;
        if (obj1.name == obj2.name && relPath1 == relPath2 && activeSelf1 == activeSelf2 && transform1 == transform2)
        {
            return true;
        }
        if (obj1.transform.parent != null && obj2.transform.parent != null && obj1.name != obj2.name)
        {
            modList.Add(string.Format("<color=orange>dev:</color>   {0} (GameObject.name)", dict2[obj2].path));
        }
        if (activeSelf1 != activeSelf2)
        {
            modList.Add(string.Format("<color=orange>dev:</color>   {0} (GameObject.activeSelf)", dict2[obj2].path));
        }
        if (transform1.localPosition != transform2.localPosition)
        {
            modList.Add(string.Format("<color=orange>dev:</color>   {0} (GameObject.transform.localPosition)", dict2[obj2].path));
        }
        if (transform1.localRotation != transform2.localRotation)
        {
            modList.Add(string.Format("<color=orange>dev:</color>   {0} (GameObject.transform.localRotation)", dict2[obj2].path));
        }
        if (transform1.localScale != transform2.localScale)
        {
            modList.Add(string.Format("<color=orange>dev:</color>   {0} (GameObject.transform.localScale)", dict2[obj2].path));
        }
        return false;
    }

    static void CompareComp(GameObject obj1, GameObject obj2)
    {
        var cs1 = obj1.GetComponents(typeof(Component)) as Component[];
        var cs2 = obj2.GetComponents(typeof(Component)) as Component[];
        var common = new List<Component>();
        var start2 = 0; // use to keep order
        for (var i = 0; i < cs1.Length; i++)
        {
            for (var j = start2; j < cs2.Length; j++)
            {
                if (cs1[i].GetType() == cs2[j].GetType())
                {
                    start2 = j + 1;
                    common.Add(cs1[i]);
                    common.Add(cs2[j]);
                    break;
                }
            }
        }
        foreach (Component c1 in cs1)
        {
            if (!common.Contains(c1))
            {
                comDelList.Add(string.Format("<color=red>trunk:</color> {0} ({1})", dict1[obj1].path, c1.GetType()));
            }
        }
        foreach (Component c2 in cs2)
        {
            if (!common.Contains(c2))
            {
                comAddList.Add(string.Format("<color=lime>dev:</color> {0} ({1})", dict2[obj2].path, c2.GetType()));
            }
        }
        var commonArr = common.ToArray();
        // Debug.Log(commonArr.Length);
        for (var i = 0; i < commonArr.Length; i += 2)
        {
            CompareCompAttr(commonArr[i], commonArr[i + 1], obj1, obj2);
        }
    }

    static void CompareCompAttr(Component comp1, Component comp2, GameObject obj1, GameObject obj2)
    {
        var props1 = comp1.GetType().GetProperties();
        var props2 = comp2.GetType().GetProperties();
        if (props1.Length != props2.Length)
        {
            comModList.Add(string.Format("<color=orange>dev:</color>   {0} (Properties Count)", dict2[obj2].path));
            return;
        }
        try
        {
            for (var i = 0; i < props1.Length; ++i)
            {
                if (Array.IndexOf(ignoreComps, props1[i].Name.ToString()) < 0
                && props1[i].Name == props2[i].Name
                && comp2.GetType().ToString() != "UnityEngine.Transform"
                && props1[i].GetValue(comp1).ToString() != props2[i].GetValue(comp2).ToString())
                {
                    comModList.Add(string.Format("<color=orange>dev:</color>   {0} ({1}.{2})", dict2[obj2].path, comp2.GetType(), props2[i].Name));
                }
            }
        }
        catch
        {
            // Debug.LogError("工具异常，请不要联系作者");
        }
    }

    static void OutputLog()
    {
        Debug.LogFormat("<color=lime>GameObject Add ({1})</color>\n{0}", string.Join("\n", addList.ToArray()), addList.Count);
        Debug.LogFormat("<color=red>GameObject Delete ({1})</color>\n{0}", string.Join("\n", delList.ToArray()), delList.Count);
        Debug.LogFormat("<color=orange>GameObject Modified ({1})</color>\n{0}", string.Join("\n", modList.ToArray()), modList.Count);
        Debug.LogFormat("<color=lime>Component Add ({1})</color>\n{0}", string.Join("\n", comAddList.ToArray()), comAddList.Count);
        Debug.LogFormat("<color=red>Component Delete ({1})</color>\n{0}", string.Join("\n", comDelList.ToArray()), comDelList.Count);
        Debug.LogFormat("<color=orange>Component Modified ({1})</color>\n{0}", string.Join("\n", comModList.ToArray()), comModList.Count);
    }

    static void InitDict(ref GameObject prefab, ref Dictionary<GameObject, PNode> dict)
    {
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
        node.localId = GetLocalID(obj);
        dict.Add(obj, node);
        foreach (Transform child in obj.transform)
        {
            DFS(child.gameObject, dict, level + 1, node.path);
        }
    }

    // UTIL
    static PropertyInfo debugModeInspectorThing;

    // A hack to allow for SerializedObject to contain the m_LocalIdentfierInFile
    // SerializedProperty, which is required for the localID to be retrieved.
    public static long GetLocalID(GameObject go)
    {
        initDebugMode();
        SerializedObject so = new SerializedObject(go);
        debugModeInspectorThing.SetValue(so, InspectorMode.Debug, null);
        SerializedProperty localIDProp = so.FindProperty("m_LocalIdentfierInFile");
        return localIDProp.longValue;
    }

    static void initDebugMode()
    {
        if (debugModeInspectorThing == null)
        {
            debugModeInspectorThing = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }

}
