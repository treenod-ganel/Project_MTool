using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class CreateMultipleScripts : EditorWindow
{
    public enum ScriptTemplate
    {
        MonoBehaviour,
        ScriptableObject,
        CSharpClass,
        Interface
    }

    [System.Serializable]
    private class ScriptEntry
    {
        public string name = "";
        public ScriptTemplate template = ScriptTemplate.MonoBehaviour;
    }

    private List<ScriptEntry> scriptEntries = new List<ScriptEntry> { new ScriptEntry() };
    private string folderPath = "Assets";
    private Vector2 scrollPosition;

    [MenuItem("Assets/Create/C# Scripts(Multiple)")]
    public static void ShowWindow()
    {
        var window = GetWindow<CreateMultipleScripts>("Create Multiple Scripts");
        window.minSize = new Vector2(450, 300);
        window.folderPath = GetSelectedFolderPath();
    }

    private static string GetSelectedFolderPath()
    {
        var selected = Selection.GetFiltered<Object>(SelectionMode.Assets);
        if (selected.Length == 0) return "Assets";

        string path = AssetDatabase.GetAssetPath(selected[0]);
        if (!Directory.Exists(path))
        {
            path = Path.GetDirectoryName(path);
        }

        return path;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        // 생성 위치 (수정 가능)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("생성 위치", GUILayout.Width(60));
        folderPath = EditorGUILayout.TextField(folderPath);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 스크립트 목록
        EditorGUILayout.LabelField("스크립트 목록", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        int removeIndex = -1;
        for (int i = 0; i < scriptEntries.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            scriptEntries[i].template = (ScriptTemplate)EditorGUILayout.EnumPopup(scriptEntries[i].template, GUILayout.Width(140));
            scriptEntries[i].name = EditorGUILayout.TextField(scriptEntries[i].name);
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                removeIndex = i;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex >= 0 && scriptEntries.Count > 1)
        {
            scriptEntries.RemoveAt(removeIndex);
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+", GUILayout.Width(25)))
        {
            scriptEntries.Add(new ScriptEntry());
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // Create 버튼
        if (GUILayout.Button("Create", GUILayout.Height(30)))
        {
            CreateScripts();
        }
    }

    private void CreateScripts()
    {
        // 폴더가 없으면 생성
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        int createdCount = 0;

        foreach (var entry in scriptEntries)
        {
            string trimmedName = entry.name.Trim();
            if (string.IsNullOrEmpty(trimmedName))
                continue;

            string filePath = Path.Combine(folderPath, trimmedName + ".cs");

            if (File.Exists(filePath))
            {
                Debug.LogWarning($"스크립트가 이미 존재합니다: {filePath}");
                continue;
            }

            string content = GetScriptContent(entry.template, trimmedName);
            File.WriteAllText(filePath, content);
            createdCount++;
        }

        AssetDatabase.Refresh();

        if (createdCount > 0)
        {
            Debug.Log($"{createdCount}개의 스크립트가 생성되었습니다. ({folderPath})");
            Close();
        }
        else
        {
            EditorUtility.DisplayDialog("알림", "생성할 스크립트가 없습니다.\n스크립트 이름을 입력해주세요.", "OK");
        }
    }

    private string GetScriptContent(ScriptTemplate template, string className)
    {
        switch (template)
        {
            case ScriptTemplate.MonoBehaviour:
                return $@"using UnityEngine;

public class {className} : MonoBehaviour
{{
    void Start()
    {{
    }}

    void Update()
    {{
    }}
}}
";
            case ScriptTemplate.ScriptableObject:
                return $@"using UnityEngine;

[CreateAssetMenu(fileName = ""{className}"", menuName = ""SO/{className}"")]
public class {className} : ScriptableObject
{{
}}
";
            case ScriptTemplate.CSharpClass:
                return $@"public class {className}
{{
}}
";
            case ScriptTemplate.Interface:
                return $@"public interface {className}
{{
}}
";
            default:
                return "";
        }
    }
}
