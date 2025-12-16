using UnityEngine;
using UnityEditor;
using System.IO;


public static class CreateProjectFolders
{
    private static DefaultFoldersPath FindDefaultFoldersPath()
    {
        // 프로젝트 내 모든 DefaultFoldersPath SO 찾기
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(DefaultFoldersPath)}");

        if (guids.Length == 0)
        {
            Debug.LogError("DefaultFoldersPath SO가 프로젝트에 존재하지 않습니다! Create > SO > Setting > DefaultFoldersPath 로 생성해주세요.");
            return null;
        }

        if (guids.Length > 1)
        {
            Debug.LogWarning($"DefaultFoldersPath SO가 {guids.Length}개 존재합니다. 첫 번째 것을 사용합니다. 하나만 유지하는 것을 권장합니다.");
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<DefaultFoldersPath>(path);
    }

    [MenuItem("Assets/Create/Create Folders")]
    public static void CreateFolders()
    {
        var defaultFolderspath = FindDefaultFoldersPath();
        if (defaultFolderspath == null) return;

        foreach (var path in defaultFolderspath.GetPath())
        {
            string folderPath = $"Assets/{path}";
            if (!Directory.Exists(folderPath))
            {
                Debug.Log($"{folderPath} 폴더가 생성 되었습니다!");
                Directory.CreateDirectory(folderPath);
            }
            else
            {
                Debug.LogWarning($"{folderPath} 해당 폴더는 이미 존재 합니다");
            }
        }
        AssetDatabase.Refresh();
    }
}
