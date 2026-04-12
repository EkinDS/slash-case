using System;

public interface ILevelEditorView
{
    event Action<PixelPigColor> ColorSelected;
    event Action<int, int> ResizeRequested;
    event Action<int> SlotCountChanged;
    event Action<PixelPigColor> PigAdded;
    event Action RemoveLastPigRequested;
    event Action ApplyRequested;
    event Action SaveRequested;
    event Action LoadRequested;
    event Action ResetRequested;

    void SetVisible(bool visible);
    void SetSelectedColor(PixelPigColor color);
    void SetSummary(PixelFlowLevelData levelData);
}
