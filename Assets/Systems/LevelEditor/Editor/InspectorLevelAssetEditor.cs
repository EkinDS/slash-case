using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

[CustomEditor(typeof(InspectorLevelAsset))]
public sealed class InspectorLevelAssetEditor : Editor
{
    private const float CellSize = 22F;
    private const string LevelsFolderPath = "Assets/Resources/Levels";

    private int selectedColorIndex;

    public override void OnInspectorGUI()
    {
        var asset = (InspectorLevelAsset)target;

        serializedObject.Update();

        DrawSaveLoadToolbar(asset);
        EditorGUILayout.Space(10F);
        DrawBoardSettings(asset);
        EditorGUILayout.Space(10F);
        DrawPalette(asset);
        EditorGUILayout.Space(10F);
        DrawBoard(asset);
        EditorGUILayout.Space(10F);
        DrawBoardColorUsage(asset);
        EditorGUILayout.Space(10F);
        DrawPigLines(asset);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(asset);
        }
    }

    private void DrawSaveLoadToolbar(InspectorLevelAsset asset)
    {
        EditorGUILayout.LabelField("Level File", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        asset.id = EditorGUILayout.IntField("Id", asset.id);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(asset, "Edit Level Id");
            asset.id = Mathf.Max(1, asset.id);
            EditorUtility.SetDirty(asset);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = asset.id > 0;

            if (GUILayout.Button("Save"))
            {
                SaveLevel(asset);
            }

            if (GUILayout.Button("Load"))
            {
                LoadLevel(asset);
            }

            GUI.enabled = true;
        }
    }

    private static void DrawBoardSettings(InspectorLevelAsset asset)
    {
        EditorGUILayout.LabelField("Board", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        asset.width = EditorGUILayout.IntSlider("Width", asset.width, 4, 20);
        asset.height = EditorGUILayout.IntSlider("Height", asset.height, 4, 20);
        asset.waitingSlotCount = EditorGUILayout.IntSlider("Waiting Slots", asset.waitingSlotCount, 2, 5);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(asset, "Change Board Settings");
            asset.ResizeBoardStorage();
        }
    }

    private void DrawPalette(InspectorLevelAsset asset)
    {
        EditorGUILayout.LabelField("Palette", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Color"))
        {
            Undo.RecordObject(asset, "Add Color");
            asset.colors.Add(new InspectorLevelColor());
            EditorUtility.SetDirty(asset);
        }

        for (var i = 0; i < asset.colors.Count; i++)
        {
            var colorEntry = asset.colors[i];

            if (colorEntry == null)
            {
                asset.colors[i] = new InspectorLevelColor();
                colorEntry = asset.colors[i];
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(selectedColorIndex == i, "Use", "Button", GUILayout.Width(48F)))
                    {
                        selectedColorIndex = i;
                    }

                    var previewRect = GUILayoutUtility.GetRect(28F, 18F, GUILayout.Width(28F));
                    EditorGUI.DrawRect(previewRect, colorEntry.ToColor());

                    if (GUILayout.Button("Remove", GUILayout.Width(64F)))
                    {
                        Undo.RecordObject(asset, "Remove Color");
                        RemoveColor(asset, i);
                        EditorUtility.SetDirty(asset);
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUI.BeginChangeCheck();
                var hexValue = EditorGUILayout.TextField("Hex", colorEntry.hex);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(asset, "Edit Color Hex");
                    colorEntry.hex = hexValue;
                    colorEntry.SyncRgbFromHex();
                    EditorUtility.SetDirty(asset);
                }

            }
        }

        if (asset.colors.Count == 0)
        {
            EditorGUILayout.HelpBox("Add at least one hex color to paint the board.", MessageType.Info);
            selectedColorIndex = -1;
        }
        else
        {
            selectedColorIndex = Mathf.Clamp(selectedColorIndex, 0, asset.colors.Count - 1);
        }
    }

    private void DrawBoard(InspectorLevelAsset asset)
    {
        EditorGUILayout.LabelField("Board Paint", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = asset.colors.Count > 0;
            if (GUILayout.Button("Fill Selected Color"))
            {
                Undo.RecordObject(asset, "Fill Board");
                for (var y = 0; y < asset.height; y++)
                {
                    for (var x = 0; x < asset.width; x++)
                    {
                        asset.SetCellColorIndex(x, y, selectedColorIndex);
                    }
                }
            }

            GUI.enabled = true;

            if (GUILayout.Button("Clear Board"))
            {
                Undo.RecordObject(asset, "Clear Board");
                for (var y = 0; y < asset.height; y++)
                {
                    for (var x = 0; x < asset.width; x++)
                    {
                        asset.SetCellColorIndex(x, y, -1);
                    }
                }
            }
        }

        var boardRect = GUILayoutUtility.GetRect(asset.width * CellSize, asset.height * CellSize);
        var currentEvent = Event.current;

        for (var y = 0; y < asset.height; y++)
        {
            for (var x = 0; x < asset.width; x++)
            {
                var rect = new Rect(boardRect.x + x * CellSize, boardRect.y + y * CellSize, CellSize - 1F, CellSize - 1F);
                var colorIndex = asset.GetCellColorIndex(x, y);
                EditorGUI.DrawRect(rect, asset.GetPreviewColor(colorIndex));
                Handles.DrawSolidRectangleWithOutline(rect, Color.clear, new Color(0F, 0F, 0F, 0.35F));

                if (!rect.Contains(currentEvent.mousePosition))
                {
                    continue;
                }

                if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) && currentEvent.button == 0)
                {
                    Undo.RecordObject(asset, "Paint Cell");
                    asset.SetCellColorIndex(x, y, selectedColorIndex);
                    EditorUtility.SetDirty(asset);
                    currentEvent.Use();
                }

                if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) && currentEvent.button == 1)
                {
                    Undo.RecordObject(asset, "Erase Cell");
                    asset.SetCellColorIndex(x, y, -1);
                    EditorUtility.SetDirty(asset);
                    currentEvent.Use();
                }
            }
        }

        EditorGUILayout.HelpBox("Left click paints with the selected hex color. Right click clears a cell.", MessageType.None);
    }

    private void DrawPigLines(InspectorLevelAsset asset)
    {
        EditorGUILayout.LabelField("Pig Lines", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Line"))
        {
            Undo.RecordObject(asset, "Add Line");
            asset.pigLines.Add(new InspectorPigLine());
            EditorUtility.SetDirty(asset);
        }

        var labels = asset.BuildColorLabels();

        for (var lineIndex = 0; lineIndex < asset.pigLines.Count; lineIndex++)
        {
            var line = asset.pigLines[lineIndex];

            if (line == null)
            {
                asset.pigLines[lineIndex] = new InspectorPigLine();
                line = asset.pigLines[lineIndex];
            }

            if (line.pigs == null)
            {
                line.pigs = new System.Collections.Generic.List<InspectorPigEntry>();
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Line {lineIndex + 1}", EditorStyles.boldLabel);

                    if (GUILayout.Button("Add Pig", GUILayout.Width(72F)))
                    {
                        Undo.RecordObject(asset, "Add Pig");
                        line.pigs.Add(new InspectorPigEntry
                        {
                            colorIndex = asset.colors.Count > 0 ? 0 : -1,
                            ammo = 1
                        });
                        EditorUtility.SetDirty(asset);
                    }

                    if (GUILayout.Button("Remove Line", GUILayout.Width(96F)))
                    {
                        Undo.RecordObject(asset, "Remove Line");
                        asset.pigLines.RemoveAt(lineIndex);
                        EditorUtility.SetDirty(asset);
                        GUIUtility.ExitGUI();
                    }
                }

                if (line.pigs.Count == 0)
                {
                    EditorGUILayout.HelpBox("This line has no pigs yet.", MessageType.None);
                }

                for (var pigIndex = 0; pigIndex < line.pigs.Count; pigIndex++)
                {
                    var pig = line.pigs[pigIndex];

                    if (pig == null)
                    {
                        line.pigs[pigIndex] = new InspectorPigEntry();
                        pig = line.pigs[pigIndex];
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Pig {pigIndex + 1}", GUILayout.Width(42F));

                        if (labels.Length > 0)
                        {
                            pig.colorIndex = EditorGUILayout.Popup(pig.colorIndex, labels);
                        }
                        else
                        {
                            pig.colorIndex = -1;
                            EditorGUILayout.LabelField("Add palette colors first");
                        }

                        pig.ammo = EditorGUILayout.IntField("Bullets", pig.ammo);
                        pig.ammo = Mathf.Max(0, pig.ammo);

                        if (GUILayout.Button("Remove", GUILayout.Width(64F)))
                        {
                            Undo.RecordObject(asset, "Remove Pig");
                            line.pigs.RemoveAt(pigIndex);
                            EditorUtility.SetDirty(asset);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
        }
    }

    private void DrawBoardColorUsage(InspectorLevelAsset asset)
    {
        EditorGUILayout.LabelField("Board Color Usage", EditorStyles.boldLabel);

        if (asset.colors.Count == 0)
        {
            EditorGUILayout.HelpBox("Add palette colors to see board color counts.", MessageType.None);
            return;
        }

        var labels = asset.BuildColorLabels();
        var hasUsedColor = false;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            for (var i = 0; i < asset.colors.Count; i++)
            {
                var count = asset.CountCellsWithColor(i);

                if (count <= 0)
                {
                    continue;
                }

                hasUsedColor = true;
                EditorGUILayout.LabelField($"{labels[i]}: {count}");
            }

            if (!hasUsedColor)
            {
                EditorGUILayout.LabelField("No painted cells");
            }
        }
    }

    private void RemoveColor(InspectorLevelAsset asset, int colorIndex)
    {
        asset.colors.RemoveAt(colorIndex);

        for (var i = 0; i < asset.cellColorIndices.Count; i++)
        {
            if (asset.cellColorIndices[i] == colorIndex)
            {
                asset.cellColorIndices[i] = -1;
            }
            else if (asset.cellColorIndices[i] > colorIndex)
            {
                asset.cellColorIndices[i]--;
            }
        }

        for (var lineIndex = 0; lineIndex < asset.pigLines.Count; lineIndex++)
        {
            var line = asset.pigLines[lineIndex];

            if (line == null || line.pigs == null)
            {
                continue;
            }

            for (var pigIndex = 0; pigIndex < line.pigs.Count; pigIndex++)
            {
                if (line.pigs[pigIndex] == null)
                {
                    continue;
                }

                if (line.pigs[pigIndex].colorIndex == colorIndex)
                {
                    line.pigs[pigIndex].colorIndex = asset.colors.Count > 0 ? 0 : -1;
                }
                else if (line.pigs[pigIndex].colorIndex > colorIndex)
                {
                    line.pigs[pigIndex].colorIndex--;
                }
            }
        }

        if (asset.colors.Count == 0)
        {
            selectedColorIndex = -1;
        }
        else
        {
            selectedColorIndex = Mathf.Clamp(selectedColorIndex, 0, asset.colors.Count - 1);
        }
    }

    private static void SaveLevel(InspectorLevelAsset asset)
    {
        asset.id = Mathf.Max(1, asset.id);
        Directory.CreateDirectory(LevelsFolderPath);

        var filePath = ResolveLevelFilePath(asset.id);
        var fileData = asset.ToFileData();
        var json = JsonUtility.ToJson(fileData, true);

        File.WriteAllText(filePath, json);
        AssetDatabase.Refresh();
        EditorUtility.SetDirty(asset);
    }

    private void LoadLevel(InspectorLevelAsset asset)
    {
        asset.id = Mathf.Max(1, asset.id);

        var filePath = ResolveLevelFilePath(asset.id);

        if (!File.Exists(filePath))
        {
            EditorUtility.DisplayDialog("Level Not Found", $"No level file exists at {filePath}.", "OK");
            return;
        }

        var json = File.ReadAllText(filePath);
        var fileData = JsonUtility.FromJson<InspectorLevelFileData>(json);

        Undo.RecordObject(asset, "Load Level");
        asset.ApplyFileData(fileData);
        selectedColorIndex = asset.colors.Count > 0 ? Mathf.Clamp(selectedColorIndex, 0, asset.colors.Count - 1) : -1;
        EditorUtility.SetDirty(asset);
    }

    private static string BuildLevelFileName(int levelId)
    {
        return $"Level{levelId:D2}.json";
    }

    private static string ResolveLevelFilePath(int levelId)
    {
        var preferredPath = Path.Combine(LevelsFolderPath, BuildLevelFileName(levelId));

        if (File.Exists(preferredPath))
        {
            return preferredPath;
        }

        if (levelId == 1)
        {
            var defaultLevelPath = Path.Combine(LevelsFolderPath, "DefaultLevel.json");

            if (File.Exists(defaultLevelPath))
            {
                return defaultLevelPath;
            }
        }

        if (!Directory.Exists(LevelsFolderPath))
        {
            return preferredPath;
        }

        var candidatePaths = Directory.GetFiles(LevelsFolderPath, "*.json");

        for (var i = 0; i < candidatePaths.Length; i++)
        {
            var candidatePath = candidatePaths[i];
            var json = File.ReadAllText(candidatePath);
            var levelData = JsonUtility.FromJson<PixelFlowLevelData>(json);
            var candidateId = levelData != null && levelData.id > 0
                ? levelData.id
                : ParseLevelIdFromPath(candidatePath);

            if (candidateId == levelId)
            {
                return candidatePath;
            }
        }

        return preferredPath;
    }

    private static int ParseLevelIdFromPath(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        if (string.Equals(fileName, "DefaultLevel", System.StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        var digits = new string(fileName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var parsedId) ? parsedId : 0;
    }
}
