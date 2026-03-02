using UnityEngine;

/// <summary>
/// Base class for anything that should be saveable in the game.
/// Automatically assigns a UniqueID and registers itself with SaveManager.
/// </summary>
public abstract class SaveableEntity : MonoBehaviour
{
    [SerializeField]
    private string uniqueID;

    /// <summary>
    /// Returns the unique identifier of this object.
    /// </summary>
    /// <returns>The unique ID string.</returns>
    public string GetUniqueID() => this.uniqueID;

    /// <summary>
    /// Capture the state of all [SaveField] variables and the transform.
    /// </summary>
    /// <returns>Returns the dictionary in the SaveUtility that this object will be saved in.</returns>
    public virtual object CaptureState()
    {
        // Capture all fields marked with [SaveField]
        var state = SaveUtility.Capture(this) as System.Collections.Generic.Dictionary<string, object>;

        // Capture transform automatically
        TransformData tdata = new TransformData
        {
            Position = this.transform.position,
            Rotation = this.transform.rotation,
            Scale = this.transform.localScale,
        };
        state["__Transform"] = tdata;

        return state;
    }

    /// <summary>
    /// Restore all [SaveField] variables and the transform from saved data.
    /// </summary>
    /// <param name="state">The state that we will return to this object.</param>
    public virtual void RestoreState(object state)
    {
        if (!(state is System.Collections.Generic.Dictionary<string, object> stateDict))
        {
            return;
        }

        // Restore all fields marked with [SaveField]
        SaveUtility.Restore(this, stateDict);

        // Restore transform automatically
        if (stateDict.TryGetValue("__Transform", out object tobj) && tobj is TransformData tdata)
        {
            this.transform.position = tdata.Position;
            this.transform.rotation = tdata.Rotation;
            this.transform.localScale = tdata.Scale;
        }
    }

    /// <summary>
    /// Checks if this object already has a unique id, if it doesn't, it creates a new one.
    /// </summary>
    protected virtual void Awake()
    {
        // Generate a new ID if this object doesn't have one
        if (string.IsNullOrEmpty(this.uniqueID))
        {
            this.uniqueID = System.Guid.NewGuid().ToString();
        }

        // Register with the SaveManager
        SaveManager.Register(this);
    }

    /// <summary>
    /// Special container to save transform data automatically.
    /// </summary>
    [System.Serializable]
    public class TransformData
    {
        [SerializeField]
        private Vector3 position;
        [SerializeField]
        private Quaternion rotation;
        [SerializeField]
        private Vector3 scale;

        /// <summary>
        /// Gets or sets the position of this object.
        /// </summary>
        public Vector3 Position
        {
            get => this.position;
            set => this.position = value;
        }

        /// <summary>
        /// Gets or sets the rotation of this object.
        /// </summary>
        public Quaternion Rotation
        {
            get => this.rotation;
            set => this.rotation = value;
        }

        /// <summary>
        /// Gets or sets the scale of this object.
        /// </summary>
        public Vector3 Scale
        {
            get => this.scale;
            set => this.scale = value;
        }
    }
}