using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PixelFlowLevelLoader
{
    public static List<PixelFlowLevelData> LoadAllFromResources(string resourcePath)
    {
        var assets = Resources.LoadAll<TextAsset>(resourcePath);
        var levels = new List<PixelFlowLevelData>();

        if (assets == null || assets.Length == 0)
        {
            return levels;
        }

        foreach (var asset in assets.OrderBy(asset => asset.name))
        {
            var level = Load(asset);

            if (level != null)
            {
                levels.Add(level);
            }
        }

        return levels;
    }

    public static PixelFlowLevelData Load(TextAsset levelAsset)
    {
        if (levelAsset == null || string.IsNullOrWhiteSpace(levelAsset.text))
        {
            return null;
        }

        return JsonUtility.FromJson<PixelFlowLevelData>(levelAsset.text);
    }
}
