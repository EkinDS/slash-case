using System;
using System.Collections.Generic;

public interface IWaitingSlotsView
{
    event Action<int> SlotClicked;

    void BuildSlots(int slotCount);
    void Render(IReadOnlyList<WaitingSlotState> slotStates);
    void PlayOverflowWarning();
}
