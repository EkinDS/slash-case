public readonly struct WaitingSlotState
{
    public WaitingSlotState(bool hasPig, PixelPigColor color, int ammo)
    {
        HasPig = hasPig;
        Color = color;
        Ammo = ammo;
    }

    public bool HasPig { get; }
    public PixelPigColor Color { get; }
    public int Ammo { get; }
}
