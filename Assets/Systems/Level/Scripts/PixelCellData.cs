using System;

[Serializable]
public class PixelCellData
{
    public int x;
    public int y;
    public PixelPigColor color;

    public PixelCellData()
    {
    }

    public PixelCellData(int x, int y, PixelPigColor color)
    {
        this.x = x;
        this.y = y;
        this.color = color;
    }
}
