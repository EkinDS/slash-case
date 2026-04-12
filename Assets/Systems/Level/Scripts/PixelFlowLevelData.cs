using System;

[Serializable]
public class PixelFlowLevelData
{
    public int width = 6;
    public int height = 6;
    public int waitingSlotCount = 5;
    public PixelCellData[] cells = new PixelCellData[0];
    public PigSpawnData[] pigQueue = new PigSpawnData[0];
}
