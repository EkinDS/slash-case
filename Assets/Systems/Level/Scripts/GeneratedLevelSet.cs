using System.Collections.Generic;

public static class GeneratedLevelSet
{
    private const int BoardSize = 20;

    public static List<PixelFlowLevelData> CreateFiveLevels()
    {
        // Keep startup deterministic and cheap. Exhaustive solver validation here was
        // running against hundreds of full-board candidates during Awake().
        var palettes = new[]
        {
            new[] { PixelPigColor.Red, PixelPigColor.Blue, PixelPigColor.Yellow, PixelPigColor.Black },
            new[] { PixelPigColor.Blue, PixelPigColor.Black, PixelPigColor.Red, PixelPigColor.Yellow },
            new[] { PixelPigColor.Yellow, PixelPigColor.Red, PixelPigColor.Black, PixelPigColor.Blue },
            new[] { PixelPigColor.Black, PixelPigColor.Yellow, PixelPigColor.Blue, PixelPigColor.Red }
        };

        return new List<PixelFlowLevelData>
        {
            CreateMixedLevel(5, 0, 1, 2, palettes[0]),
            CreateSpiralLevel(6, 2, 4, palettes[1]),
            CreateQuadrantLevel(5, 1, 3, palettes[2]),
            CreateMixedLevel(6, 4, 2, 5, palettes[3]),
            CreateSpiralLevel(5, 6, 1, palettes[0])
        };
    }

    private static PixelFlowLevelData CreateMixedLevel(int waitingSlotCount, int ringOffset, int stripeOffset, int diagonalOffset,
        PixelPigColor[] palette)
    {
        var cells = new List<PixelCellData>();

        for (var y = 0; y < BoardSize; y++)
        {
            for (var x = 0; x < BoardSize; x++)
            {
                var color = GetMixedColor(x, y, ringOffset, stripeOffset, diagonalOffset, palette);
                cells.Add(new PixelCellData(x, y, color));
            }
        }

        return CreateLevel(waitingSlotCount, cells);
    }

    private static PixelFlowLevelData CreateSpiralLevel(int waitingSlotCount, int offsetA, int offsetB, PixelPigColor[] palette)
    {
        var cells = new List<PixelCellData>();

        for (var y = 0; y < BoardSize; y++)
        {
            for (var x = 0; x < BoardSize; x++)
            {
                var ringIndex = Min(Min(x, y), Min(BoardSize - 1 - x, BoardSize - 1 - y));
                var edgeBias = x >= y ? x : y;
                var paletteIndex = (ringIndex + (edgeBias / 3) + offsetA + ((x + y + offsetB) / 5)) % palette.Length;
                cells.Add(new PixelCellData(x, y, palette[(paletteIndex + palette.Length) % palette.Length]));
            }
        }

        return CreateLevel(waitingSlotCount, cells);
    }

    private static PixelFlowLevelData CreateQuadrantLevel(int waitingSlotCount, int offsetA, int offsetB, PixelPigColor[] palette)
    {
        var cells = new List<PixelCellData>();

        for (var y = 0; y < BoardSize; y++)
        {
            for (var x = 0; x < BoardSize; x++)
            {
                var quadrant = (x < BoardSize / 2 ? 0 : 1) + (y < BoardSize / 2 ? 0 : 2);
                var band = ((x + offsetA) / 4 + (y + offsetB) / 4) % palette.Length;
                var paletteIndex = (quadrant + band + offsetA) % palette.Length;
                cells.Add(new PixelCellData(x, y, palette[(paletteIndex + palette.Length) % palette.Length]));
            }
        }

        return CreateLevel(waitingSlotCount, cells);
    }

    private static PixelFlowLevelData CreateLevel(int waitingSlotCount, List<PixelCellData> cells)
    {
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

    private static PixelPigColor GetMixedColor(int x, int y, int ringOffset, int stripeOffset, int diagonalOffset, PixelPigColor[] palette)
    {
        var ringIndex = Min(Min(x, y), Min(BoardSize - 1 - x, BoardSize - 1 - y));
        var stripeIndex = (x / 3 + stripeOffset) % 4;
        var diagonalIndex = ((x + y) / 4 + diagonalOffset) % 4;

        if (ringIndex < 2)
        {
            return GetColor(palette, (ringIndex + ringOffset) % palette.Length);
        }

        if (ringIndex < 5)
        {
            return GetColor(palette, (stripeIndex + ringOffset) % palette.Length);
        }

        if (ringIndex < 8)
        {
            return GetColor(palette, (diagonalIndex + stripeOffset) % palette.Length);
        }

        return GetColor(palette, ((x / 2) + (y / 2) + diagonalOffset) % palette.Length);
    }

    private static PixelPigColor GetColor(PixelPigColor[] palette, int index)
    {
        return palette[(index % palette.Length + palette.Length) % palette.Length];
    }

    private static int Min(int left, int right)
    {
        return left < right ? left : right;
    }
}
