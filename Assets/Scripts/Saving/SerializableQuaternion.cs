using System;
using UnityEngine;

/// <summary>
/// Serializable wrapper for UnityEngine.Quaternion for binary saving.
/// </summary>
[Serializable]
public class SerializableQuaternion
{
    public float x, y, z, w;

    public SerializableQuaternion() { }

    public SerializableQuaternion(Quaternion q)
    {
        x = q.x;
        y = q.y;
        z = q.z;
        w = q.w;
    }

    public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);
}