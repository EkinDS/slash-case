using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    private const float LevelTransitionDelaySeconds = 0.5F;

    private System.Collections.Generic.List<PixelFlowLevelData> loadedLevels;
    private PixelFlowLevelData currentLevelData;
    private int currentLevelIndex;
    private Coroutine levelTransitionCoroutine;

    private IPixelFlowGamePresenter gamePresenter;
    private ILevelEditorPresenter levelEditorPresenter;
    private ILevelSaveLoad levelSaveLoad;
    private ILevelCatalog levelCatalog;
    private ILevelEditorView levelEditorView;
    private IPixelFlowHudView hudView;

    private void Awake()
    {
        ConfigureCamera();

        var worldRoot = new GameObject("WorldRoot").transform;
        worldRoot.SetParent(transform, false);

        ICellPrefabProvider cellPrefabProvider = new ResourcesCellPrefabProvider();
        IPigPrefabProvider pigPrefabProvider = new ResourcesPigPrefabProvider();
        levelSaveLoad = new PixelFlowLevelSaveLoad();
        levelCatalog = new ResourcesLevelCatalog("Levels");

        var gridViewBehaviour = new GameObject("PixelGridView").AddComponent<PixelGridView>();
        gridViewBehaviour.Initialize(worldRoot, cellPrefabProvider);
        IPixelGridView gridView = gridViewBehaviour;

        var slotsViewBehaviour = new GameObject("WaitingSlotsView").AddComponent<WaitingSlotsView>();
        slotsViewBehaviour.Initialize(worldRoot);
        IWaitingSlotsView slotsView = slotsViewBehaviour;

        var hudViewBehaviour = new GameObject("HudView").AddComponent<PixelFlowHudView>();
        hudViewBehaviour.Initialize(worldRoot);
        hudView = hudViewBehaviour;

        var levelEditorViewBehaviour = new GameObject("LevelEditorView").AddComponent<LevelEditorView>();
        levelEditorViewBehaviour.Initialize(worldRoot);
        levelEditorView = levelEditorViewBehaviour;
        levelEditorView.SetVisible(false);

        loadedLevels = levelCatalog.LoadAll();
        currentLevelIndex = 0;
        currentLevelData = GetCurrentLoadedLevel();

        var pigViewFactory = new PigViewFactory(pigPrefabProvider);
        gamePresenter = new PixelFlowGamePresenter(gridView, slotsView, hudView, gridView.PigRoot, pigViewFactory);
        gamePresenter.Initialize();
        gamePresenter.RestartRequested += RestartLevel;
        gamePresenter.EditorToggleRequested += ToggleEditor;
        gamePresenter.GridCellClicked += OnGridCellClicked;
        gamePresenter.LevelCompleted += AdvanceToNextLevel;
        gamePresenter.LevelFailed += RestartLevelWithDelay;

        levelEditorPresenter = new LevelEditorPresenter(levelEditorView, levelSaveLoad, GetCurrentLoadedLevel, ApplyLevel);
        levelEditorPresenter.SetLevel(currentLevelData);

        ApplyLevel(currentLevelData);
    }

    private void Update()
    {
        gamePresenter?.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (levelTransitionCoroutine != null)
        {
            StopCoroutine(levelTransitionCoroutine);
            levelTransitionCoroutine = null;
        }

        if (gamePresenter != null)
        {
            gamePresenter.RestartRequested -= RestartLevel;
            gamePresenter.EditorToggleRequested -= ToggleEditor;
            gamePresenter.GridCellClicked -= OnGridCellClicked;
            gamePresenter.LevelCompleted -= AdvanceToNextLevel;
            gamePresenter.LevelFailed -= RestartLevelWithDelay;
            gamePresenter.Dispose();
        }

        levelEditorPresenter?.Dispose();
    }

    private void RestartLevel()
    {
        ApplyLevel(currentLevelData);
    }

    private void RestartLevelWithDelay()
    {
        if (levelTransitionCoroutine != null)
        {
            StopCoroutine(levelTransitionCoroutine);
        }

        levelTransitionCoroutine = StartCoroutine(RestartLevelRoutine());
    }

    private void AdvanceToNextLevel()
    {
        if (levelTransitionCoroutine != null)
        {
            StopCoroutine(levelTransitionCoroutine);
        }

        levelTransitionCoroutine = StartCoroutine(AdvanceToNextLevelRoutine());
    }

    private void ToggleEditor()
    {
        var newState = !levelEditorView.IsVisible;
        levelEditorView.SetVisible(newState);
        gamePresenter.SetEditorOpen(newState);

        if (newState)
        {
            levelEditorPresenter.SetLevel(currentLevelData);
        }
    }

    private void OnGridCellClicked(int x, int y)
    {
        levelEditorPresenter.ApplyPaint(x, y);
        currentLevelData = CloneLevel(levelEditorPresenter.GetWorkingLevel());
        ApplyLevel(currentLevelData, false);
    }

    private void ApplyLevel(PixelFlowLevelData levelData)
    {
        ApplyLevel(levelData, true);
    }

    private void ApplyLevel(PixelFlowLevelData levelData, bool updateEditor)
    {
        currentLevelData = CloneLevel(levelData ?? GetCurrentLoadedLevel());
        gamePresenter.LoadLevel(currentLevelData);
        gamePresenter.SetEditorOpen(levelEditorView != null && levelEditorView.IsVisible);
        hudView?.SetLevelLabel($"Level {currentLevelData.id}");

        if (updateEditor)
        {
            levelEditorPresenter.SetLevel(currentLevelData);
        }
    }

    private static void ConfigureCamera()
    {
        var mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return;
        }

        return;
        mainCamera.orthographic = false;
        mainCamera.fieldOfView = 34F;
        mainCamera.transform.position = new Vector3(0F, 18F, -16F);
        mainCamera.transform.rotation = Quaternion.Euler(56F, 0F, 0F);
        mainCamera.backgroundColor = new Color32(25, 31, 45, 255);
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
    }

    private static PixelFlowLevelData CloneLevel(PixelFlowLevelData source)
    {
        if (source == null)
        {
            return new PixelFlowLevelData();
        }

        return JsonUtility.FromJson<PixelFlowLevelData>(JsonUtility.ToJson(source));
    }

    private PixelFlowLevelData GetCurrentLoadedLevel()
    {
        if (loadedLevels == null || loadedLevels.Count == 0)
        {
            return new PixelFlowLevelData
            {
                id = currentLevelIndex + 1
            };
        }

        if (currentLevelIndex < 0 || currentLevelIndex >= loadedLevels.Count)
        {
            currentLevelIndex = 0;
        }

        var baseLevel = CloneLevel(loadedLevels[currentLevelIndex]);
        var savedLevel = levelSaveLoad.Load(baseLevel.id);
        return CloneLevel(savedLevel ?? baseLevel);
    }

    private System.Collections.IEnumerator AdvanceToNextLevelRoutine()
    {
        yield return new WaitForSeconds(LevelTransitionDelaySeconds);

        currentLevelIndex = loadedLevels != null && loadedLevels.Count > 0
            ? (currentLevelIndex + 1) % loadedLevels.Count
            : 0;
        ApplyLevel(GetCurrentLoadedLevel());
        levelTransitionCoroutine = null;
    }

    private System.Collections.IEnumerator RestartLevelRoutine()
    {
        yield return new WaitForSeconds(LevelTransitionDelaySeconds);
        ApplyLevel(currentLevelData);
        levelTransitionCoroutine = null;
    }
}
