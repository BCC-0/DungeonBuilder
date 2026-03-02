using System;
using UnityEngine;

/// <summary>
/// Serializable wrapper for UnityEngine.Vector3 for binary saving.
/// </summary>
[Serializable]
public class SerializableVector3
{
    public float x, y, z;

    public SerializableVector3() { }

    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public Vector3 ToVector3() => new Vector3(x, y, z);
}
