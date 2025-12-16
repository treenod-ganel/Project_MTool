using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class CopyGameObjectPath
{
    [MenuItem("GameObject/Copy Path")]
    private static void CopyPath()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "게임 오브젝트를 선택해주세요.", "OK");
            return;
        }

        string slashPath = GetSlashPath(selected);
        string hierarchyPath = GetHierarchyPath(selected);
        EditorGUIUtility.systemCopyBuffer = slashPath;
        Debug.Log("경로 복사됨:\n" + hierarchyPath);
    }

    private static string GetSlashPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private static string GetHierarchyPath(GameObject obj)
    {
        var hierarchy = new List<string>();
        Transform current = obj.transform;

        while (current != null)
        {
            hierarchy.Add(current.name);
            current = current.parent;
        }

        hierarchy.Reverse();

        var sb = new StringBuilder();
        for (int i = 0; i < hierarchy.Count; i++)
        {
            sb.Append(new string(' ', i * 2));
            if (i > 0) sb.Append("ㄴ ");
            sb.AppendLine(hierarchy[i]);
        }

        return sb.ToString().TrimEnd();
    }
}
