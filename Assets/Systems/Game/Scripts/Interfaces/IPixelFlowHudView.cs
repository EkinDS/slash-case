using System;
using UnityEngine;

public interface IPixelFlowHudView
{
    event Action RestartRequested;
    event Action EditorToggleRequested;

    void SetStatus(string text, Color color);
    void SetStartSolvableState(bool solvable);
    void SetUnlosableState(bool unlosable);
    void SetEditorButtonLabel(string label);
    void SetLevelLabel(string text);
}
