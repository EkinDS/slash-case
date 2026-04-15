using System;
using System.Collections.Generic;
using UnityEngine;

public interface IWaitingSlotsView
{
    event Action<int> SlotClicked;

    void BuildSlots(int slotCount);
    void SetLineCount(int lineCount);
    void RenderSlots(IReadOnlyList<WaitingSlotState> slotStates);
    Vector3 GetSlotWorldPosition(int slotIndex);
    Vector3 GetLinePigWorldPosition(int lineIndex, int pigIndex);
    void PlayOverflowWarning();
}
