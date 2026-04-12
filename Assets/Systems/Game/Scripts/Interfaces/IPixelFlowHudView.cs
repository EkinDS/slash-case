using System;
using UnityEngine;

public interface IPixelFlowHudView
{
    event Action RestartRequested;
    event Action EditorToggleRequested;

    void SetStatus(string text, Color color);
    void SetGuaranteeVisible(bool visible);
    void SetEditorButtonLabel(string label);
}
