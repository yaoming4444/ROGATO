using System;
using UnityEngine;

public class PopupVisibilityNotifier : MonoBehaviour
{
    public event Action Shown;
    public event Action Hidden;

    private void OnEnable() => Shown?.Invoke();
    private void OnDisable() => Hidden?.Invoke();
}

