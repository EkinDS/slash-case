using UnityEngine;

public enum PixelPigColor
{
    None = -1,
    Red = 0,
    Blue = 1,
    Green = 2,
    Yellow = 3
}

public static class PixelPigColorExtensions
{
    public static Color ToUnityColor(this PixelPigColor color)
    {
        switch (color)
        {
            case PixelPigColor.Red:
                return new Color32(240, 90, 90, 255);
            case PixelPigColor.Blue:
                return new Color32(79, 149, 255, 255);
            case PixelPigColor.Green:
                return new Color32(102, 214, 125, 255);
            case PixelPigColor.Yellow:
                return new Color32(255, 205, 84, 255);
            default:
                return new Color32(47, 54, 79, 255);
        }
    }

    public static string ToDisplayName(this PixelPigColor color)
    {
        switch (color)
        {
            case PixelPigColor.Red:
                return "Red";
            case PixelPigColor.Blue:
                return "Blue";
            case PixelPigColor.Green:
                return "Green";
            case PixelPigColor.Yellow:
                return "Yellow";
            default:
                return "None";
        }
    }
}
