using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class HierarchyColorDrawer
{
    private const float width = 2.5f;

    static HierarchyColorDrawer()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
    }

    private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go == null) return;

        HierarchyColor hierarchyColor = go.GetComponent<HierarchyColor>();
        if (hierarchyColor == null) return;

        Color color = hierarchyColor.color;
        color.a = 0.3f;

        var rect = new Rect(selectionRect.x - 5, selectionRect.y, width, selectionRect.height);

        EditorGUI.DrawRect(rect, color);
    }
}
