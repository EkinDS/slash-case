using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    private const int TotalGeneratedLevels = 5;

    private System.Collections.Generic.List<PixelFlowLevelData> generatedLevels;
    private PixelFlowLevelData currentLevelData;
    private int currentLevelIndex;

    private PixelFlowGamePresenter gamePresenter;
    private LevelEditorPresenter levelEditorPresenter;
    private PixelFlowLevelSaveLoad levelSaveLoad;
    private LevelEditorView levelEditorView;
    private PixelFlowHudView hudView;

    private void Awake()
    {
        ConfigureCamera();

        var worldRoot = new GameObject("WorldRoot").transform;
        worldRoot.SetParent(transform, false);

        var gridView = new GameObject("PixelGridView").AddComponent<PixelGridView>();
        gridView.Initialize(worldRoot);

        var slotsView = new GameObject("WaitingSlotsView").AddComponent<WaitingSlotsView>();
        slotsView.Initialize(worldRoot);

        hudView = new GameObject("HudView").AddComponent<PixelFlowHudView>();
        hudView.Initialize(worldRoot);

        levelEditorView = new GameObject("LevelEditorView").AddComponent<LevelEditorView>();
        levelEditorView.Initialize(worldRoot);
        levelEditorView.SetVisible(false);

        levelSaveLoad = new PixelFlowLevelSaveLoad();
        generatedLevels = GeneratedLevelSet.CreateFiveLevels();
        currentLevelIndex = 0;
        currentLevelData = CloneLevel(generatedLevels[currentLevelIndex]);

        gamePresenter = new PixelFlowGamePresenter(gridView, slotsView, hudView, gridView.PigRoot);
        gamePresenter.Initialize();
        gamePresenter.RestartRequested += RestartLevel;
        gamePresenter.EditorToggleRequested += ToggleEditor;
        gamePresenter.GridCellClicked += OnGridCellClicked;
        gamePresenter.LevelCompleted += AdvanceToNextLevel;

        levelEditorPresenter = new LevelEditorPresenter(levelEditorView, levelSaveLoad, GetCurrentGeneratedLevel, ApplyLevel);
        levelEditorPresenter.SetLevel(currentLevelData);

        ApplyLevel(currentLevelData);
    }

    private void Update()
    {
        gamePresenter?.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (gamePresenter != null)
        {
            gamePresenter.RestartRequested -= RestartLevel;
            gamePresenter.EditorToggleRequested -= ToggleEditor;
            gamePresenter.GridCellClicked -= OnGridCellClicked;
            gamePresenter.LevelCompleted -= AdvanceToNextLevel;
            gamePresenter.Dispose();
        }

        levelEditorPresenter?.Dispose();
    }

    private void RestartLevel()
    {
        ApplyLevel(currentLevelData);
    }

    private void AdvanceToNextLevel()
    {
        currentLevelIndex = (currentLevelIndex + 1) % TotalGeneratedLevels;
        ApplyLevel(GetCurrentGeneratedLevel());
    }

    private void ToggleEditor()
    {
        var newState = !levelEditorView.gameObject.activeSelf;
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
        currentLevelData = CloneLevel(levelData ?? GetCurrentGeneratedLevel());
        gamePresenter.LoadLevel(currentLevelData);
        gamePresenter.SetEditorOpen(levelEditorView != null && levelEditorView.gameObject.activeSelf);
        hudView?.SetLevelLabel($"Level {currentLevelIndex + 1}");

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

    private PixelFlowLevelData GetCurrentGeneratedLevel()
    {
        if (generatedLevels == null || generatedLevels.Count == 0)
        {
            return new PixelFlowLevelData();
        }

        if (currentLevelIndex < 0 || currentLevelIndex >= generatedLevels.Count)
        {
            currentLevelIndex = 0;
        }

        return CloneLevel(generatedLevels[currentLevelIndex]);
    }
}
