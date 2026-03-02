using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Unity-serializable wrapper for a dictionary of objects.
/// Supports primitives, strings, Vector3, Quaternion, lists of strings, and TransformData.
/// </summary>
[Serializable]
public class SerializableDictionary
{
    [SerializeField]
    private List<SerializableKeyValue> items = new List<SerializableKeyValue>();

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializableDictionary"/> class.
    /// Empty constructor.
    /// </summary>
    public SerializableDictionary()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializableDictionary"/> class
    /// from a standard dictionary.
    /// </summary>
    /// <param name="dict">The dictionary to serialize.</param>
    public SerializableDictionary(Dictionary<string, object> dict)
    {
        foreach (var kv in dict)
        {
            this.items.Add(new SerializableKeyValue
            {
                Key = kv.Key,
                Value = this.SerializeValue(kv.Value),
            });
        }
    }

    /// <summary>
    /// Gets the list of key/value pairs representing the dictionary.
    /// Each value is serialized as a JSON string.
    /// </summary>
    public List<SerializableKeyValue> Items => this.items;

    /// <summary>
    /// Converts the serializable dictionary back to a standard dictionary.
    /// </summary>
    /// <returns>A dictionary with deserialized values.</returns>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>();
        foreach (var kv in this.items)
        {
            dict[kv.Key] = this.DeserializeValue(kv.Value);
        }

        return dict;
    }

    /// <summary>
    /// Serializes supported object types to JSON strings.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>JSON string representation of the value.</returns>
    private string SerializeValue(object value)
    {
        if (value == null)
        {
            return null;
        }

        Type t = value.GetType();

        if (t == typeof(int) || t == typeof(float) || t == typeof(bool) || t == typeof(string))
        {
            return JsonUtility.ToJson(new PrimitiveWrapper(value));
        }

        if (t == typeof(Vector3) || t == typeof(Quaternion))
        {
            return JsonUtility.ToJson(value);
        }

        if (t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(List<>) &&
            t.GetGenericArguments()[0] == typeof(string))
        {
            return JsonUtility.ToJson(new StringListWrapper((List<string>)value));
        }

        if (t.Name == "TransformData")
        {
            return JsonUtility.ToJson(value);
        }

        // Fallback
        return JsonUtility.ToJson(value);
    }

    /// <summary>
    /// Deserializes a JSON string back to the original object.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    private object DeserializeValue(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        // Try primitive
        try
        {
            var prim = JsonUtility.FromJson<PrimitiveWrapper>(json);
            if (prim != null)
            {
                return prim.GetValue();
            }
        }
        catch
        {
            // Ignore
        }

        // Try list of strings
        try
        {
            var list = JsonUtility.FromJson<StringListWrapper>(json);
            if (list != null)
            {
                return list.Items;
            }
        }
        catch
        {
            // Ignore
        }

        // Fallback: return JSON string itself
        return json;
    }

    /// <summary>
    /// Wraps primitive types (int, float, bool, string) for JSON serialization.
    /// </summary>
    [Serializable]
    private class PrimitiveWrapper
    {
        [SerializeField]
        private string type;

        [SerializeField]
        private string value;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveWrapper"/> class.
        /// Default constructor.
        /// </summary>
        public PrimitiveWrapper()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveWrapper"/> class
        /// from an object.
        /// </summary>
        /// <param name="val">The object to wrap.</param>
        public PrimitiveWrapper(object val)
        {
            if (val is int i)
            {
                this.type = "int";
                this.value = i.ToString();
            }
            else if (val is float f)
            {
                this.type = "float";
                this.value = f.ToString();
            }
            else if (val is bool b)
            {
                this.type = "bool";
                this.value = b.ToString();
            }
            else if (val is string s)
            {
                this.type = "string";
                this.value = s;
            }
        }

        /// <summary>
        /// Gets the original value as an object.
        /// </summary>
        /// <returns>The deserialized object.</returns>
        public object GetValue()
        {
            return this.type switch
            {
                "int" => int.Parse(this.value),
                "float" => float.Parse(this.value),
                "bool" => bool.Parse(this.value),
                "string" => this.value,
                _ => this.value
            };
        }
    }

    /// <summary>
    /// Wraps a list of strings for JSON serialization.
    /// </summary>
    [Serializable]
    private class StringListWrapper
    {
        [SerializeField]
        private List<string> items;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringListWrapper"/> class.
        /// Default constructor.
        /// </summary>
        public StringListWrapper()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringListWrapper"/> class
        /// from a list of strings.
        /// </summary>
        /// <param name="items">The list of strings to wrap.</param>
        public StringListWrapper(List<string> items)
        {
            this.items = items;
        }

        /// <summary>
        /// Gets the list of strings.
        /// </summary>
        public List<string> Items => this.items;
    }
}