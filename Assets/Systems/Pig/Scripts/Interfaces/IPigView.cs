using System;
using UnityEngine;

public interface IPigView
{
    Vector2 Position { get; }

    void SetPosition(Vector2 anchoredPosition);
    void SetWorldPosition(Vector3 worldPosition);
    void SetMovementDirection(Vector2 direction);
    void SetAimDirection(Vector2 direction);
    void SetAmmo(int ammo);
    void SetClickAction(Action onClick);
    void ClearClickAction();
    void PlayLaunch();
    void PlayHitFeedback();
    void PlayExhaustedAndDestroy();
    void DestroySelf();
}
