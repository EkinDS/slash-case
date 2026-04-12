public readonly struct QueuedPigState
{
    public QueuedPigState(PixelPigColor color, int ammo)
    {
        Color = color;
        Ammo = ammo;
    }

    public PixelPigColor Color { get; }
    public int Ammo { get; }
}
