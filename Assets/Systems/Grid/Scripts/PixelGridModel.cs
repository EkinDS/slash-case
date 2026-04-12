using System.Collections.Generic;

public sealed class PixelGridModel
{
    private const int FixedBoardSize = 20;

    private readonly PixelPigColor[,] colors;
    private readonly bool[,] alive;
    private readonly Dictionary<PixelPigColor, int> remainingByColor = new Dictionary<PixelPigColor, int>();

    public PixelGridModel(PixelFlowLevelData levelData)
    {
        Width = FixedBoardSize;
        Height = FixedBoardSize;
        colors = new PixelPigColor[Width, Height];
        alive = new bool[Width, Height];

        foreach (PixelPigColor color in System.Enum.GetValues(typeof(PixelPigColor)))
        {
            if (color != PixelPigColor.None)
            {
                remainingByColor[color] = 0;
            }
        }

        if (levelData?.cells == null)
        {
            return;
        }

        for (var i = 0; i < levelData.cells.Length; i++)
        {
            var cell = levelData.cells[i];

            if (cell == null || cell.color == PixelPigColor.None)
            {
                continue;
            }

            if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height)
            {
                continue;
            }

            colors[cell.x, cell.y] = cell.color;
            alive[cell.x, cell.y] = true;
            remainingByColor[cell.color]++;
            RemainingPixelCount++;
        }
    }

    public int Width { get; }
    public int Height { get; }
    public int RemainingPixelCount { get; private set; }

    public PixelPigColor GetColor(int x, int y)
    {
        return IsInside(x, y) && alive[x, y] ? colors[x, y] : PixelPigColor.None;
    }

    public int GetRemainingCount(PixelPigColor color)
    {
        return remainingByColor.TryGetValue(color, out var count) ? count : 0;
    }

    public bool TryHitFirstVisibleMatchingPixel(ConveyorSide side, int lineIndex, PixelPigColor pigColor,
        out PixelHitResult hitResult)
    {
        hitResult = default;

        switch (side)
        {
            case ConveyorSide.Top:
                return TryHitColumn(lineIndex, 0, Height, 1, pigColor, out hitResult);
            case ConveyorSide.Bottom:
                return TryHitColumn(lineIndex, Height - 1, -1, -1, pigColor, out hitResult);
            case ConveyorSide.Left:
                return TryHitRow(lineIndex, 0, Width, 1, pigColor, out hitResult);
            default:
                return TryHitRow(lineIndex, Width - 1, -1, -1, pigColor, out hitResult);
        }
    }

    private bool TryHitColumn(int x, int startY, int endExclusive, int step, PixelPigColor pigColor,
        out PixelHitResult hitResult)
    {
        hitResult = default;

        if (x < 0 || x >= Width)
        {
            return false;
        }

        for (var y = startY; y != endExclusive; y += step)
        {
            if (!alive[x, y])
            {
                continue;
            }

            if (colors[x, y] != pigColor)
            {
                return false;
            }

            alive[x, y] = false;
            RemainingPixelCount--;
            remainingByColor[pigColor]--;
            hitResult = new PixelHitResult(x, y, pigColor);
            return true;
        }

        return false;
    }

    private bool TryHitRow(int y, int startX, int endExclusive, int step, PixelPigColor pigColor,
        out PixelHitResult hitResult)
    {
        hitResult = default;

        if (y < 0 || y >= Height)
        {
            return false;
        }

        for (var x = startX; x != endExclusive; x += step)
        {
            if (!alive[x, y])
            {
                continue;
            }

            if (colors[x, y] != pigColor)
            {
                return false;
            }

            alive[x, y] = false;
            RemainingPixelCount--;
            remainingByColor[pigColor]--;
            hitResult = new PixelHitResult(x, y, pigColor);
            return true;
        }

        return false;
    }

    private bool IsInside(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }
}
