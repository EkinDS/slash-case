using System;

public interface ILevelEditorPresenter : IDisposable
{
    void SetLevel(PixelFlowLevelData levelData);
    void ApplyPaint(int x, int y);
    PixelFlowLevelData GetWorkingLevel();
}
