using TMPro;
using UnityEngine;

public static class WorldObjectUtility
{
    private static TMP_FontAsset defaultFontAsset;

    public static GameObject CreatePrimitive(string name, PrimitiveType primitiveType, Transform parent, Vector3 localPosition,
        Vector3 localScale, Color color)
    {
        var gameObject = CreatePrimitive(name, primitiveType, parent, localPosition, localScale);
        SetColor(gameObject, color);
        return gameObject;
    }

    public static GameObject CreatePrimitive(string name, PrimitiveType primitiveType, Transform parent, Vector3 localPosition,
        Vector3 localScale, WorldMaterialRole materialRole)
    {
        var gameObject = CreatePrimitive(name, primitiveType, parent, localPosition, localScale);
        SetMaterial(gameObject, WorldMaterialPalette.Get(materialRole));
        return gameObject;
    }

    public static GameObject CreatePrimitive(string name, PrimitiveType primitiveType, Transform parent, Vector3 localPosition,
        Vector3 localScale)
    {
        var gameObject = GameObject.CreatePrimitive(primitiveType);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localScale = localScale;
        return gameObject;
    }

    public static GameObject CreatePrimitive(string name, PrimitiveType primitiveType, Transform parent, Vector3 localPosition)
    {
        return CreatePrimitive(name, primitiveType, parent, localPosition, Vector3.one);
    }

    public static TextMeshPro CreateWorldText(string name, Transform parent, Vector3 localPosition, string content, int fontSize,
        Color color, TextAnchor anchor, float characterSize = 0.1F)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localRotation = Quaternion.identity;
        gameObject.transform.localScale = Vector3.one * characterSize;

        var textMesh = gameObject.AddComponent<TextMeshPro>();
        textMesh.font = GetDefaultFontAsset();
        textMesh.text = content;
        textMesh.fontSize = fontSize;
        textMesh.alignment = ConvertAlignment(anchor);
        textMesh.color = color;
        textMesh.enableWordWrapping = false;

        return textMesh;
    }

    public static void SetColor(GameObject gameObject, Color color)
    {
        var renderer = gameObject != null ? gameObject.GetComponent<Renderer>() : null;

        if (renderer != null)
        {
            if (renderer.sharedMaterial == null)
            {
                renderer.sharedMaterial = WorldMaterialPalette.CreateMaterial(color);
            }

            renderer.material.color = color;
        }
    }

    public static void SetMaterial(GameObject gameObject, Material material)
    {
        var renderer = gameObject != null ? gameObject.GetComponent<Renderer>() : null;

        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static TMP_FontAsset GetDefaultFontAsset()
    {
        if (defaultFontAsset == null)
        {
            defaultFontAsset = TMP_Settings.defaultFontAsset;

            if (defaultFontAsset == null)
            {
                defaultFontAsset = Resources.Load<TMP_FontAsset>("TextMesh Pro/Fonts & Materials/LiberationSans SDF");
            }
        }

        return defaultFontAsset;
    }

    private static TextAlignmentOptions ConvertAlignment(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter:
                return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight:
                return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft:
                return TextAlignmentOptions.Left;
            case TextAnchor.MiddleRight:
                return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft:
                return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter:
                return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight:
                return TextAlignmentOptions.BottomRight;
            default:
                return TextAlignmentOptions.Center;
        }
    }
}
