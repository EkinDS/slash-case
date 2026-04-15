using UnityEngine;

public interface IPigViewFactory
{
    IPigView Create(Transform parent, string viewName, PixelPigColor color, int ammo);
}
