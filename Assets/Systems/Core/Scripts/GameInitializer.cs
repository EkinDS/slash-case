using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    private const string DefaultLevelResourcePath = "Levels/DefaultLevel";

    private PixelFlowLevelData defaultLevelData;
    private PixelFlowLevelData currentLevelData;

    private PixelFlowGamePresenter gamePresenter;
    private LevelEditorPresenter levelEditorPresenter;
    private PixelFlowLevelSaveLoad levelSaveLoad;
    private LevelEditorView levelEditorView;

    private void Awake()
    {
        ConfigureCamera();

        var worldRoot = new GameObject("WorldRoot").transform;
        worldRoot.SetParent(transform, false);

        var gridView = new GameObject("PixelGridView").AddComponent<PixelGridView>();
        gridView.Initialize(worldRoot);

        var slotsView = new GameObject("WaitingSlotsView").AddComponent<WaitingSlotsView>();
        slotsView.Initialize(worldRoot);

        var hudView = new GameObject("HudView").AddComponent<PixelFlowHudView>();
        hudView.Initialize(worldRoot);

        levelEditorView = new GameObject("LevelEditorView").AddComponent<LevelEditorView>();
        levelEditorView.Initialize(worldRoot);
        levelEditorView.SetVisible(false);

        levelSaveLoad = new PixelFlowLevelSaveLoad();
        defaultLevelData = PixelFlowLevelLoader.Load(Resources.Load<TextAsset>(DefaultLevelResourcePath));
        currentLevelData = levelSaveLoad.Load() ?? CloneLevel(defaultLevelData);

        gamePresenter = new PixelFlowGamePresenter(gridView, slotsView, hudView, gridView.PigRoot);
        gamePresenter.Initialize();
        gamePresenter.RestartRequested += RestartLevel;
        gamePresenter.EditorToggleRequested += ToggleEditor;
        gamePresenter.GridCellClicked += OnGridCellClicked;

        levelEditorPresenter = new LevelEditorPresenter(levelEditorView, levelSaveLoad, () => CloneLevel(defaultLevelData), ApplyLevel);
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
            gamePresenter.Dispose();
        }

        levelEditorPresenter?.Dispose();
    }

    private void RestartLevel()
    {
        ApplyLevel(currentLevelData);
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
        currentLevelData = CloneLevel(levelData ?? defaultLevelData);
        gamePresenter.LoadLevel(currentLevelData);
        gamePresenter.SetEditorOpen(levelEditorView != null && levelEditorView.gameObject.activeSelf);

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
}
