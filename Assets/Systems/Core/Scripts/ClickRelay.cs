using System;
using UnityEngine;

public sealed class ClickRelay : MonoBehaviour
{
    public event Action Clicked;

    private void OnMouseDown()
    {
        Clicked?.Invoke();
    }
}
