using System;
using UnityEngine;

public interface IPixelGridView
{
    event Action<int, int> CellClicked;

    Rect BoardRect { get; }

    void BuildGrid(int width, int height);
    void RenderCell(int x, int y, PixelPigColor color, bool alive);
    Vector2 GetCellCenter(int x, int y);
    void SetEditorSelection(PixelPigColor color);
    void PlayShot(Vector2 from, Vector2 to, PixelPigColor color, Action onImpact);
    void PlayCellHit(int x, int y, Action onComplete);
    void SetConveyorCapacity(int remainingCapacity, int totalCapacity);
}
