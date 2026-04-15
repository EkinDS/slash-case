using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class WorldMaterialPalette
{
    private static readonly Dictionary<WorldMaterialRole, Material> Materials = new Dictionary<WorldMaterialRole, Material>();

    public static Material Get(WorldMaterialRole role)
    {
        if (Materials.TryGetValue(role, out var material))
        {
            return material;
        }

        material = CreateMaterial(GetColor(role));
        material.name = $"{role}Material";
        Materials.Add(role, material);
        return material;
    }

    public static Material CreateMaterial(Color color)
    {
        var shader = ResolveRenderableShader();
        var material = new Material(shader)
        {
            color = color
        };
        return material;
    }

    private static Shader ResolveRenderableShader()
    {
        var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var primitiveRenderer = primitive.GetComponent<Renderer>();
        var primitiveShader = primitiveRenderer != null && primitiveRenderer.sharedMaterial != null
            ? primitiveRenderer.sharedMaterial.shader
            : null;
        Object.Destroy(primitive);

        if (primitiveShader != null)
        {
            return primitiveShader;
        }

        var pipelineMaterial = GraphicsSettings.defaultRenderPipeline != null
            ? GraphicsSettings.defaultRenderPipeline.defaultMaterial
            : null;

        if (pipelineMaterial != null && pipelineMaterial.shader != null)
        {
            return pipelineMaterial.shader;
        }

        return Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");
    }

    private static Color GetColor(WorldMaterialRole role)
    {
        switch (role)
        {
            case WorldMaterialRole.BoardBase:
                return new Color32(46, 52, 68, 255);
            case WorldMaterialRole.Conveyor:
                return new Color32(80, 82, 127, 255);
            case WorldMaterialRole.WaitingSlotEmpty:
                return new Color32(50, 50, 80, 255);
            case WorldMaterialRole.HudButtonPrimary:
                return new Color32(83, 126, 218, 255);
            case WorldMaterialRole.HudButtonSecondary:
                return new Color32(66, 174, 143, 255);
            case WorldMaterialRole.EditorPanel:
                return new Color32(61, 67, 82, 255);
            case WorldMaterialRole.EditorButtonBlue:
                return new Color32(83, 110, 164, 255);
            case WorldMaterialRole.EditorButtonGreen:
                return new Color32(79, 152, 111, 255);
            case WorldMaterialRole.EditorButtonRed:
                return new Color32(167, 90, 90, 255);
            case WorldMaterialRole.EditorButtonPurple:
                return new Color32(118, 92, 156, 255);
            default:
                return new Color32(120, 120, 120, 255);
        }
    }
}
