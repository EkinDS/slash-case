using UnityEngine;

public interface IPigView
{
    Vector2 Position { get; }

    void SetPosition(Vector2 anchoredPosition);
    void SetMovementDirection(Vector2 direction);
    void SetAimDirection(Vector2 direction);
    void SetAmmo(int ammo);
    void PlayLaunch();
    void PlayHitFeedback();
    void PlayExhaustedAndDestroy();
    void DestroySelf();
}
