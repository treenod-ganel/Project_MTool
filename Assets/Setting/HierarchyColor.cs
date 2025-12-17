using UnityEngine;

public class HierarchyColor : MonoBehaviour
{
    public Color color = Color.cyan;

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.RepaintHierarchyWindow();
    }
#endif
}
