using System;
using System.Collections.Generic;

public sealed class LevelEditorPresenter : IDisposable
{
    private const int FixedBoardSize = 20;

    private readonly ILevelEditorView view;
    private readonly PixelFlowLevelSaveLoad saveLoad;
    private readonly Func<PixelFlowLevelData> getDefaultLevel;
    private readonly Action<PixelFlowLevelData> applyLevel;

    private PixelFlowLevelData workingLevel;

    public LevelEditorPresenter(ILevelEditorView view, PixelFlowLevelSaveLoad saveLoad, Func<PixelFlowLevelData> getDefaultLevel,
        Action<PixelFlowLevelData> applyLevel)
    {
        this.view = view;
        this.saveLoad = saveLoad;
        this.getDefaultLevel = getDefaultLevel;
        this.applyLevel = applyLevel;

        view.ColorSelected += OnColorSelected;
        view.ResizeRequested += OnResizeRequested;
        view.SlotCountChanged += OnSlotCountChanged;
        view.PigAdded += OnPigAdded;
        view.RemoveLastPigRequested += OnRemoveLastPigRequested;
        view.ApplyRequested += OnApplyRequested;
        view.SaveRequested += OnSaveRequested;
        view.LoadRequested += OnLoadRequested;
        view.ResetRequested += OnResetRequested;
    }

    public PixelPigColor SelectedColor { get; private set; } = PixelPigColor.Red;

    public void SetLevel(PixelFlowLevelData levelData)
    {
        workingLevel = Clone(levelData);
        EnforceFixedBoardSize();
        view.SetSelectedColor(SelectedColor);
        view.SetSummary(workingLevel);
    }

    public void ApplyPaint(int x, int y)
    {
        if (workingLevel == null)
        {
            return;
        }

        var cellDictionary = BuildCellDictionary();
        var key = x * 1000 + y;

        if (cellDictionary.ContainsKey(key))
        {
            cellDictionary[key].color = SelectedColor;
        }
        else
        {
            cellDictionary.Add(key, new PixelCellData(x, y, SelectedColor));
        }

        workingLevel.cells = BuildCellsArray(cellDictionary);
        EnforceFixedBoardSize();
        view.SetSummary(workingLevel);
    }

    public PixelFlowLevelData GetWorkingLevel()
    {
        return Clone(workingLevel);
    }

    public void Dispose()
    {
        view.ColorSelected -= OnColorSelected;
        view.ResizeRequested -= OnResizeRequested;
        view.SlotCountChanged -= OnSlotCountChanged;
        view.PigAdded -= OnPigAdded;
        view.RemoveLastPigRequested -= OnRemoveLastPigRequested;
        view.ApplyRequested -= OnApplyRequested;
        view.SaveRequested -= OnSaveRequested;
        view.LoadRequested -= OnLoadRequested;
        view.ResetRequested -= OnResetRequested;
    }

    private void OnColorSelected(PixelPigColor color)
    {
        SelectedColor = color;
        view.SetSelectedColor(color);
    }

    private void OnResizeRequested(int deltaWidth, int deltaHeight)
    {
        EnforceFixedBoardSize();
        view.SetSummary(workingLevel);
    }

    private void OnSlotCountChanged(int delta)
    {
        if (workingLevel == null)
        {
            return;
        }

        workingLevel.waitingSlotCount = System.Math.Max(1, workingLevel.waitingSlotCount + delta);
        view.SetSummary(workingLevel);
    }

    private void OnPigAdded(PixelPigColor color)
    {
        if (workingLevel == null)
        {
            return;
        }

        var pigs = new List<PigSpawnData>(workingLevel.pigQueue ?? new PigSpawnData[0])
        {
            new PigSpawnData(color, 4)
        };
        workingLevel.pigQueue = pigs.ToArray();
        EnsurePigLines();
        var linePigs = new List<PigSpawnData>(workingLevel.pigLines[0].pigs ?? new PigSpawnData[0])
        {
            new PigSpawnData(color, 4)
        };
        workingLevel.pigLines[0].pigs = linePigs.ToArray();
        view.SetSummary(workingLevel);
    }

    private void OnRemoveLastPigRequested()
    {
        if (workingLevel == null || workingLevel.pigQueue == null || workingLevel.pigQueue.Length == 0)
        {
            return;
        }

        var pigs = new List<PigSpawnData>(workingLevel.pigQueue);
        pigs.RemoveAt(pigs.Count - 1);
        workingLevel.pigQueue = pigs.ToArray();

        EnsurePigLines();

        for (var lineIndex = workingLevel.pigLines.Length - 1; lineIndex >= 0; lineIndex--)
        {
            var linePigs = new List<PigSpawnData>(workingLevel.pigLines[lineIndex].pigs ?? new PigSpawnData[0]);

            if (linePigs.Count == 0)
            {
                continue;
            }

            linePigs.RemoveAt(linePigs.Count - 1);
            workingLevel.pigLines[lineIndex].pigs = linePigs.ToArray();
            break;
        }

        view.SetSummary(workingLevel);
    }

