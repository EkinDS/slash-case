using UnityEngine;

public static class PixelFlowLevelLoader
{
    public static PixelFlowLevelData Load(TextAsset levelAsset)
    {
        if (levelAsset == null || string.IsNullOrWhiteSpace(levelAsset.text))
        {
            return null;
        }

        return JsonUtility.FromJson<PixelFlowLevelData>(levelAsset.text);
    }
}
