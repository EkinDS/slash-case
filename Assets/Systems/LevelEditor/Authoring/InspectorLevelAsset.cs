using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InspectorLevel", menuName = "SlashCase/Inspector Level Asset")]
public sealed class InspectorLevelAsset : ScriptableObject
{
    private const int MinBoardSize = 4;
    private const int MaxBoardSize = 20;
    private const int MinWaitingSlots = 2;
    private const int MaxWaitingSlots = 5;

    [Min(1)] public int id = 1;
    [Range(MinBoardSize, MaxBoardSize)] public int width = 8;
    [Range(MinBoardSize, MaxBoardSize)] public int height = 8;
    [Range(MinWaitingSlots, MaxWaitingSlots)] public int waitingSlotCount = MinWaitingSlots;
    public List<InspectorLevelColor> colors = new List<InspectorLevelColor>();
    public List<int> cellColorIndices = new List<int>();
    public List<InspectorPigLine> pigLines = new List<InspectorPigLine>();

    private void OnValidate()
    {
        id = Mathf.Max(1, id);
        width = Mathf.Clamp(width, MinBoardSize, MaxBoardSize);
        height = Mathf.Clamp(height, MinBoardSize, MaxBoardSize);
        waitingSlotCount = Mathf.Clamp(waitingSlotCount, MinWaitingSlots, MaxWaitingSlots);

        if (colors == null)
        {
            colors = new List<InspectorLevelColor>();
        }

        if (cellColorIndices == null)
        {
            cellColorIndices = new List<int>();
        }

        if (pigLines == null)
        {
            pigLines = new List<InspectorPigLine>();
        }

        ResizeBoardStorage();
        ClampBoardIndices();
        ClampPigLines();
    }

    public int GetCellColorIndex(int x, int y)
    {
        var index = GetFlatIndex(x, y);
        return index >= 0 && index < cellColorIndices.Count ? cellColorIndices[index] : -1;
    }

    public void SetCellColorIndex(int x, int y, int colorIndex)
    {
        ResizeBoardStorage();
        var index = GetFlatIndex(x, y);

        if (index >= 0 && index < cellColorIndices.Count)
        {
            cellColorIndices[index] = colorIndex;
        }
    }

    public void ResizeBoardStorage()
    {
        var targetCount = width * height;

        while (cellColorIndices.Count < targetCount)
        {
            cellColorIndices.Add(-1);
        }

        while (cellColorIndices.Count > targetCount)
        {
            cellColorIndices.RemoveAt(cellColorIndices.Count - 1);
        }
    }

