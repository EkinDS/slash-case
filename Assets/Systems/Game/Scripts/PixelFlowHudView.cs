using System;
using TMPro;
using UnityEngine;

public sealed class PixelFlowHudView : MonoBehaviour, IPixelFlowHudView
{
    private TextMeshPro statusText;
    private TextMeshPro startSolvableText;
    private TextMeshPro unlosableText;
    private TextMeshPro levelText;

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
            "Launch pigs from the front of each line",
            56,
            Color.white,
            TextAnchor.MiddleLeft,
            0.08F);

        startSolvableText = WorldObjectUtility.CreateWorldText(
            "StartSolvable",
            transform,
            new Vector3(-0.8F, 0.4F, 12.9F),
            "Start: Solvable",
            42,
            new Color32(114, 255, 167, 255),
            TextAnchor.MiddleCenter,
            0.065F);

        unlosableText = WorldObjectUtility.CreateWorldText(
            "Unlosable",
            transform,
            new Vector3(4.2F, 0.4F, 12.9F),
            "Now: Risky",
            42,
            new Color32(194, 203, 221, 255),
            TextAnchor.MiddleCenter,
            0.065F);

        levelText = WorldObjectUtility.CreateWorldText(
            "Level",
            transform,
            new Vector3(9.2F, 0.4F, 12.9F),
            "Level 1",
            56,
            Color.white,
            TextAnchor.MiddleRight,
            0.08F);
    }

    public void SetStatus(string text, Color color)
    {
        statusText.text = text;
        statusText.color = color;
        statusText.GetComponent<MeshRenderer>().material.color = color;
    }

    public void SetStartSolvableState(bool solvable)
    {
        if (startSolvableText == null)
        {
            return;
        }

        var color = solvable ? new Color32(114, 255, 167, 255) : new Color32(255, 122, 122, 255);
        startSolvableText.text = solvable ? "Start: Solvable" : "Start: Unsolvable";
        startSolvableText.color = color;
        startSolvableText.GetComponent<MeshRenderer>().material.color = color;
    }

    public void SetUnlosableState(bool unlosable)
    {
        if (unlosableText == null)
        {
            return;
        }

        var color = unlosable ? new Color32(255, 224, 107, 255) : new Color32(194, 203, 221, 255);
        unlosableText.text = unlosable ? "Now: Unlosable" : "Now: Risky";
        unlosableText.color = color;
        unlosableText.GetComponent<MeshRenderer>().material.color = color;
    }

    public void SetEditorButtonLabel(string label)
    {
    }

    public void SetLevelLabel(string text)
    {
        if (levelText != null)
        {
            levelText.text = text;
        }
    }
}
