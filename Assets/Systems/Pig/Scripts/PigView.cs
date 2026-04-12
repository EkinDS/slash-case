using System.Collections;
using TMPro;
using UnityEngine;

public sealed class PigView : MonoBehaviour, IPigView
{
    private const float ConveyorRideHeight = 0.85F;
    private static readonly Quaternion AmmoTextWorldRotation = Quaternion.Euler(90F, 0F, 0F);

    private Transform cachedTransform;
    private Transform visualRoot;
    private GameObject bodyObject;
    private TextMeshPro ammoText;
    private Coroutine activeRoutine;
    private Vector2 lastDirection = Vector2.right;
    private float walkCycle;

    public Vector2 Position => new Vector2(cachedTransform.position.x, cachedTransform.position.z);

    public void Initialize(Transform parent, PixelPigColor color)
    {
        cachedTransform = transform;
        cachedTransform.SetParent(parent, false);
        cachedTransform.localPosition = Vector3.zero;

        visualRoot = new GameObject("VisualRoot").transform;
        visualRoot.SetParent(cachedTransform, false);

        bodyObject = WorldObjectUtility.CreatePrimitive(
            "Body",
            PrimitiveType.Cube,
            visualRoot,
            new Vector3(0F, 0.42F, 0F),
            new Vector3(0.72F, 0.72F, 0.72F));
        WorldObjectUtility.SetColor(bodyObject, color.ToUnityColor());
        Destroy(bodyObject.GetComponent<Collider>());

        ammoText = WorldObjectUtility.CreateWorldText(
            "Ammo",
            cachedTransform,
            new Vector3(0F, 1.6F, 0F),
            "0",
            96,
            Color.black,
            TextAnchor.MiddleCenter,
            0.12F);
        ammoText.text = "0";
        OrientAmmoText();
    }

    public void SetPosition(Vector2 anchoredPosition)
    {
        cachedTransform.position = new Vector3(anchoredPosition.x, ConveyorRideHeight, anchoredPosition.y);
        walkCycle += Time.deltaTime * 14F;
        bodyObject.transform.localPosition = new Vector3(0F, 0.42F + Mathf.Abs(Mathf.Sin(walkCycle)) * 0.08F, 0F);
        OrientAmmoText();
    }

    public void SetMovementDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.0001F)
        {
            return;
        }

        lastDirection = direction.normalized;
        visualRoot.rotation = Quaternion.LookRotation(new Vector3(lastDirection.x, 0F, lastDirection.y), Vector3.up);
        OrientAmmoText();
    }

    public void SetAmmo(int ammo)
    {
        ammoText.text = ammo.ToString();
    }

    public void PlayLaunch()
    {
        StartManagedRoutine(PulseScale(Vector3.one * 0.65F, Vector3.one, 0.18F));
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
        var originalScale = cachedTransform.localScale;
        var originalColor = bodyObject.GetComponent<Renderer>().material.color;
        WorldObjectUtility.SetColor(bodyObject, Color.white);
        bodyObject.transform.localScale = Vector3.one * 1.12F;
        yield return new WaitForSeconds(0.06F);
        WorldObjectUtility.SetColor(bodyObject, originalColor);
        cachedTransform.localScale = originalScale;
        bodyObject.transform.localScale = Vector3.one;
        activeRoutine = null;
    }

    private IEnumerator ExhaustRoutine()
    {
        var duration = 0.2F;
        var elapsed = 0F;
        var startScale = cachedTransform.localScale;
        var originalColor = bodyObject.GetComponent<Renderer>().material.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            cachedTransform.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, t);
            WorldObjectUtility.SetColor(bodyObject,
                Color.Lerp(originalColor, new Color(originalColor.r, originalColor.g, originalColor.b, 0.1F), t));
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
        if (ammoText != null)
        {
            ammoText.transform.rotation = AmmoTextWorldRotation;
        }
    }
}