    public Color GetPreviewColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= colors.Count || colors[colorIndex] == null)
        {
            return new Color(0.14F, 0.16F, 0.2F, 1F);
        }

        return colors[colorIndex].ToColor();
    }

    public string[] BuildColorLabels()
    {
        var labels = new string[colors.Count];

        for (var i = 0; i < colors.Count; i++)
        {
            labels[i] = colors[i] != null && !string.IsNullOrWhiteSpace(colors[i].hex)
                ? colors[i].hex
                : $"Color {i + 1}";
        }

        return labels;
    }

    public int CountCellsWithColor(int colorIndex)
    {
        var count = 0;

        for (var i = 0; i < cellColorIndices.Count; i++)
        {
            if (cellColorIndices[i] == colorIndex)
            {
                count++;
            }
        }

        return count;
    }

    public InspectorLevelFileData ToFileData()
    {
        var cells = new List<PixelCellData>();

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var colorIndex = GetCellColorIndex(x, y);

                if (colorIndex < 0)
                {
                    continue;
                }

                cells.Add(new PixelCellData(x, y, (PixelPigColor)colorIndex));
            }
        }

        var serializedPigLines = new PigLineData[pigLines.Count];
        var pigQueue = new List<PigSpawnData>();

        for (var lineIndex = 0; lineIndex < pigLines.Count; lineIndex++)
        {
            var line = pigLines[lineIndex];
            var linePigs = line != null && line.pigs != null
                ? line.pigs
                : new List<InspectorPigEntry>();
            var serializedPigs = new PigSpawnData[linePigs.Count];

            for (var pigIndex = 0; pigIndex < linePigs.Count; pigIndex++)
            {
                var pig = linePigs[pigIndex] ?? new InspectorPigEntry();
                var spawnData = new PigSpawnData((PixelPigColor)Mathf.Max(0, pig.colorIndex), Mathf.Max(0, pig.ammo));
                serializedPigs[pigIndex] = spawnData;
                pigQueue.Add(new PigSpawnData(spawnData.color, spawnData.ammo));
            }

            serializedPigLines[lineIndex] = new PigLineData
            {
                pigs = serializedPigs
            };
        }

        return new InspectorLevelFileData
        {
            id = id,
            width = width,
            height = height,
            waitingSlotCount = waitingSlotCount,
            colors = colors != null ? colors.ToArray() : Array.Empty<InspectorLevelColor>(),
            cells = cells.ToArray(),
            pigLines = serializedPigLines,
            pigQueue = pigQueue.ToArray()
        };
    }

    public void ApplyFileData(InspectorLevelFileData fileData)
    {
        if (fileData == null)
        {
            return;
        }

        id = Mathf.Max(1, fileData.id);
        width = Mathf.Clamp(fileData.width, MinBoardSize, MaxBoardSize);
        height = Mathf.Clamp(fileData.height, MinBoardSize, MaxBoardSize);
        waitingSlotCount = Mathf.Clamp(fileData.waitingSlotCount, MinWaitingSlots, MaxWaitingSlots);
        colors = fileData.colors != null ? new List<InspectorLevelColor>(fileData.colors) : new List<InspectorLevelColor>();
        cellColorIndices = new List<int>();
        pigLines = new List<InspectorPigLine>();

        EnsureColorCapacityForFileData(fileData);
        ResizeBoardStorage();

        for (var i = 0; i < cellColorIndices.Count; i++)
        {
            cellColorIndices[i] = -1;
        }

        if (fileData.cells != null)
        {
            for (var i = 0; i < fileData.cells.Length; i++)
            {
                var cell = fileData.cells[i];

                if (cell == null || cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
                {
                    continue;
                }

                SetCellColorIndex(cell.x, cell.y, (int)cell.color);
            }
        }

        if (fileData.pigLines != null)
        {
            for (var lineIndex = 0; lineIndex < fileData.pigLines.Length; lineIndex++)
            {
                var sourceLine = fileData.pigLines[lineIndex];
                var line = new InspectorPigLine();

                if (sourceLine != null && sourceLine.pigs != null)
                {
                    for (var pigIndex = 0; pigIndex < sourceLine.pigs.Length; pigIndex++)
                    {
                        var pig = sourceLine.pigs[pigIndex];

                        if (pig == null)
                        {
                            continue;
                        }

                        line.pigs.Add(new InspectorPigEntry
                        {
                            colorIndex = (int)pig.color,
                            ammo = pig.ammo
                        });
                    }
                }

                pigLines.Add(line);
            }
        }

        ClampBoardIndices();
        ClampPigLines();
    }

    private void EnsureColorCapacityForFileData(InspectorLevelFileData fileData)
    {
        var highestColorIndex = -1;

        if (fileData.cells != null)
        {
            for (var i = 0; i < fileData.cells.Length; i++)
            {
                if (fileData.cells[i] != null)
                {
                    highestColorIndex = Mathf.Max(highestColorIndex, (int)fileData.cells[i].color);
                }
            }
        }

        if (fileData.pigLines != null)
        {
            for (var lineIndex = 0; lineIndex < fileData.pigLines.Length; lineIndex++)
            {
                var line = fileData.pigLines[lineIndex];

                if (line == null || line.pigs == null)
                {
                    continue;
                }

                for (var pigIndex = 0; pigIndex < line.pigs.Length; pigIndex++)
                {
                    if (line.pigs[pigIndex] != null)
                    {
                        highestColorIndex = Mathf.Max(highestColorIndex, (int)line.pigs[pigIndex].color);
                    }
                }
            }
        }

        for (var colorIndex = colors.Count; colorIndex <= highestColorIndex; colorIndex++)
        {
            colors.Add(CreateFallbackColor(colorIndex));
        }
    }

    private static InspectorLevelColor CreateFallbackColor(int index)
    {
        var fallbackHex = index switch
        {
            0 => "#F94144",
            1 => "#F3722C",
            2 => "#F9C74F",
            3 => "#90BE6D",
            4 => "#43AA8B",
            5 => "#577590",
            _ => "#FFFFFF"
        };

        var color = new InspectorLevelColor
        {
            hex = fallbackHex
        };
        color.SyncRgbFromHex();
        return color;
    }

    private int GetFlatIndex(int x, int y)
    {
        return y * width + x;
    }

    private void ClampBoardIndices()
    {
        for (var i = 0; i < cellColorIndices.Count; i++)
        {
            if (cellColorIndices[i] < -1)
            {
                cellColorIndices[i] = -1;
            }

            if (cellColorIndices[i] >= colors.Count)
            {
                cellColorIndices[i] = -1;
            }
        }
    }

    private void ClampPigLines()
    {
        for (var i = 0; i < pigLines.Count; i++)
        {
            if (pigLines[i] == null)
            {
                pigLines[i] = new InspectorPigLine();
            }

            if (pigLines[i].pigs == null)
            {
                pigLines[i].pigs = new List<InspectorPigEntry>();
            }

            for (var pigIndex = 0; pigIndex < pigLines[i].pigs.Count; pigIndex++)
            {
                if (pigLines[i].pigs[pigIndex] == null)
                {
                    pigLines[i].pigs[pigIndex] = new InspectorPigEntry();
                }

                pigLines[i].pigs[pigIndex].ammo = Mathf.Max(0, pigLines[i].pigs[pigIndex].ammo);

                if (pigLines[i].pigs[pigIndex].colorIndex < 0 || pigLines[i].pigs[pigIndex].colorIndex >= colors.Count)
                {
                    pigLines[i].pigs[pigIndex].colorIndex = colors.Count > 0 ? 0 : -1;
                }
            }
        }
    }
}

