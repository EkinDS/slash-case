using System.Collections.Generic;

public static class GeneratedLevelSet
{
    private const int BoardSize = 20;

    public static List<PixelFlowLevelData> CreateFiveLevels()
    {
        return new List<PixelFlowLevelData>
        {
            CreateRingLevel(1, 5, new[] { PixelPigColor.Red, PixelPigColor.Blue, PixelPigColor.Yellow, PixelPigColor.Black }),
            CreateRingLevel(2, 5, new[] { PixelPigColor.Yellow, PixelPigColor.Blue, PixelPigColor.Red, PixelPigColor.Green }),
            CreateRingLevel(1, 6, new[] { PixelPigColor.Green, PixelPigColor.Black, PixelPigColor.Yellow, PixelPigColor.Blue, PixelPigColor.Red }),
            CreateRingLevel(2, 5, new[] { PixelPigColor.Black, PixelPigColor.Red, PixelPigColor.Blue, PixelPigColor.Green, PixelPigColor.Yellow }),
            CreateRingLevel(1, 5, new[] { PixelPigColor.Blue, PixelPigColor.Yellow, PixelPigColor.Black, PixelPigColor.Red, PixelPigColor.Green })
        };
    }

    private static PixelFlowLevelData CreateRingLevel(int ringThickness, int waitingSlotCount, PixelPigColor[] ringColors)
    {
        var cells = new List<PixelCellData>();

        for (var y = 0; y < BoardSize; y++)
        {
            for (var x = 0; x < BoardSize; x++)
            {
                var ringIndex = GetRingIndex(x, y, ringThickness);
                var color = ringColors[ringIndex % ringColors.Length];
                cells.Add(new PixelCellData(x, y, color));
            }
        }

        return new PixelFlowLevelData
        {
            width = BoardSize,
            height = BoardSize,
            waitingSlotCount = waitingSlotCount,
            cells = cells.ToArray(),
            pigLines = new PigLineData[0],
            pigQueue = new PigSpawnData[0]
        };
    }

    private static int GetRingIndex(int x, int y, int ringThickness)
    {
        var distanceToEdge = Min(Min(x, y), Min(BoardSize - 1 - x, BoardSize - 1 - y));
        return ringThickness <= 0 ? distanceToEdge : distanceToEdge / ringThickness;
    }

    private static int Min(int left, int right)
    {
        return left < right ? left : right;
    }
}
