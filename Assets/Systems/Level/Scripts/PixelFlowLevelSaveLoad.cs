using UnityEngine;

public sealed class PixelFlowLevelSaveLoad : ILevelSaveLoad
{
    private const string LevelDataKeyPrefix = "PixelFlow.Level.";

    public void Save(PixelFlowLevelData levelData)
    {
        if (levelData == null)
        {
            return;
        }

        Save(levelData.id, levelData);
    }

    public void Save(int levelId, PixelFlowLevelData levelData)
    {
        if (levelData == null || levelId <= 0)
        {
            return;
        }

        levelData.id = levelId;
        PlayerPrefs.SetString(BuildKey(levelId), JsonUtility.ToJson(levelData));
        PlayerPrefs.Save();
    }

    public PixelFlowLevelData Load()
    {
        return null;
    }

    public PixelFlowLevelData Load(int levelId)
    {
        if (levelId <= 0 || !PlayerPrefs.HasKey(BuildKey(levelId)))
        {
            return null;
        }

        var serializedLevel = PlayerPrefs.GetString(BuildKey(levelId));

        if (string.IsNullOrWhiteSpace(serializedLevel))
        {
            return null;
        }

        var level = JsonUtility.FromJson<PixelFlowLevelData>(serializedLevel);

        if (level != null && level.id <= 0)
        {
            level.id = levelId;
        }

        return level;
    }

    public void Clear()
    {
    }

    public void Clear(int levelId)
    {
        if (levelId <= 0)
        {
            return;
        }

        PlayerPrefs.DeleteKey(BuildKey(levelId));
        PlayerPrefs.Save();
    }

    private static string BuildKey(int levelId)
    {
        return $"{LevelDataKeyPrefix}{levelId}";
    }
}
