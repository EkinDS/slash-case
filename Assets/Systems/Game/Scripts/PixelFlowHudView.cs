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
            "StatusText",
            transform,
            new Vector3(0F, 0.25F, 14.11F),
            string.Empty,
            96,
            Color.white,
            TextAnchor.MiddleCenter,
            0.16F);
        statusText.transform.localRotation = Quaternion.Euler(90F, 0F, 0F);
    }

    public void SetStatus(string text, Color color)
    {
        if (statusText == null)
        {
            return;
        }

        if (!string.Equals(text, "Level Completed", StringComparison.Ordinal) &&
            !string.Equals(text, "Level Failed", StringComparison.Ordinal) &&
            !string.IsNullOrEmpty(text))
        {
            return;
        }

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
