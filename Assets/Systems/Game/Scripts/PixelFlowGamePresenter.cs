using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PixelFlowGamePresenter : IDisposable
{
    private const string PigPrefabResourcePath = "Pig/Prefabs/Pig";
    private const int ConveyorCapacity = 5;
    private const float BasePigSpeed = 15F;
    private const float ConveyorPadding = 3.175F;
    private const float LaunchSpacing = 1.1F;
    private const float GuaranteeSpeedMultiplier = 2F;
    private const float GuaranteeRampSeconds = 1.1F;
    private const float ShotAlignmentToleranceRatio = 0.35F;

    private readonly IPixelGridView gridView;
    private readonly IWaitingSlotsView waitingSlotsView;
    private readonly IPixelFlowHudView hudView;
    private readonly Transform pigLayer;

    private PixelGridModel gridModel;
    private ConveyorLoopModel conveyorLoopModel;
    private readonly List<List<PigModel>> pigLines = new List<List<PigModel>>();
    private readonly List<PigModel> activePigs = new List<PigModel>();
    private readonly List<IPigView> activePigViews = new List<IPigView>();
    private readonly List<PigModel> slotAssignments = new List<PigModel>();

    private PixelFlowLevelData currentLevelData;
    private bool guaranteedMode;
    private bool levelFailed;
    private bool levelCompleted;
    private bool editorOpen;
    private bool hasLaunchedPig;
    private bool startSolvable;
    private float currentSpeedMultiplier = 1F;
    private float guaranteeRampProgress;
    private int nextPigId;
    private int pendingShots;
    private GameObject pigPrefab;

    public PixelFlowGamePresenter(IPixelGridView gridView, IWaitingSlotsView waitingSlotsView, IPixelFlowHudView hudView,
        Transform pigLayer)
    {
        this.gridView = gridView;
        this.waitingSlotsView = waitingSlotsView;
        this.hudView = hudView;
        this.pigLayer = pigLayer;

        waitingSlotsView.SlotClicked += OnSlotClicked;
        waitingSlotsView.PigLineClicked += OnPigLineClicked;
    }

    public event Action RestartRequested;
    public event Action EditorToggleRequested;
    public event Action<int, int> GridCellClicked;
    public event Action LevelCompleted;
    public event Action LevelFailed;

    public void Initialize()
    {
        hudView.RestartRequested += OnRestartRequested;
        hudView.EditorToggleRequested += OnEditorToggleRequested;
        gridView.CellClicked += OnCellClicked;
    }

    public void LoadLevel(PixelFlowLevelData levelData)
    {
        ClearPigs();
        currentLevelData = levelData;

        guaranteedMode = false;
        levelFailed = false;
        levelCompleted = false;
        currentSpeedMultiplier = 1F;
        guaranteeRampProgress = 0F;
        hasLaunchedPig = false;
        startSolvable = PixelFlowLevelAnalyzer.IsStartSolvable(levelData);
        pendingShots = 0;

        gridModel = new PixelGridModel(levelData);
        gridView.BuildGrid(gridModel.Width, gridModel.Height);

        for (var y = 0; y < gridModel.Height; y++)
        {
            for (var x = 0; x < gridModel.Width; x++)
            {
                var color = gridModel.GetColor(x, y);
                gridView.RenderCell(x, y, color, color != PixelPigColor.None);
            }
        }

        conveyorLoopModel = new ConveyorLoopModel(gridView.BoardRect, ConveyorPadding);

        slotAssignments.Clear();
        var slotCount = Mathf.Max(1, levelData != null ? levelData.waitingSlotCount : 1);
        waitingSlotsView.BuildSlots(slotCount);

        for (var i = 0; i < slotCount; i++)
        {
            slotAssignments.Add(null);
        }

        pigLines.Clear();
        var lineCount = levelData != null && levelData.pigLines != null && levelData.pigLines.Length > 0
            ? levelData.pigLines.Length
            : 1;

        for (var i = 0; i < lineCount; i++)
        {
            pigLines.Add(new List<PigModel>());
        }

        nextPigId = 1;
        GeneratePigLinesFromPuzzle();
        RenderWaitingArea();
        UpdateConveyorCapacityDisplay();
        hudView.SetStatus(string.Empty, Color.white);
        hudView.SetStatus("Launch pigs from the front of each line", Color.white);
        hudView.SetStartSolvableState(startSolvable);
        hudView.SetUnlosableState(false);
        hudView.SetEditorButtonLabel(editorOpen ? "Close" : "Editor");
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0F || editorOpen || levelFailed || levelCompleted)
        {
            return;
        }

        UpdateGuaranteedMode(deltaTime);
        MoveActivePigs(deltaTime);
        ResolvePigShots();
        CheckCompletion();
    }

    public void SetEditorOpen(bool isOpen)
    {
        editorOpen = isOpen;
        hudView.SetEditorButtonLabel(isOpen ? "Close" : "Editor");

        if (isOpen)
        {
            hudView.SetStatus("Editor open: click grid cells to paint", new Color32(199, 224, 255, 255));
        }
        else if (!levelCompleted && !levelFailed)
        {
            hudView.SetStatus("Launch pigs from the front of each line", Color.white);
        }
    }

    public void Dispose()
    {
        waitingSlotsView.SlotClicked -= OnSlotClicked;
        waitingSlotsView.PigLineClicked -= OnPigLineClicked;
        hudView.RestartRequested -= OnRestartRequested;
        hudView.EditorToggleRequested -= OnEditorToggleRequested;
        gridView.CellClicked -= OnCellClicked;
        ClearPigs();
    }

    private void UpdateGuaranteedMode(float deltaTime)
    {
        if (!guaranteedMode && IsGuaranteedFinish())
        {
            guaranteedMode = true;
            hudView.SetUnlosableState(true);
            hudView.SetStatus("Unlosable state: remaining pigs auto-resolve", new Color32(255, 224, 107, 255));
        }

        if (!guaranteedMode)
        {
            return;
        }

        guaranteeRampProgress = Mathf.Clamp01(guaranteeRampProgress + deltaTime / GuaranteeRampSeconds);
        currentSpeedMultiplier = Mathf.Lerp(1F, GuaranteeSpeedMultiplier, guaranteeRampProgress);
    }

    private void MoveActivePigs(float deltaTime)
    {
        for (var i = activePigs.Count - 1; i >= 0; i--)
        {
            var pigModel = activePigs[i];
            var previousDistance = pigModel.Distance;
            pigModel.Distance += BasePigSpeed * currentSpeedMultiplier * deltaTime;
            pigModel.TotalDistanceTravelled += BasePigSpeed * currentSpeedMultiplier * deltaTime;
            var previousPosition = conveyorLoopModel.EvaluatePosition(previousDistance);
            var currentPosition = conveyorLoopModel.EvaluatePosition(pigModel.Distance);

            var didWrap = previousDistance >= 0F &&
                Mathf.FloorToInt(previousDistance / conveyorLoopModel.LoopLength) <
                Mathf.FloorToInt(pigModel.Distance / conveyorLoopModel.LoopLength);

            activePigViews[i].SetMovementDirection(currentPosition - previousPosition);
            activePigViews[i].SetPosition(currentPosition);

            if (!didWrap)
            {
                continue;
            }

            if (pigModel.AmmoRemaining <= 0)
            {
                RemoveActivePigAt(i, true);
                continue;
            }

            if (guaranteedMode)
            {
                pigModel.ResetShotTracking();
                continue;
            }

            var parked = TryParkPig(pigModel);

            if (!parked)
            {
                levelFailed = true;
                waitingSlotsView.PlayOverflowWarning();
                hudView.SetStatus("Level Failed", new Color32(255, 122, 122, 255));
                LevelFailed?.Invoke();
            }

            RemoveActivePigAt(i, false);
        }
    }

    private void ResolvePigShots()
    {
        if (activePigs.Count == 0)
        {
            return;
        }

        var orderedIndexes = new List<int>(activePigs.Count);

        for (var i = 0; i < activePigs.Count; i++)
        {
            orderedIndexes.Add(i);
        }

        orderedIndexes.Sort((left, right) => activePigs[right].TotalDistanceTravelled.CompareTo(activePigs[left].TotalDistanceTravelled));

        for (var orderIndex = 0; orderIndex < orderedIndexes.Count; orderIndex++)
        {
            if (levelFailed || levelCompleted)
            {
                return;
            }

            var pigIndex = orderedIndexes[orderIndex];

            if (pigIndex < 0 || pigIndex >= activePigs.Count)
            {
                continue;
            }

            var pig = activePigs[pigIndex];

            if (pig.AmmoRemaining <= 0 || pig.Distance < 0F)
            {
                continue;
            }

            var side = conveyorLoopModel.EvaluateSide(pig.Distance);

            if (!TryGetAlignedLineIndex(activePigViews[pigIndex].Position, side, out var lineIndex))
            {
                continue;
            }

            var sideIndex = (int)side;

            if (pig.LastShotSide == sideIndex && pig.LastShotLineIndex == lineIndex)
            {
                continue;
            }

            pig.LastShotSide = sideIndex;
            pig.LastShotLineIndex = lineIndex;

            if (!gridModel.TryHitFirstVisibleMatchingPixel(side, lineIndex, pig.Color, out var hitResult))
            {
                continue;
            }

            activePigViews[pigIndex].SetAimDirection(
                gridView.GetCellCenter(hitResult.X, hitResult.Y) - activePigViews[pigIndex].Position);
            pig.ConsumeAmmo();
            activePigViews[pigIndex].SetAmmo(pig.AmmoRemaining);
            activePigViews[pigIndex].PlayHitFeedback();
            pendingShots++;
            gridView.PlayShot(
                activePigViews[pigIndex].Position,
                gridView.GetCellCenter(hitResult.X, hitResult.Y),
                pig.Color,
                () =>
                {
                    gridView.PlayCellHit(
                        hitResult.X,
                        hitResult.Y,
                        () =>
                        {
                            gridView.RenderCell(hitResult.X, hitResult.Y, PixelPigColor.None, false);
                            pendingShots = Mathf.Max(0, pendingShots - 1);
                            CheckCompletion();
                        });
                });

            if (pig.AmmoRemaining > 0)
            {
                continue;
            }

            RemoveActivePigAt(pigIndex, true);

            for (var i = 0; i < orderedIndexes.Count; i++)
            {
                if (orderedIndexes[i] > pigIndex)
                {
                    orderedIndexes[i]--;
                }
            }
        }
    }

    private void CheckCompletion()
    {
        if (levelFailed || levelCompleted)
        {
            return;
        }

        if (gridModel.RemainingPixelCount > 0 || activePigs.Count > 0 || pendingShots > 0)
        {
            return;
        }

        levelCompleted = true;
        hudView.SetStatus("Level Completed", new Color32(114, 255, 167, 255));
        LevelCompleted?.Invoke();
    }

    private bool IsGuaranteedFinish()
    {
        if (gridModel.RemainingPixelCount <= 0 || !hasLaunchedPig || pendingShots > 0)
        {
            return false;
        }

        return PixelFlowLevelAnalyzer.IsCurrentStateUnlosable(
            BuildCurrentCells(),
            BuildCurrentPigLines(),
            BuildCurrentSlots(),
            BuildCurrentActivePigs(),
            slotAssignments.Count);
    }

    private void OnSlotClicked(int slotIndex)
    {
        if (editorOpen || levelFailed || levelCompleted)
        {
            return;
        }

        LaunchPigFromSlot(slotIndex);
    }

    private void OnPigLineClicked(int lineIndex)
    {
        if (editorOpen || levelFailed || levelCompleted)
        {
            return;
        }

        LaunchPigFromLine(lineIndex);
    }

    private void LaunchPigFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotAssignments.Count)
        {
            return;
        }

        if (!CanLaunchMorePigs())
        {
            hudView.SetStatus("Conveyor full", new Color32(255, 214, 122, 255));
            return;
        }

        var pig = slotAssignments[slotIndex];

        if (pig == null)
        {
            return;
        }

        slotAssignments[slotIndex] = null;
        LaunchPig(pig);
        RenderWaitingArea();
        UpdateConveyorCapacityDisplay();
        hudView.SetStatus("Pigs are flowing", new Color32(220, 232, 255, 255));
    }

    private void LaunchPigFromLine(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= pigLines.Count || pigLines[lineIndex].Count == 0)
        {
            return;
        }

        if (!CanLaunchMorePigs())
        {
            hudView.SetStatus("Conveyor full", new Color32(255, 214, 122, 255));
            return;
        }

        var pig = pigLines[lineIndex][0];
        pigLines[lineIndex].RemoveAt(0);
        LaunchPig(pig);
        RenderWaitingArea();
        UpdateConveyorCapacityDisplay();
        hudView.SetStatus("Pigs are flowing", new Color32(220, 232, 255, 255));
    }

    private bool TryParkPig(PigModel pig)
    {
        for (var i = 0; i < slotAssignments.Count; i++)
        {
            if (slotAssignments[i] != null)
            {
                continue;
            }

            slotAssignments[i] = pig;
            pig.IsActive = false;
            RenderWaitingArea();
            UpdateConveyorCapacityDisplay();
            return true;
        }

        return false;
    }

    private void RenderWaitingArea()
    {
        var states = new List<WaitingSlotState>(slotAssignments.Count);

        for (var i = 0; i < slotAssignments.Count; i++)
        {
            if (slotAssignments[i] == null)
            {
                states.Add(new WaitingSlotState(false, PixelPigColor.None, 0));
                continue;
            }

            states.Add(new WaitingSlotState(true, slotAssignments[i].Color, slotAssignments[i].AmmoRemaining));
        }

        var lineStates = new List<IReadOnlyList<QueuedPigState>>(pigLines.Count);

        for (var lineIndex = 0; lineIndex < pigLines.Count; lineIndex++)
        {
            var queuedStates = new List<QueuedPigState>(pigLines[lineIndex].Count);

            for (var pigIndex = 0; pigIndex < pigLines[lineIndex].Count; pigIndex++)
            {
                var pig = pigLines[lineIndex][pigIndex];
                queuedStates.Add(new QueuedPigState(pig.Color, pig.AmmoRemaining));
            }

            lineStates.Add(queuedStates);
        }

        waitingSlotsView.Render(states, lineStates);
    }

    private void ClearPigs()
    {
        for (var i = 0; i < activePigViews.Count; i++)
        {
            activePigViews[i]?.DestroySelf();
        }

        activePigViews.Clear();
        activePigs.Clear();
    }

    private void RemoveActivePigAt(int index, bool exhausted)
    {
        if (index < 0 || index >= activePigs.Count)
        {
            return;
        }

        var pigView = activePigViews[index];

        if (exhausted)
        {
            pigView.PlayExhaustedAndDestroy();
        }
        else
        {
            pigView.DestroySelf();
        }

        activePigs.RemoveAt(index);
        activePigViews.RemoveAt(index);
        RenderWaitingArea();
        UpdateConveyorCapacityDisplay();
    }

    private void OnRestartRequested()
    {
        RestartRequested?.Invoke();
    }

    private void OnEditorToggleRequested()
    {
        EditorToggleRequested?.Invoke();
    }

    private void OnCellClicked(int x, int y)
    {
        if (editorOpen)
        {
            GridCellClicked?.Invoke(x, y);
        }
    }

    private void LaunchPig(PigModel pig)
    {
        pig.IsActive = true;
        hasLaunchedPig = true;
        pig.ResetShotTracking();
        pig.Distance = activePigs.Count * -LaunchSpacing;
        pig.TotalDistanceTravelled = activePigs.Count * LaunchSpacing;
        activePigs.Add(pig);

        var pigViewObject = CreatePigViewObject(pig.Id);
        var pigView = pigViewObject != null ? pigViewObject.GetComponent<PigView>() : null;

        if (pigView == null)
        {
            pigViewObject = new GameObject($"PigView_{pig.Id}");
            pigView = pigViewObject.AddComponent<PigView>();
        }

        pigView.Initialize(pigLayer, pig.Color);
        pigView.SetPosition(conveyorLoopModel.EvaluatePosition(pig.Distance));
        pigView.SetAmmo(pig.AmmoRemaining);
        pigView.PlayLaunch();
        activePigViews.Add(pigView);
    }

    private GameObject CreatePigViewObject(int pigId)
    {
        if (pigPrefab == null)
        {
            pigPrefab = Resources.Load<GameObject>(PigPrefabResourcePath);
        }

        if (pigPrefab == null)
        {
            Debug.LogError($"Pig prefab not found at Resources path '{PigPrefabResourcePath}'.");
            return null;
        }

        var pigViewObject = UnityEngine.Object.Instantiate(pigPrefab);
        pigViewObject.name = $"PigView_{pigId}";
        return pigViewObject;
    }

    private bool TryGetAlignedLineIndex(Vector2 pigPosition, ConveyorSide side, out int lineIndex)
    {
        lineIndex = 0;

        if (gridModel == null)
        {
            return false;
        }

        var boardRect = gridView.BoardRect;

        switch (side)
        {
            case ConveyorSide.Top:
            case ConveyorSide.Bottom:
                return TryGetAlignedColumnIndex(
                    pigPosition.x,
                    boardRect.xMin,
                    boardRect.width,
                    gridModel.Width,
                    out lineIndex);
            case ConveyorSide.Left:
            case ConveyorSide.Right:
                if (!TryGetAlignedRowIndex(
                        pigPosition.y,
                        boardRect.yMin,
                        boardRect.height,
                        gridModel.Height,
                        out var rowIndex))
                {
                    return false;
                }

                lineIndex = rowIndex;
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetAlignedColumnIndex(float coordinate, float min, float size, int count, out int index)
    {
        index = 0;

        if (count <= 0 || size <= 0F)
        {
            return false;
        }

        var step = size / count;
        var firstCenter = min + step * 0.5F;
        var rawIndex = Mathf.RoundToInt((coordinate - firstCenter) / step);
        rawIndex = Mathf.Clamp(rawIndex, 0, count - 1);
        var center = firstCenter + rawIndex * step;
        var tolerance = step * ShotAlignmentToleranceRatio;

        if (Mathf.Abs(coordinate - center) > tolerance)
        {
            return false;
        }

        index = rawIndex;
        return true;
    }

    private static bool TryGetAlignedRowIndex(float coordinate, float min, float size, int count, out int index)
    {
        index = 0;

        if (count <= 0 || size <= 0F)
        {
            return false;
        }

        var step = size / count;
        var topCenter = min + size - step * 0.5F;
        var rawIndex = Mathf.RoundToInt((topCenter - coordinate) / step);
        rawIndex = Mathf.Clamp(rawIndex, 0, count - 1);
        var center = topCenter - rawIndex * step;
        var tolerance = step * ShotAlignmentToleranceRatio;

        if (Mathf.Abs(coordinate - center) > tolerance)
        {
            return false;
        }

        index = rawIndex;
        return true;
    }

    private void GeneratePigLinesFromPuzzle()
    {
        var configuredPigLines = currentLevelData != null ? currentLevelData.pigLines : null;

        if (configuredPigLines != null && configuredPigLines.Length > 0)
        {
            for (var lineIndex = 0; lineIndex < configuredPigLines.Length && lineIndex < pigLines.Count; lineIndex++)
            {
                var lineData = configuredPigLines[lineIndex];

                if (lineData?.pigs == null)
                {
                    continue;
                }

                for (var pigIndex = 0; pigIndex < lineData.pigs.Length; pigIndex++)
                {
                    var sourcePig = lineData.pigs[pigIndex];

                    if (sourcePig == null || sourcePig.color == PixelPigColor.None || sourcePig.ammo <= 0)
                    {
                        continue;
                    }

                    pigLines[lineIndex].Add(new PigModel(nextPigId++, sourcePig.color, sourcePig.ammo));
                }
            }
        }
    }

    private PixelCellData[] BuildCurrentCells()
    {
        var cells = new List<PixelCellData>();

        for (var y = 0; y < gridModel.Height; y++)
        {
            for (var x = 0; x < gridModel.Width; x++)
            {
                var color = gridModel.GetColor(x, y);

                if (color != PixelPigColor.None)
                {
                    cells.Add(new PixelCellData(x, y, color));
                }
            }
        }

        return cells.ToArray();
    }

    private IReadOnlyList<IReadOnlyList<PigSpawnData>> BuildCurrentPigLines()
    {
        var lines = new List<IReadOnlyList<PigSpawnData>>(pigLines.Count);

        for (var lineIndex = 0; lineIndex < pigLines.Count; lineIndex++)
        {
            var pigs = new List<PigSpawnData>(pigLines[lineIndex].Count);

            for (var pigIndex = 0; pigIndex < pigLines[lineIndex].Count; pigIndex++)
            {
                var pig = pigLines[lineIndex][pigIndex];
                pigs.Add(new PigSpawnData(pig.Color, pig.AmmoRemaining));
            }

            lines.Add(pigs);
        }

        return lines;
    }

    private IReadOnlyList<PigSpawnData> BuildCurrentSlots()
    {
        var slots = new List<PigSpawnData>(slotAssignments.Count);

        for (var i = 0; i < slotAssignments.Count; i++)
        {
            var pig = slotAssignments[i];
            slots.Add(pig != null ? new PigSpawnData(pig.Color, pig.AmmoRemaining) : null);
        }

        return slots;
    }

    private IReadOnlyList<PigSpawnData> BuildCurrentActivePigs()
    {
        var pigs = new List<PigSpawnData>(activePigs.Count);

        for (var i = 0; i < activePigs.Count; i++)
        {
            var pig = activePigs[i];
            pigs.Add(new PigSpawnData(pig.Color, pig.AmmoRemaining));
        }

        return pigs;
    }

    private bool CanLaunchMorePigs()
    {
        return activePigs.Count < ConveyorCapacity;
    }

    private void UpdateConveyorCapacityDisplay()
    {
        gridView.SetConveyorCapacity(ConveyorCapacity - activePigs.Count, ConveyorCapacity);
    }
}
