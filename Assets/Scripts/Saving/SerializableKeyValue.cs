using System;

/// <summary>
/// Represents a single key/value pair in a serializable dictionary.
/// Value is stored as a JSON string for Unity serialization.
/// </summary>
[Serializable]
public class SerializableKeyValue
{
    private string key;
    private string value;

    /// <summary>
    /// Gets or sets the key of this entry.
    /// </summary>
    public string Key
    {
        get => this.key;
        set => this.key = value;
    }

    /// <summary>
    /// Gets or sets the value of this entry, stored as a JSON string.
    /// </summary>
    public string Value
    {
        get => this.value;
        set => this.value = value;
    }
}