using System;
using UnityEngine;

/// <summary>
/// The editor event bus keeps track of editor actions and is called by multiple different control types.
/// </summary>
public static class EditorEventBus
{
    public static Action<Vector3> OnPointerMoved;
    public static Action OnPrimaryDown;
    public static Action OnPrimaryUp;
    public static Action OnDelete;
    public static Action<float> OnZoom;
    public static Action<Vector2> OnPan;

    public static void RaisePointerMoved(Vector3 pos) => OnPointerMoved?.Invoke(pos);
    public static void RaisePrimaryDown() => OnPrimaryDown?.Invoke();
    public static void RaisePrimaryUp() => OnPrimaryUp?.Invoke();
    public static void RaiseDelete() => OnDelete?.Invoke();
    public static void RaiseZoom(float amount) => OnZoom?.Invoke(amount);
    public static void RaisePan(Vector2 delta) => OnPan?.Invoke(delta);
}