using UnityEngine;

public sealed class PixelFlowLevelSaveLoad
{
    private const string LevelDataKey = "PixelFlow.CustomLevel";

    public void Save(PixelFlowLevelData levelData)
    {
        if (levelData == null)
        {
            return;
        }

        PlayerPrefs.SetString(LevelDataKey, JsonUtility.ToJson(levelData));
        PlayerPrefs.Save();
    }

    public PixelFlowLevelData Load()
    {
        if (!PlayerPrefs.HasKey(LevelDataKey))
        {
            return null;
        }

        var serializedLevel = PlayerPrefs.GetString(LevelDataKey);

        if (string.IsNullOrWhiteSpace(serializedLevel))
        {
            return null;
        }

        return JsonUtility.FromJson<PixelFlowLevelData>(serializedLevel);
    }

    public void Clear()
    {
        PlayerPrefs.DeleteKey(LevelDataKey);
        PlayerPrefs.Save();
    }
}
