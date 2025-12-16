using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class HierarchyColorDrawer
{
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

        EditorGUI.DrawRect(selectionRect, color);
    }
}
