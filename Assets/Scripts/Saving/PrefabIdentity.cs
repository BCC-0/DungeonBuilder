using UnityEngine;

/// <summary>
/// Stores the prefab ID on a prefab so it can be saved/loaded automatically.
/// </summary>
[DisallowMultipleComponent]
public class PrefabIdentity : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Unique ID for this prefab used by the save system.")]
    private string prefabID;

    /// <summary>
    /// Gets or sets the prefab ID for this prefab.
    /// </summary>
    public string PrefabID
    {
        get => this.prefabID;
        set => this.prefabID = value;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        if (string.IsNullOrEmpty(this.prefabID))
        {
            this.prefabID = this.gameObject.name;
        }
    }
#endif
}