using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public sealed class LevelEditorView : MonoBehaviour, ILevelEditorView
{
    private Transform root;
    private TextMeshPro summaryText;
    private readonly System.Collections.Generic.Dictionary<PixelPigColor, GameObject> colorButtons =
        new System.Collections.Generic.Dictionary<PixelPigColor, GameObject>();

    public event Action<PixelPigColor> ColorSelected;
    public event Action<int, int> ResizeRequested;
    public event Action<int> SlotCountChanged;
    public event Action<PixelPigColor> PigAdded;
    public event Action RemoveLastPigRequested;
    public event Action ApplyRequested;
    public event Action SaveRequested;
    public event Action LoadRequested;
    public event Action ResetRequested;

    public void Initialize(Transform parent)
    {
        root = new GameObject("LevelEditor").transform;
        root.SetParent(parent, false);
        root.localPosition = new Vector3(16.2F, 0F, 0F);

        var panel = WorldObjectUtility.CreatePrimitive(
            "EditorPanel",
            PrimitiveType.Cube,
            root,
            new Vector3(0F, 0.2F, 0F),
            new Vector3(4.4F, 0.15F, 12.8F),
            WorldMaterialRole.EditorPanel);
        Destroy(panel.GetComponent<Collider>());

        WorldObjectUtility.CreateWorldText(
            "EditorTitle",
            root,
            new Vector3(0F, 0.3F, 5.5F),
            "Level Designer",
            56,
            Color.white,
            TextAnchor.MiddleCenter,
            0.08F);

        CreateColorButtons();
        CreateResizeButtons();
        CreateSlotButtons();
        CreatePigButtons();
        CreateActionButtons();

        summaryText = WorldObjectUtility.CreateWorldText(
            "Summary",
            root,
            new Vector3(0F, 0.3F, -4.1F),
            string.Empty,
            42,
            new Color32(224, 232, 252, 255),
            TextAnchor.UpperCenter,
            0.06F);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);

        if (root != null)
        {
            root.gameObject.SetActive(visible);
        }
    }

    public void SetSelectedColor(PixelPigColor color)
    {
        foreach (var pair in colorButtons)
        {
            WorldObjectUtility.SetColor(pair.Value,
                pair.Key == color ? Color.Lerp(pair.Key.ToUnityColor(), Color.white, 0.2F) : pair.Key.ToUnityColor());
        }
    }

    public void SetSummary(PixelFlowLevelData levelData)
    {
        if (summaryText == null || levelData == null)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Grid: {levelData.width} x {levelData.height}");
        builder.AppendLine($"Slots: {levelData.waitingSlotCount}");
        var hasExactAmmo = PixelFlowLevelAnalyzer.HasExactAmmoForBoard(levelData);
        var pigCount = PixelFlowLevelAnalyzer.CountPigs(levelData);
        var isStartSolvable = PixelFlowLevelAnalyzer.IsStartSolvable(levelData);
        builder.AppendLine(hasExactAmmo ? "Ammo: Exact" : "Ammo: Mismatch");
        builder.AppendLine($"Pigs: {pigCount}");
        builder.AppendLine(isStartSolvable ? "Start solvable: Yes" : "Start solvable: No");
        builder.AppendLine("Configured pigs:");

        var configuredPigs = new List<PigSpawnData>();

        if (levelData.pigLines != null)
        {
            for (var lineIndex = 0; lineIndex < levelData.pigLines.Length; lineIndex++)
            {
                var line = levelData.pigLines[lineIndex];

                if (line?.pigs == null)
                {
                    continue;
                }

                for (var pigIndex = 0; pigIndex < line.pigs.Length; pigIndex++)
                {
                    configuredPigs.Add(line.pigs[pigIndex]);
                }
            }
        }

        if (configuredPigs.Count == 0)
        {
            builder.AppendLine("None");
        }
        else
        {
            for (var i = 0; i < configuredPigs.Count; i++)
            {
                var pig = configuredPigs[i];
                builder.AppendLine($"{i + 1}. {pig.color.ToDisplayName()} ({pig.ammo})");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Click cells to paint.");
        summaryText.text = builder.ToString();
    }

    private void CreateColorButtons()
    {
        var colors = new[] { PixelPigColor.Red, PixelPigColor.Blue, PixelPigColor.Green, PixelPigColor.Yellow, PixelPigColor.None };

        for (var i = 0; i < colors.Length; i++)
        {
            var color = colors[i];
            var button = CreateButton(
                $"{color}Button",
                new Vector3(i % 2 == 0 ? -1.1F : 1.1F, 0.35F, 4.2F - (i / 2) * 1.1F),
                color.ToUnityColor(),
                color == PixelPigColor.None ? "Erase" : color.ToDisplayName(),
                () => ColorSelected?.Invoke(color));
            colorButtons.Add(color, button);
        }
    }

    private void CreateResizeButtons()
    {
        CreateButton("Wider", new Vector3(-1.1F, 0.35F, 1.5F), WorldMaterialRole.EditorButtonBlue, "W +",
            () => ResizeRequested?.Invoke(1, 0));
        CreateButton("Narrower", new Vector3(1.1F, 0.35F, 1.5F), WorldMaterialRole.EditorButtonBlue, "W -",
            () => ResizeRequested?.Invoke(-1, 0));
        CreateButton("Taller", new Vector3(-1.1F, 0.35F, 0.4F), WorldMaterialRole.EditorButtonBlue, "H +",
            () => ResizeRequested?.Invoke(0, 1));
        CreateButton("Shorter", new Vector3(1.1F, 0.35F, 0.4F), WorldMaterialRole.EditorButtonBlue, "H -",
            () => ResizeRequested?.Invoke(0, -1));
    }

    private void CreateSlotButtons()
    {
        CreateButton("SlotAdd", new Vector3(-1.1F, 0.35F, -0.9F), WorldMaterialRole.EditorButtonGreen, "Slot +",
            () => SlotCountChanged?.Invoke(1));
        CreateButton("SlotRemove", new Vector3(1.1F, 0.35F, -0.9F), WorldMaterialRole.EditorButtonRed, "Slot -",
            () => SlotCountChanged?.Invoke(-1));
    }

    private void CreatePigButtons()
    {
        var colors = new[] { PixelPigColor.Red, PixelPigColor.Blue, PixelPigColor.Green, PixelPigColor.Yellow };

        for (var i = 0; i < colors.Length; i++)
        {
            var color = colors[i];
            CreateButton(
                $"Add{color}",
                new Vector3(i % 2 == 0 ? -1.1F : 1.1F, 0.35F, -2.4F - (i / 2) * 1.1F),
                color.ToUnityColor(),
                $"+ {color.ToDisplayName()}",
                () => PigAdded?.Invoke(color));
        }

        CreateButton("RemoveLastPig", new Vector3(0F, 0.35F, -4.8F), WorldMaterialRole.EditorButtonPurple, "Remove Last",
            () => RemoveLastPigRequested?.Invoke(), new Vector3(2.3F, 0.3F, 0.75F));
    }

    private void CreateActionButtons()
    {
        CreateButton("Apply", new Vector3(-1.1F, 0.35F, -6F), WorldMaterialRole.EditorButtonGreen, "Apply",
            () => ApplyRequested?.Invoke());
        CreateButton("Save", new Vector3(1.1F, 0.35F, -6F), WorldMaterialRole.EditorButtonBlue, "Save",
            () => SaveRequested?.Invoke());
        CreateButton("Load", new Vector3(-1.1F, 0.35F, -7.2F), WorldMaterialRole.EditorButtonBlue, "Load Saved",
            () => LoadRequested?.Invoke());
        CreateButton("Reset", new Vector3(1.1F, 0.35F, -7.2F), WorldMaterialRole.EditorButtonRed, "Default",
            () => ResetRequested?.Invoke());
    }

    private GameObject CreateButton(string name, Vector3 localPosition, WorldMaterialRole materialRole, string label, Action onClick)
    {
        return CreateButton(name, localPosition, materialRole, label, onClick, new Vector3(1.8F, 0.3F, 0.75F));
    }

    private GameObject CreateButton(string name, Vector3 localPosition, Color color, string label, Action onClick)
    {
        return CreateButton(name, localPosition, color, label, onClick, new Vector3(1.8F, 0.3F, 0.75F));
    }

    private GameObject CreateButton(string name, Vector3 localPosition, WorldMaterialRole materialRole, string label, Action onClick, Vector3 scale)
    {
        var button = WorldObjectUtility.CreatePrimitive(name, PrimitiveType.Cube, root, localPosition, scale, materialRole);
        var relay = button.AddComponent<ClickRelay>();
        relay.Clicked += onClick;

        WorldObjectUtility.CreateWorldText(
            "Label",
            button.transform,
            new Vector3(0F, 0.6F, 0F),
            label,
            40,
            Color.white,
            TextAnchor.MiddleCenter,
            0.055F);

        return button;
    }

    private GameObject CreateButton(string name, Vector3 localPosition, Color color, string label, Action onClick, Vector3 scale)
    {
        var button = WorldObjectUtility.CreatePrimitive(name, PrimitiveType.Cube, root, localPosition, scale, color);
        var relay = button.AddComponent<ClickRelay>();
        relay.Clicked += onClick;

        WorldObjectUtility.CreateWorldText(
            "Label",
            button.transform,
            new Vector3(0F, 0.6F, 0F),
            label,
            40,
            Color.white,
            TextAnchor.MiddleCenter,
            0.055F);

        return button;
    }
}
