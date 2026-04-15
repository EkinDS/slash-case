using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class WaitingSlotsView : MonoBehaviour, IWaitingSlotsView
{
    private const string PigPrefabResourcePath = "Pig/Prefabs/Pig";
    private const float QueuedPigSpacing = 2.15F;
    private const float QueuedPigFrontOffset = 1.3F;
    private const float LineSpacing = 4.5F;
    private const float LineDepthOffset = -5.6F;
    private static readonly Vector3 SlotPigScale = Vector3.one * 1.1F;
    private static readonly Vector3 QueuedPigScaleVector = Vector3.one * 1.2F;
    private static readonly Vector3 PigColliderCenter = new Vector3(0F, 0.55F, 0F);
    private static readonly Vector3 PigColliderSize = new Vector3(1.2F, 1.1F, 1.2F);

    private readonly List<GameObject> slotObjects = new List<GameObject>();
    private readonly List<GameObject> slotPigObjects = new List<GameObject>();
    private readonly List<Transform> lineRoots = new List<Transform>();
    private readonly List<List<GameObject>> linePigObjects = new List<List<GameObject>>();
    private Transform slotRoot;
    private Coroutine warningRoutine;
    private GameObject pigPrefab;

    public event Action<int> SlotClicked;
    public event Action<int> PigLineClicked;

    public void Initialize(Transform parent)
    {
        slotRoot = new GameObject("WaitingSlots").transform;
        slotRoot.SetParent(parent, false);
        slotRoot.localPosition = new Vector3(0F, 0F, -14.8F);
    }

    public void BuildSlots(int slotCount)
    {
        for (var i = 0; i < slotObjects.Count; i++)
        {
            Destroy(slotObjects[i]);
        }

        slotObjects.Clear();
        ClearSlotPigs();

        var safeCount = Mathf.Max(1, slotCount);
        var spacing = 1.8F;
        var totalWidth = (safeCount - 1) * spacing;

        for (var i = 0; i < safeCount; i++)
        {
            var localIndex = i;
            var slotObject = WorldObjectUtility.CreatePrimitive(
                $"Slot_{i}",
                PrimitiveType.Cube,
                slotRoot,
                new Vector3(-totalWidth * 0.5F + i * spacing, 0F, 0F),
                new Vector3(1.25F, 0.35F, 1.25F),
                WorldMaterialRole.WaitingSlotEmpty);
            var relay = slotObject.AddComponent<ClickRelay>();
            relay.Clicked += () => SlotClicked?.Invoke(localIndex);

            slotObjects.Add(slotObject);
            slotPigObjects.Add(null);
        }
    }

    public void Render(IReadOnlyList<WaitingSlotState> slotStates, IReadOnlyList<IReadOnlyList<QueuedPigState>> lineStates)
    {
        for (var i = 0; i < slotObjects.Count; i++)
        {
            var state = i < slotStates.Count ? slotStates[i] : new WaitingSlotState(false, PixelPigColor.None, 0);

            if (state.HasPig)
            {
                EnsureSlotPig(i, state);
                EnsureSlotCollider(slotObjects[i]);
            }
            else
            {
                DestroySlotPig(i);
                RemoveSlotCollider(slotObjects[i]);
            }

            WorldObjectUtility.SetMaterial(slotObjects[i], WorldMaterialPalette.Get(WorldMaterialRole.WaitingSlotEmpty));
        }

        RenderPigLines(lineStates);
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

    private void RenderPigLines(IReadOnlyList<IReadOnlyList<QueuedPigState>> lineStates)
    {
        EnsureLineRoots(lineStates != null ? lineStates.Count : 0);

        for (var lineIndex = 0; lineIndex < lineRoots.Count; lineIndex++)
        {
            var pigObjects = linePigObjects[lineIndex];

            for (var i = 0; i < pigObjects.Count; i++)
            {
                Destroy(pigObjects[i]);
            }

            pigObjects.Clear();

            var states = lineStates != null && lineIndex < lineStates.Count ? lineStates[lineIndex] : null;

            if (states == null)
            {
                continue;
            }

            for (var pigIndex = 0; pigIndex < states.Count; pigIndex++)
            {
                var state = states[pigIndex];
                var pigObject = CreatePigObject(
                    $"QueuedPig_{lineIndex}_{pigIndex}",
                    lineRoots[lineIndex],
                    new Vector3(0F, 0F, QueuedPigFrontOffset - pigIndex * QueuedPigSpacing),
                    state.Color,
                    state.Ammo,
                    QueuedPigScaleVector);

                if (pigObject == null)
                {
                    continue;
                }

                if (pigIndex == 0)
                {
                    var capturedLineIndex = lineIndex;
                    var relay = pigObject.AddComponent<ClickRelay>();
                    relay.Clicked += () => PigLineClicked?.Invoke(capturedLineIndex);
                }
                else
                {
                    Destroy(pigObject.GetComponent<Collider>());
                }

                pigObjects.Add(pigObject);
            }
        }
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
            linePigObjects.Add(new List<GameObject>());
        }

        var totalWidth = (safeLineCount - 1) * LineSpacing;

        for (var lineIndex = 0; lineIndex < lineRoots.Count; lineIndex++)
        {
            lineRoots[lineIndex].localPosition = new Vector3(-totalWidth * 0.5F + lineIndex * LineSpacing, 0F, LineDepthOffset);
            lineRoots[lineIndex].gameObject.SetActive(lineIndex < safeLineCount);
        }
    }

    private static void EnsureSlotCollider(GameObject slotObject)
    {
        if (slotObject != null && slotObject.GetComponent<Collider>() == null)
        {
            slotObject.AddComponent<BoxCollider>();
        }
    }

    private static void RemoveSlotCollider(GameObject slotObject)
    {
        var collider = slotObject != null ? slotObject.GetComponent<Collider>() : null;

        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private void EnsureSlotPig(int slotIndex, WaitingSlotState state)
    {
        if (slotIndex < 0 || slotIndex >= slotObjects.Count)
        {
            return;
        }

        var existingPig = slotIndex < slotPigObjects.Count ? slotPigObjects[slotIndex] : null;

        if (existingPig == null)
        {
            existingPig = CreatePigObject(
                $"SlotPig_{slotIndex}",
                slotObjects[slotIndex].transform,
                new Vector3(0F, 0.18F, 0F),
                state.Color,
                state.Ammo,
                SlotPigScale);

            if (slotIndex < slotPigObjects.Count)
            {
                slotPigObjects[slotIndex] = existingPig;
            }
        }

        if (existingPig == null)
        {
            return;
        }

        var pigView = existingPig.GetComponent<PigView>();

        if (pigView != null)
        {
            pigView.Initialize(existingPig.transform.parent, state.Color);
            existingPig.transform.localPosition = new Vector3(0F, 0.18F, 0F);
            existingPig.transform.localScale = SlotPigScale;
            pigView.SetAmmo(state.Ammo);
        }
    }

    private void DestroySlotPig(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotPigObjects.Count)
        {
            return;
        }

        if (slotPigObjects[slotIndex] != null)
        {
            Destroy(slotPigObjects[slotIndex]);
            slotPigObjects[slotIndex] = null;
        }
    }

    private void ClearSlotPigs()
    {
        for (var i = 0; i < slotPigObjects.Count; i++)
        {
            if (slotPigObjects[i] != null)
            {
                Destroy(slotPigObjects[i]);
            }
        }

        slotPigObjects.Clear();
    }

    private GameObject CreatePigObject(string name, Transform parent, Vector3 localPosition, PixelPigColor color, int ammo, Vector3 localScale)
    {
        var prefab = GetPigPrefab();

        if (prefab == null)
        {
            return null;
        }

        var pigObject = Instantiate(prefab, parent, false);
        pigObject.name = name;
        pigObject.transform.localPosition = localPosition;
        pigObject.transform.localScale = localScale;
        EnsurePigCollider(pigObject);

        var pigView = pigObject.GetComponent<PigView>();

        if (pigView != null)
        {
            pigView.Initialize(parent, color);
            pigObject.transform.localPosition = localPosition;
            pigObject.transform.localScale = localScale;
            pigView.SetAmmo(ammo);
        }

        return pigObject;
    }

    private static void EnsurePigCollider(GameObject pigObject)
    {
        if (pigObject == null)
        {
            return;
        }

        var collider = pigObject.GetComponent<BoxCollider>();

        if (collider == null)
        {
            collider = pigObject.AddComponent<BoxCollider>();
        }

        collider.center = PigColliderCenter;
        collider.size = PigColliderSize;
    }

    private GameObject GetPigPrefab()
    {
        if (pigPrefab == null)
        {
            pigPrefab = Resources.Load<GameObject>(PigPrefabResourcePath);
        }

        if (pigPrefab == null)
        {
            Debug.LogError($"Pig prefab not found at Resources path '{PigPrefabResourcePath}'.");
        }

        return pigPrefab;
    }
}
