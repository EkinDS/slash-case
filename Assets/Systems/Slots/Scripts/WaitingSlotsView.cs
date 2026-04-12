using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class WaitingSlotsView : MonoBehaviour, IWaitingSlotsView
{
    private readonly List<GameObject> slotObjects = new List<GameObject>();
    private readonly List<TextMeshPro> slotTexts = new List<TextMeshPro>();
    private Transform slotRoot;
    private Coroutine warningRoutine;

    public event Action<int> SlotClicked;

    public void Initialize(Transform parent)
    {
        slotRoot = new GameObject("WaitingSlots").transform;
        slotRoot.SetParent(parent, false);
        slotRoot.localPosition = new Vector3(0F, 0F, -14.8F);

        WorldObjectUtility.CreateWorldText(
            "Title",
            slotRoot,
            new Vector3(0F, 0.2F, -2.2F),
            "Waiting Slots",
            96,
            Color.black,
            TextAnchor.MiddleCenter,
            0.16F);
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
                "Empty",
                80,
                Color.black,
                TextAnchor.MiddleCenter,
                0.14F);
            text.transform.localRotation = Quaternion.Euler(90F, 0F, 0F);

            slotObjects.Add(slotObject);
            slotTexts.Add(text);
        }
    }

    public void Render(IReadOnlyList<WaitingSlotState> slotStates)
    {
        for (var i = 0; i < slotObjects.Count; i++)
        {
            var state = i < slotStates.Count ? slotStates[i] : new WaitingSlotState(false, PixelPigColor.None, 0);
            if (state.HasPig)
            {
                WorldObjectUtility.SetColor(slotObjects[i], state.Color.ToUnityColor());
            }
            else
            {
                WorldObjectUtility.SetMaterial(slotObjects[i], WorldMaterialPalette.Get(WorldMaterialRole.WaitingSlotEmpty));
            }
            slotTexts[i].text = state.HasPig ? state.Ammo.ToString() : "Empty";
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
}
