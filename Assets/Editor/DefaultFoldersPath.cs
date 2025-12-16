using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(DefaultFoldersPath), menuName = "SO/Setting/DefaultFoldersPath")]
public class DefaultFoldersPath : ScriptableObject
{
    public List<String> GetPath() => defaultFoldersPath;

    [SerializeField]
    private List<String> defaultFoldersPath = new List<string>()
    {
        "01.Script",
        "02.Sprite",
        "03.Model",
        "04.Prefab",
        "05.Animation",
        "06.Audio",
        "07.Material",
        "08.SO"
    };

}
