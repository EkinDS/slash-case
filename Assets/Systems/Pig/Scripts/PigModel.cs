public sealed class PigModel
{
    private const int UninitializedShotSide = -1;
    private const int UninitializedShotLine = -1;

    public PigModel(int id, PixelPigColor color, int ammo)
    {
        Id = id;
        Color = color;
        AmmoRemaining = ammo;
        ResetShotTracking();
    }

    public int Id { get; }
    public PixelPigColor Color { get; }
    public int AmmoRemaining { get; private set; }
    public float Distance { get; set; }
    public float TotalDistanceTravelled { get; set; }
    public bool IsActive { get; set; }
    public int LastShotSide { get; set; }
    public int LastShotLineIndex { get; set; }

    public void ConsumeAmmo()
    {
        AmmoRemaining = System.Math.Max(0, AmmoRemaining - 1);
    }

    public void ResetShotTracking()
    {
        LastShotSide = UninitializedShotSide;
        LastShotLineIndex = UninitializedShotLine;
    }
}