[Serializable]
public sealed class InspectorLevelColor
{
    public string hex = "#FFFFFF";
    [Range(0, 255)] public int red = 255;
    [Range(0, 255)] public int green = 255;
    [Range(0, 255)] public int blue = 255;

    public Color ToColor()
    {
        return new Color32((byte)red, (byte)green, (byte)blue, 255);
    }

    public void SyncHexFromRgb()
    {
        hex = $"#{red:X2}{green:X2}{blue:X2}";
    }

    public void SyncRgbFromHex()
    {
        if (TryParseHexColor(hex, out var parsed))
        {
            red = parsed.r;
            green = parsed.g;
            blue = parsed.b;
            hex = $"#{red:X2}{green:X2}{blue:X2}";
        }
    }

    public static bool TryParseHexColor(string value, out Color32 color)
    {
        color = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        if (trimmed.StartsWith("#"))
        {
            trimmed = trimmed.Substring(1);
        }

        if (trimmed.Length != 6)
        {
            return false;
        }

        if (!byte.TryParse(trimmed.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var r) ||
            !byte.TryParse(trimmed.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) ||
            !byte.TryParse(trimmed.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return false;
        }

        color = new Color32(r, g, b, 255);
        return true;
    }
}

[Serializable]
public sealed class InspectorPigEntry
{
    public int colorIndex;
    [Min(0)] public int ammo = 1;
}

[Serializable]
public sealed class InspectorPigLine
{
    public List<InspectorPigEntry> pigs = new List<InspectorPigEntry>();
}

[Serializable]
public sealed class InspectorLevelFileData
{
    public int id;
    public int width;
    public int height;
    public int waitingSlotCount;
    public InspectorLevelColor[] colors = Array.Empty<InspectorLevelColor>();
    public PixelCellData[] cells = Array.Empty<PixelCellData>();
    public PigLineData[] pigLines = Array.Empty<PigLineData>();
    public PigSpawnData[] pigQueue = Array.Empty<PigSpawnData>();
}
