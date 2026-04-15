using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class WaitingSlotsView : MonoBehaviour, IWaitingSlotsView
{
    private const float SlotSpacing = 4F;
    private const float SlotWidth = 2.53F;
    private const float SlotHeight = 0.5F;
    private const float SlotDepth = 2.53F;
    private const float QueuedPigSpacing = 4.8F;
    private const float QueuedPigFrontOffset = 4.8F;
    private const float LineSpacing = 4F;
    private const float LineDepthOffset = -9.5F;
    private const float SlotPigVerticalOffset = 0.54F;

    private readonly List<GameObject> slotObjects = new List<GameObject>();
    private readonly List<Transform> lineRoots = new List<Transform>();
    private Transform slotRoot;
    private Coroutine warningRoutine;

    public event Action<int> SlotClicked;

    public void Initialize(Transform parent)
    {
        slotRoot = new GameObject("WaitingSlots").transform;
        slotRoot.SetParent(parent, false);
        slotRoot.localPosition = new Vector3(0F, 0F, -18.8F);
    }

    public void BuildSlots(int slotCount)
    {
        for (var i = 0; i < slotObjects.Count; i++)
        {
            Destroy(slotObjects[i]);
        }

        slotObjects.Clear();

        var safeCount = Mathf.Max(1, slotCount);
        var totalWidth = (safeCount - 1) * SlotSpacing;

        for (var i = 0; i < safeCount; i++)
        {
            var localIndex = i;
            var slotObject = WorldObjectUtility.CreatePrimitive(
                $"Slot_{i}",
                PrimitiveType.Cube,
                slotRoot,
                new Vector3(-totalWidth * 0.5F + i * SlotSpacing, 0F, 0F),
                new Vector3(SlotWidth, SlotHeight, SlotDepth),
                WorldMaterialRole.WaitingSlotEmpty);
            var relay = slotObject.AddComponent<ClickRelay>();
            relay.Clicked += () => SlotClicked?.Invoke(localIndex);

            slotObjects.Add(slotObject);
        }
    }

    public void SetLineCount(int lineCount)
    {
        EnsureLineRoots(lineCount);
    }

    public void RenderSlots(IReadOnlyList<WaitingSlotState> slotStates)
    {
        for (var i = 0; i < slotObjects.Count; i++)
        {
            var state = i < slotStates.Count ? slotStates[i] : new WaitingSlotState(false, PixelPigColor.None, 0);

            if (state.HasPig)
            {
                EnsureSlotCollider(slotObjects[i]);
            }
            else
            {
                RemoveSlotCollider(slotObjects[i]);
            }

            WorldObjectUtility.SetMaterial(slotObjects[i], WorldMaterialPalette.Get(WorldMaterialRole.WaitingSlotEmpty));
        }
    }

    public void PlayOverflowWarning()
    {
        if (warningRoutine != null)
        {
            StopCoroutine(warningRoutine);
        }

        warningRoutine = StartCoroutine(OverflowWarningRoutine());
    }

    private IEnumerator OverflowWarningRoutine()
    {
        var originalPosition = slotRoot.localPosition;

        for (var i = 0; i < 8; i++)
        {
            slotRoot.localPosition = originalPosition + new Vector3(i % 2 == 0 ? 0.18F : -0.18F, 0F, 0F);
            yield return new WaitForSeconds(0.03F);
        }

        slotRoot.localPosition = originalPosition;
        warningRoutine = null;
    }

    private void EnsureLineRoots(int lineCount)
    {
        var safeLineCount = Mathf.Max(1, lineCount);

        while (lineRoots.Count < safeLineCount)
        {
            var lineIndex = lineRoots.Count;
            var lineRoot = new GameObject($"PigLine_{lineIndex}").transform;
            lineRoot.SetParent(slotRoot, false);
            lineRoots.Add(lineRoot);
        }

        var totalWidth = (safeLineCount - 1) * LineSpacing;

        for (var lineIndex = 0; lineIndex < lineRoots.Count; lineIndex++)
        {
            lineRoots[lineIndex].localPosition = new Vector3(-totalWidth * 0.5F + lineIndex * LineSpacing, 0F, LineDepthOffset);
            lineRoots[lineIndex].gameObject.SetActive(lineIndex < safeLineCount);
        }
    }

    public Vector3 GetSlotWorldPosition(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotObjects.Count)
        {
            return slotRoot != null ? slotRoot.position : Vector3.zero;
        }

        var localPosition = slotObjects[slotIndex].transform.localPosition;
        return slotRoot.TransformPoint(new Vector3(localPosition.x, SlotPigVerticalOffset, localPosition.z));
    }

    public Vector3 GetLinePigWorldPosition(int lineIndex, int pigIndex)
    {
        if (lineIndex < 0 || lineIndex >= lineRoots.Count)
        {
            return slotRoot != null ? slotRoot.position : Vector3.zero;
        }

        var localPosition = new Vector3(0F, 0F, QueuedPigFrontOffset - pigIndex * QueuedPigSpacing);
        return lineRoots[lineIndex].TransformPoint(localPosition);
    }

    private static void EnsureSlotCollider(GameObject slotObject)
    {
        if (slotObject == null)
        {
            return;
        }

        var collider = slotObject.GetComponent<BoxCollider>();

        if (collider == null)
        {
            collider = slotObject.AddComponent<BoxCollider>();
        }

        collider.center = Vector3.zero;
        collider.size = Vector3.one;
    }

    private static void RemoveSlotCollider(GameObject slotObject)
    {
        var collider = slotObject != null ? slotObject.GetComponent<Collider>() : null;

        if (collider != null)
        {
            Destroy(collider);
        }
    }

}
