using System.Collections;
using System;
using TMPro;
using UnityEngine;

public sealed class PigView : MonoBehaviour, IPigView
{
    private const float PigScaleMultiplier = 3F;
    private static readonly Vector3 ClickColliderCenter = new Vector3(0F, 0.55F, 0F);
    private static readonly Vector3 ClickColliderSize = new Vector3(1.2F, 1.1F, 1.2F);
    [SerializeField] private SkinnedMeshRenderer renderer;
    [SerializeField] private TextMeshPro bulletCountText;
    
    private const float ConveyorRideHeight = 0.85F;
    private static readonly Vector3 LaunchStartScale = Vector3.one * (0.65F * PigScaleMultiplier);
    private static readonly Vector3 LaunchEndScale = Vector3.one * PigScaleMultiplier;
    private static readonly Quaternion AmmoTextWorldRotation = Quaternion.Euler(90F, 0F, 0F);

    private Transform cachedTransform;
    private Transform rendererTransform;
    private Coroutine activeRoutine;
    private Vector2 lastDirection = Vector2.right;
    private float walkCycle;
    private Vector3 rendererBaseLocalPosition;
    private Vector3 rendererBaseLocalScale;
    private Quaternion rendererBaseLocalRotation;

    public Vector2 Position => new Vector2(cachedTransform.position.x, cachedTransform.position.z);

    public void Initialize(Transform parent, PixelPigColor color)
    {
        cachedTransform = transform;
        cachedTransform.SetParent(parent, false);
        cachedTransform.localPosition = Vector3.zero;
        cachedTransform.localScale = LaunchEndScale;
        cachedTransform.rotation = Quaternion.identity;

        rendererTransform = renderer != null ? renderer.transform : null;

        if (rendererTransform != null)
        {
            rendererBaseLocalPosition = rendererTransform.localPosition;
            rendererBaseLocalScale = rendererTransform.localScale;
            rendererBaseLocalRotation = rendererTransform.localRotation;
        }

        ApplyColor(color);
        SetAmmo(0);
        OrientAmmoText();
    }

    public void SetPosition(Vector2 anchoredPosition)
    {
        cachedTransform.position = new Vector3(anchoredPosition.x, ConveyorRideHeight, anchoredPosition.y);
        walkCycle += Time.deltaTime * 14F;

        if (rendererTransform != null)
        {
            rendererTransform.localPosition = rendererBaseLocalPosition + new Vector3(0F, Mathf.Abs(Mathf.Sin(walkCycle)) * 0.24F, 0F);
        }

        OrientAmmoText();
    }

    public void SetWorldPosition(Vector3 worldPosition)
    {
        cachedTransform.position = worldPosition;
        OrientAmmoText();
    }

    public void SetMovementDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.0001F)
        {
            return;
        }

        lastDirection = direction.normalized;
        ApplyFacing(lastDirection);

        OrientAmmoText();
    }

    public void SetAimDirection(Vector2 direction)
    {
        OrientAmmoText();
    }

    public void SetAmmo(int ammo)
    {
        if (bulletCountText != null)
        {
            bulletCountText.text = ammo.ToString();
        }
    }

    public void SetClickAction(Action onClick)
    {
        var existingRelay = GetComponent<ClickRelay>();

        if (existingRelay != null)
        {
            Destroy(existingRelay);
        }

        var relay = gameObject.AddComponent<ClickRelay>();
        relay.Clicked += onClick;

        var collider = GetComponent<BoxCollider>();

        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider>();
        }

        collider.center = ClickColliderCenter;
        collider.size = ClickColliderSize;
    }

    public void ClearClickAction()
    {
        var relay = GetComponent<ClickRelay>();

        if (relay != null)
        {
            Destroy(relay);
        }

        var collider = GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }
    }

    public void PlayLaunch()
    {
        StartManagedRoutine(PulseScale(LaunchStartScale, LaunchEndScale, 0.18F));
    }

    public void PlayHitFeedback()
    {
        StartManagedRoutine(HitRoutine());
    }

    public void PlayExhaustedAndDestroy()
    {
        StartManagedRoutine(ExhaustRoutine());
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    private IEnumerator HitRoutine()
    {
        cachedTransform.localScale = LaunchEndScale * 1.08F;

        if (rendererTransform != null)
        {
            rendererTransform.localScale = rendererBaseLocalScale * 1.05F;
        }

        yield return new WaitForSeconds(0.06F);
        cachedTransform.localScale = LaunchEndScale;

        if (rendererTransform != null)
        {
            rendererTransform.localScale = rendererBaseLocalScale;
        }

        activeRoutine = null;
    }

    private IEnumerator ExhaustRoutine()
    {
        var duration = 0.2F;
        var elapsed = 0F;
        var startScale = cachedTransform.localScale;
        var originalColor = renderer != null ? renderer.material.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            cachedTransform.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, t);

            if (renderer != null)
            {
                renderer.material.color = Color.Lerp(originalColor, new Color(originalColor.r, originalColor.g, originalColor.b, 0.1F), t);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator PulseScale(Vector3 from, Vector3 to, float duration)
    {
        var elapsed = 0F;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            cachedTransform.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        cachedTransform.localScale = to;
        activeRoutine = null;
    }

    private void StartManagedRoutine(IEnumerator routine)
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(routine);
    }

    private void OrientAmmoText()
    {
        if (bulletCountText != null)
        {
            bulletCountText.transform.rotation = AmmoTextWorldRotation;
        }
    }

    private void ApplyColor(PixelPigColor color)
    {
        if (renderer != null)
        {
            renderer.material.color = color.ToUnityColor();
        }
    }

    private void ApplyFacing(Vector2 direction)
    {
        if (cachedTransform == null)
        {
            return;
        }

        var cardinalDirection = GetCardinalDirection(direction);
        var facingDirection = new Vector2(-cardinalDirection.y, cardinalDirection.x);
        cachedTransform.rotation = Quaternion.LookRotation(new Vector3(facingDirection.x, 0F, facingDirection.y), Vector3.up);
    }

    private static Vector2 GetCardinalDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return direction.x >= 0F ? Vector2.right : Vector2.left;
        }

        return direction.y >= 0F ? Vector2.up : Vector2.down;
    }
}
