using System;
using TMPro;
using UnityEngine;

public sealed class PixelFlowHudView : MonoBehaviour, IPixelFlowHudView
{
    private TextMeshPro statusText;
    private TextMeshPro guaranteeText;

    public event Action RestartRequested;
    public event Action EditorToggleRequested;

    public void Initialize(Transform parent)
    {
        transform.SetParent(parent, false);
        transform.position = new Vector3(0F, 0F, 0F);

        statusText = WorldObjectUtility.CreateWorldText(
            "Status",
            transform,
            new Vector3(-9.5F, 0.4F, 12.9F),
            "Launch pigs from the slots",
            56,
            Color.white,
            TextAnchor.MiddleLeft,
            0.08F);

        guaranteeText = WorldObjectUtility.CreateWorldText(
            "Guarantee",
            transform,
            new Vector3(0F, 0.4F, 12.9F),
            "Guaranteed Finish",
            54,
            new Color32(255, 224, 107, 255),
            TextAnchor.MiddleCenter,
            0.08F);
        guaranteeText.gameObject.SetActive(false);
    }

    public void SetStatus(string text, Color color)
    {
        statusText.text = text;
        statusText.color = color;
        statusText.GetComponent<MeshRenderer>().material.color = color;
    }

    public void SetGuaranteeVisible(bool visible)
    {
        guaranteeText.gameObject.SetActive(visible);
    }

    public void SetEditorButtonLabel(string label)
    {
    }
}
