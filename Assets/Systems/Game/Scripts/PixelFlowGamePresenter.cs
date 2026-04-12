using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PixelFlowGamePresenter : IDisposable
{
    private const float BasePigSpeed = 9.6F;
    private const float ConveyorPadding = 1F;
    private const float LaunchSpacing = 1.1F;
    private const float GuaranteeSpeedMultiplier = 1.8F;
    private const float GuaranteeRampSeconds = 1.1F;

    private readonly IPixelGridView gridView;
    private readonly IWaitingSlotsView waitingSlotsView;
    private readonly IPixelFlowHudView hudView;
    private readonly Transform pigLayer;

    private PixelGridModel gridModel;
    private ConveyorLoopModel conveyorLoopModel;
    private readonly List<PigModel> waitingPigs = new List<PigModel>();
    private readonly List<PigModel> activePigs = new List<PigModel>();
    private readonly List<IPigView> activePigViews = new List<IPigView>();
    private readonly List<PigModel> slotAssignments = new List<PigModel>();

    private bool guaranteedMode;
    private bool levelFailed;
    private bool levelCompleted;
    private bool editorOpen;
    private bool hasLaunchedPig;
    private float currentSpeedMultiplier = 1F;
    private float guaranteeRampProgress;
    private int nextPigId;

    public PixelFlowGamePresenter(IPixelGridView gridView, IWaitingSlotsView waitingSlotsView, IPixelFlowHudView hudView,
        Transform pigLayer)
    {
        this.gridView = gridView;
        this.waitingSlotsView = waitingSlotsView;
        this.hudView = hudView;
        this.pigLayer = pigLayer;

        waitingSlotsView.SlotClicked += OnSlotClicked;
    }

    public event Action RestartRequested;
    public event Action EditorToggleRequested;
    public event Action<int, int> GridCellClicked;

    public void Initialize()
    {
        hudView.RestartRequested += OnRestartRequested;
        hudView.EditorToggleRequested += OnEditorToggleRequested;
        gridView.CellClicked += OnCellClicked;
    }

    public void LoadLevel(PixelFlowLevelData levelData)
    {
        ClearPigs();

        guaranteedMode = false;
        levelFailed = false;
        levelCompleted = false;
        currentSpeedMultiplier = 1F;
        guaranteeRampProgress = 0F;
        hasLaunchedPig = false;

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

        waitingPigs.Clear();
        nextPigId = 1;

        if (levelData?.pigQueue != null)
        {
            for (var i = 0; i < levelData.pigQueue.Length; i++)
            {
                var pig = new PigModel(nextPigId++, levelData.pigQueue[i].color, Mathf.Max(1, levelData.pigQueue[i].ammo));
                waitingPigs.Add(pig);
            }
        }

        RefillSlotsFromWaitingQueue();
        RenderSlots();
        hudView.SetStatus("Launch pigs from the slots", Color.white);
        hudView.SetGuaranteeVisible(false);
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
            hudView.SetStatus("Launch pigs from the slots", Color.white);
        }
    }

    public void Dispose()
    {
        waitingSlotsView.SlotClicked -= OnSlotClicked;
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
            hudView.SetGuaranteeVisible(true);
            hudView.SetStatus("Guaranteed finish: all pigs accelerating", new Color32(255, 224, 107, 255));
            LaunchAllWaitingPigs();
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
            pigModel.Distance = conveyorLoopModel.WrapDistance(pigModel.Distance + BasePigSpeed * currentSpeedMultiplier * deltaTime);
            pigModel.TotalDistanceTravelled += BasePigSpeed * currentSpeedMultiplier * deltaTime;
            var previousPosition = conveyorLoopModel.EvaluatePosition(previousDistance);
            var currentPosition = conveyorLoopModel.EvaluatePosition(pigModel.Distance);

            var previousWrapped = conveyorLoopModel.WrapDistance(previousDistance);
            var didWrap = pigModel.Distance < previousWrapped;

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

            if (guaranteedMode && gridModel.RemainingPixelCount > 0)
            {
                continue;
            }

            var parked = TryParkPig(pigModel);

            if (!parked)
            {
                levelFailed = true;
                waitingSlotsView.PlayOverflowWarning();
                hudView.SetStatus("Level failed: no free waiting slot", new Color32(255, 122, 122, 255));
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

            if (pig.AmmoRemaining <= 0)
            {
                continue;
            }

            var side = conveyorLoopModel.EvaluateSide(pig.Distance);
            var lineIndex = conveyorLoopModel.EvaluateLineIndex(pig.Distance, gridModel.Width, gridModel.Height);
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

            pig.ConsumeAmmo();
            activePigViews[pigIndex].SetAmmo(pig.AmmoRemaining);
            activePigViews[pigIndex].PlayHitFeedback();
            gridView.PlayShot(
                activePigViews[pigIndex].Position,
                gridView.GetCellCenter(hitResult.X, hitResult.Y),
                pig.Color,
                () =>
                {
                    gridView.RenderCell(hitResult.X, hitResult.Y, PixelPigColor.None, false);
                    gridView.PlayCellHit(hitResult.X, hitResult.Y);
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

        if (gridModel.RemainingPixelCount > 0)
        {
            return;
        }

        levelCompleted = true;
        hudView.SetStatus("Level cleared", new Color32(114, 255, 167, 255));
    }

    private bool IsGuaranteedFinish()
    {
        if (gridModel.RemainingPixelCount <= 0 || !hasLaunchedPig || waitingPigs.Count > 0)
        {
            return false;
        }

        var ammoByColor = new Dictionary<PixelPigColor, int>();

        foreach (PixelPigColor color in Enum.GetValues(typeof(PixelPigColor)))
        {
            if (color != PixelPigColor.None)
            {
                ammoByColor[color] = 0;
            }
        }

        for (var i = 0; i < waitingPigs.Count; i++)
        {
            ammoByColor[waitingPigs[i].Color] += waitingPigs[i].AmmoRemaining;
        }

        for (var i = 0; i < slotAssignments.Count; i++)
        {
            if (slotAssignments[i] != null)
            {
                ammoByColor[slotAssignments[i].Color] += slotAssignments[i].AmmoRemaining;
            }
        }

        for (var i = 0; i < activePigs.Count; i++)
        {
            ammoByColor[activePigs[i].Color] += activePigs[i].AmmoRemaining;
        }

        foreach (var pair in ammoByColor)
        {
            if (pair.Value < gridModel.GetRemainingCount(pair.Key))
            {
                return false;
            }
        }

        return true;
    }

    private void LaunchAllWaitingPigs()
    {
        for (var i = 0; i < slotAssignments.Count; i++)
        {
            if (slotAssignments[i] != null)
            {
                LaunchPigFromSlot(i);
            }
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
        if (editorOpen || levelFailed || levelCompleted || guaranteedMode)
        {
            return;
        }

        LaunchPigFromSlot(slotIndex);
    }

    private void LaunchPigFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotAssignments.Count)
        {
            return;
        }

        var pig = slotAssignments[slotIndex];

        if (pig == null)
        {
            return;
        }

        slotAssignments[slotIndex] = null;
        pig.IsActive = true;
        hasLaunchedPig = true;
        pig.ResetShotTracking();
        pig.Distance = conveyorLoopModel.WrapDistance(activePigs.Count * -LaunchSpacing);
        pig.TotalDistanceTravelled = activePigs.Count * LaunchSpacing;
        activePigs.Add(pig);

        var pigViewObject = new GameObject($"PigView_{pig.Id}");
        var pigView = pigViewObject.AddComponent<PigView>();
        pigView.Initialize(pigLayer, pig.Color);
        pigView.SetPosition(conveyorLoopModel.EvaluatePosition(pig.Distance));
        pigView.SetAmmo(pig.AmmoRemaining);
        pigView.PlayLaunch();
        activePigViews.Add(pigView);

        RefillSlotsFromWaitingQueue();
        RenderSlots();
        hudView.SetStatus("Pigs are flowing", new Color32(220, 232, 255, 255));
    }

    private void RefillSlotsFromWaitingQueue()
    {
        for (var i = 0; i < slotAssignments.Count; i++)
        {
            if (slotAssignments[i] != null || waitingPigs.Count == 0)
            {
                continue;
            }

            slotAssignments[i] = waitingPigs[0];
            waitingPigs.RemoveAt(0);
        }
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
            RenderSlots();
            return true;
        }

        return false;
    }

    private void RenderSlots()
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

        waitingSlotsView.Render(states);
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
        RenderSlots();
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
}