    private void OnApplyRequested()
    {
        applyLevel?.Invoke(Clone(workingLevel));
    }

    private void OnSaveRequested()
    {
        saveLoad.Save(workingLevel);
        applyLevel?.Invoke(Clone(workingLevel));
    }

    private void OnLoadRequested()
    {
        var savedLevel = saveLoad.Load();

        if (savedLevel == null)
        {
            return;
        }

        workingLevel = Clone(savedLevel);
        view.SetSummary(workingLevel);
        applyLevel?.Invoke(Clone(workingLevel));
    }

    private void OnResetRequested()
    {
        var defaultLevel = getDefaultLevel?.Invoke();

        if (defaultLevel == null)
        {
            return;
        }

        workingLevel = Clone(defaultLevel);
        view.SetSummary(workingLevel);
        applyLevel?.Invoke(Clone(workingLevel));
    }

    private Dictionary<int, PixelCellData> BuildCellDictionary()
    {
        var cells = new Dictionary<int, PixelCellData>();

        if (workingLevel?.cells == null)
        {
            return cells;
        }

        for (var i = 0; i < workingLevel.cells.Length; i++)
        {
            var cell = workingLevel.cells[i];
            cells[cell.x * 1000 + cell.y] = new PixelCellData(cell.x, cell.y, cell.color);
        }

        return cells;
    }

    private static PixelCellData[] BuildCellsArray(Dictionary<int, PixelCellData> cellDictionary)
    {
        var values = new List<PixelCellData>();

        foreach (var pair in cellDictionary)
        {
            if (pair.Value.color != PixelPigColor.None)
            {
                values.Add(pair.Value);
            }
        }

        return values.ToArray();
    }

    private static PixelFlowLevelData Clone(PixelFlowLevelData source)
    {
        if (source == null)
        {
            return null;
        }

        return new PixelFlowLevelData
        {
            width = FixedBoardSize,
            height = FixedBoardSize,
            waitingSlotCount = source.waitingSlotCount,
            cells = CloneCells(source.cells),
            pigLines = ClonePigLines(source.pigLines),
            pigQueue = ClonePigs(source.pigQueue)
        };
    }

    private void EnforceFixedBoardSize()
    {
        if (workingLevel == null)
        {
            return;
        }

        workingLevel.width = FixedBoardSize;
        workingLevel.height = FixedBoardSize;

        var filteredCells = new List<PixelCellData>();

        if (workingLevel.cells != null)
        {
            for (var i = 0; i < workingLevel.cells.Length; i++)
            {
                var cell = workingLevel.cells[i];

                if (cell.x >= 0 && cell.x < FixedBoardSize && cell.y >= 0 && cell.y < FixedBoardSize)
                {
                    filteredCells.Add(cell);
                }
            }
        }

        workingLevel.cells = filteredCells.ToArray();
    }

    private static PixelCellData[] CloneCells(PixelCellData[] source)
    {
        if (source == null)
        {
            return new PixelCellData[0];
        }

        var result = new PixelCellData[source.Length];

        for (var i = 0; i < source.Length; i++)
        {
            result[i] = new PixelCellData(source[i].x, source[i].y, source[i].color);
        }

        return result;
    }

    private static PigSpawnData[] ClonePigs(PigSpawnData[] source)
    {
        if (source == null)
        {
            return new PigSpawnData[0];
        }

        var result = new PigSpawnData[source.Length];

        for (var i = 0; i < source.Length; i++)
        {
            result[i] = new PigSpawnData(source[i].color, source[i].ammo);
        }

        return result;
    }

    private static PigLineData[] ClonePigLines(PigLineData[] source)
    {
        if (source == null)
        {
            return new PigLineData[0];
        }

        var result = new PigLineData[source.Length];

        for (var i = 0; i < source.Length; i++)
        {
            result[i] = new PigLineData
            {
                pigs = ClonePigs(source[i] != null ? source[i].pigs : null)
            };
        }

        return result;
    }

    private void EnsurePigLines()
    {
        if (workingLevel == null)
        {
            return;
        }

        if (workingLevel.pigLines != null && workingLevel.pigLines.Length >= 2)
        {
            return;
        }

        workingLevel.pigLines = new[]
        {
            new PigLineData(),
            new PigLineData()
        };
    }
}
