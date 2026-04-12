using System;

[Serializable]
public class PigSpawnData
{
    public PixelPigColor color;
    public int ammo;

    public PigSpawnData()
    {
    }

    public PigSpawnData(PixelPigColor color, int ammo)
    {
        this.color = color;
        this.ammo = ammo;
    }
}
