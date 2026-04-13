using System.Collections.Generic;
using System.Globalization;
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

        foreach (var asset in assets)
        {
            var level = Load(asset);

            if (level != null)
            {
                levels.Add(level);
            }
        }

        return levels
            .OrderBy(level => level.id)
            .ThenBy(level => level.width)
            .ToList();
    }

    public static PixelFlowLevelData Load(TextAsset levelAsset)
    {
        if (levelAsset == null || string.IsNullOrWhiteSpace(levelAsset.text))
        {
            return null;
        }

        var level = JsonUtility.FromJson<PixelFlowLevelData>(levelAsset.text);

        if (level == null)
        {
            return null;
        }

        if (level.id <= 0)
        {
            level.id = ParseLevelId(levelAsset.name);
        }

        if (level.id <= 0)
        {
            level.id = 1;
        }

        return level;
    }

    public static PixelFlowLevelData LoadFromResources(string resourcePath, int levelId)
    {
        if (levelId <= 0)
        {
            return null;
        }

        var allLevels = LoadAllFromResources(resourcePath);

        for (var i = 0; i < allLevels.Count; i++)
        {
            if (allLevels[i] != null && allLevels[i].id == levelId)
            {
                return allLevels[i];
            }
        }

        return null;
    }

    private static int ParseLevelId(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
        {
            return 0;
        }

        var digits = new string(assetName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId)
            ? parsedId
            : 0;
    }
}
