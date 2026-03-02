using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for anything that should be saveable in the game.
/// Automatically assigns a UniqueID and registers itself with SaveManager.
/// </summary>
public abstract class SaveableEntity : MonoBehaviour
{
    [SerializeField]
    private string uniqueID;

    public string GetUniqueID() => this.uniqueID;

    /// <summary>
    /// Capture the state of all [SaveField] variables and the transform.
    /// </summary>
    public virtual Dictionary<string, object> CaptureState()
    {
        var state = SaveUtility.Capture(this);

        // Capture transform automatically
        TransformData tdata = new TransformData
        {
            Position = new SerializableVector3(transform.position),
            Rotation = new SerializableQuaternion(transform.rotation),
            Scale = new SerializableVector3(transform.localScale)
        };
        state["__Transform"] = tdata;

        Debug.Log($"[SaveableEntity] Captured state for {name} ({GetType().Name}) with {state.Count} fields.");
        return state;
    }

    /// <summary>
    /// Restore all [SaveField] variables and the transform from saved data.
    /// </summary>
    public virtual void RestoreState(Dictionary<string, object> state)
    {
        if (state == null)
        {
            Debug.LogWarning($"[SaveableEntity] RestoreState called with null for {name}");
            return;
        }

        SaveUtility.Restore(this, state);

        if (state.TryGetValue("__Transform", out object tobj) && tobj is TransformData tdata)
        {
            transform.position = tdata.Position.ToVector3();
            transform.rotation = tdata.Rotation.ToQuaternion();
            transform.localScale = tdata.Scale.ToVector3();
        Debug.Log($"[SaveableEntity] Restored transform for {name}: {tdata.Position}");
        }
    }

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = Guid.NewGuid().ToString();

        SaveManager.Register(this);
        Debug.Log($"[SaveableEntity] Registered {name} with ID {uniqueID}");
    }

    [Serializable]
    public class TransformData
    {
        public SerializableVector3 Position;
        public SerializableQuaternion Rotation;
        public SerializableVector3 Scale;
    }
}