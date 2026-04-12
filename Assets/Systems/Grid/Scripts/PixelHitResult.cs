public readonly struct PixelHitResult
{
    public PixelHitResult(int x, int y, PixelPigColor color)
    {
        X = x;
        Y = y;
        Color = color;
    }

    public int X { get; }
    public int Y { get; }
    public PixelPigColor Color { get; }
}
