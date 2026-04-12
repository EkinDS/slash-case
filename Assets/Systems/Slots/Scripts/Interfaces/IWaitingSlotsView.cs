using System;
using System.Collections.Generic;

public interface IWaitingSlotsView
{
    event Action<int> SlotClicked;
    event Action<int> PigLineClicked;

    void BuildSlots(int slotCount);
    void Render(IReadOnlyList<WaitingSlotState> slotStates, IReadOnlyList<IReadOnlyList<QueuedPigState>> lineStates);
    void PlayOverflowWarning();
}
