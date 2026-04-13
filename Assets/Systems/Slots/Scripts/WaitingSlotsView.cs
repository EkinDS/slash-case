using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class WaitingSlotsView : MonoBehaviour, IWaitingSlotsView
{
    private const float QueuedPigScale = 1.44F;
    private const float QueuedPigSpacing = 2.15F;
    private const float QueuedPigFrontOffset = 1.3F;
    private const float LineSpacing = 4.5F;
    private const float LineDepthOffset = -5.6F;

    private readonly List<GameObject> slotObjects = new List<GameObject>();
    private readonly List<TextMeshPro> slotTexts = new List<TextMeshPro>();
    private readonly List<Transform> lineRoots = new List<Transform>();
    private readonly List<List<GameObject>> linePigObjects = new List<List<GameObject>>();
    private Transform slotRoot;
    private Coroutine warningRoutine;

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
        slotTexts.Clear();

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

            var text = WorldObjectUtility.CreateWorldText(
                "Text",
                slotObject.transform,
                new Vector3(0F, 0.82F, 0F),
                string.Empty,
                56,
                Color.black,
                TextAnchor.MiddleCenter,
                0.0882F);
            text.transform.localRotation = Quaternion.Euler(90F, 0F, 0F);

            slotObjects.Add(slotObject);
            slotTexts.Add(text);
        }
    }

    public void Render(IReadOnlyList<WaitingSlotState> slotStates, IReadOnlyList<IReadOnlyList<QueuedPigState>> lineStates)
    {
        for (var i = 0; i < slotObjects.Count; i++)
        {
            var state = i < slotStates.Count ? slotStates[i] : new WaitingSlotState(false, PixelPigColor.None, 0);

            if (state.HasPig)
            {
                WorldObjectUtility.SetColor(slotObjects[i], state.Color.ToUnityColor());
                EnsureSlotCollider(slotObjects[i]);
            }
            else
            {
                WorldObjectUtility.SetMaterial(slotObjects[i], WorldMaterialPalette.Get(WorldMaterialRole.WaitingSlotEmpty));
                RemoveSlotCollider(slotObjects[i]);
            }

            slotTexts[i].text = state.HasPig ? state.Ammo.ToString() : string.Empty;
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
                var pigObject = WorldObjectUtility.CreatePrimitive(
                    $"QueuedPig_{lineIndex}_{pigIndex}",
                    PrimitiveType.Cube,
                    lineRoots[lineIndex],
                    new Vector3(0F, 0F, QueuedPigFrontOffset - pigIndex * QueuedPigSpacing),
                    new Vector3(QueuedPigScale, 0.56F, QueuedPigScale),
                    state.Color.ToUnityColor());

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

                var text = WorldObjectUtility.CreateWorldText(
                    "Ammo",
                    pigObject.transform,
                    new Vector3(0F, 0.95F, 0F),
                    state.Ammo.ToString(),
                    56,
                    Color.black,
                    TextAnchor.MiddleCenter,
                    0.126F);
                text.transform.localRotation = Quaternion.Euler(90F, 0F, 0F);

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
}
