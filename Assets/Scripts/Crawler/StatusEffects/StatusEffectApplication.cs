using System;
using UnityEngine;

/// <summary>
/// A status effect together with the duration it is applied with. Weapons,
/// damage sources and potions use this, so the same effect can be applied for
/// a short time by one source and for a long time by another.
/// </summary>
[Serializable]
public struct StatusEffectApplication
{
    [SerializeField]
    private StatusEffectDefinition definition;

    [SerializeField]
    [Min(0f)]
    [Tooltip("Overrides the duration of the effect. Zero uses the duration of the definition.")]
    private float duration;

    /// <summary>
    /// Gets the effect this application applies.
    /// </summary>
    public StatusEffectDefinition Definition => this.definition;

    /// <summary>
    /// Gets the duration in seconds, or zero to use the duration of the definition.
    /// </summary>
    public float Duration => this.duration;
}
