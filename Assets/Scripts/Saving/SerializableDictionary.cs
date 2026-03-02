using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity-serializable dictionary storing string key/value pairs.
/// Values are stored as JSON strings.
/// </summary>
[Serializable]
public class SerializableDictionary
{
    [SerializeField]
    private List<SerializableKeyValue> items =
        new List<SerializableKeyValue>();

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializableDictionary"/> class.
    /// </summary>
    public SerializableDictionary()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializableDictionary"/> class
    /// from a dictionary.
    /// </summary>
    /// <param name="dictionary">Dictionary to copy.</param>
    public SerializableDictionary(Dictionary<string, string> dictionary)
    {
        foreach (var pair in dictionary)
        {
            this.items.Add(new SerializableKeyValue
            {
                Key = pair.Key,
                Value = pair.Value,
            });
        }
    }

    /// <summary>
    /// Converts this serializable dictionary back into a normal dictionary.
    /// </summary>
    /// <returns>A dictionary containing all stored values.</returns>
    public Dictionary<string, string> ToDictionary()
    {
        Dictionary<string, string> dictionary =
            new Dictionary<string, string>();

        foreach (var item in this.items)
        {
            dictionary[item.Key] = item.Value;
        }

        return dictionary;
    }
}