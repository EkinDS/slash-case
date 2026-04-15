using System;

public interface IPixelFlowGamePresenter : IDisposable
{
    event Action RestartRequested;
    event Action EditorToggleRequested;
    event Action<int, int> GridCellClicked;
    event Action LevelCompleted;
    event Action LevelFailed;

    void Initialize();
    void LoadLevel(PixelFlowLevelData levelData);
    void Tick(float deltaTime);
    void SetEditorOpen(bool isOpen);
}
