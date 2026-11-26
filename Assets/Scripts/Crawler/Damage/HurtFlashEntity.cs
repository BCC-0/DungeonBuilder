using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for entities that can be hurt and flash when they take damage.
/// Only entities that should react to being damaged, such as the player and
/// enemies, extend this class. Walls, items and other saveable entities do
/// not get a hurt effect.
/// </summary>
public abstract class HurtFlashEntity : SaveableEntity
{
    private readonly List<SpriteRenderer> flashRenderers = new List<SpriteRenderer>();
    private readonly List<Color> baseColors = new List<Color>();

    [Header("Hurt Effect")]
    [SerializeField]
    private Color hurtColor = Color.red;

    [SerializeField]
    private float hurtFlashDuration = 0.15f;

    private Coroutine hurtFlashCoroutine;

    /// <summary>
    /// Plays the hurt effect by flashing all enabled child sprite renderers
    /// to the hurt color and back. When it is called again during a flash,
    /// the running flash is stopped and the colors are reset first, so
    /// repeated hits never keep a part of the tint.
    /// </summary>
    public void FlashHurtEffect()
    {
        if (this.hurtFlashCoroutine != null)
        {
            this.StopCoroutine(this.hurtFlashCoroutine);
            this.hurtFlashCoroutine = null;
            this.RestoreBaseColors();
        }

        // The colors are read while the entity still has its normal colors,
        // so an interrupted flash can never take its own tint for the original.
        this.CaptureBaseColors();

        this.hurtFlashCoroutine = this.StartCoroutine(this.HurtFlashRoutine());
    }

    /// <summary>
    /// Reads the colors of all enabled child sprite renderers, so the flash
    /// can always return to them.
    /// </summary>
    private void CaptureBaseColors()
    {
        this.flashRenderers.Clear();
        this.baseColors.Clear();

        foreach (SpriteRenderer renderer in this.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!renderer.enabled)
            {
                continue;
            }

            this.flashRenderers.Add(renderer);
            this.baseColors.Add(renderer.color);
        }
    }

    /// <summary>
    /// Returns all flashing sprite renderers to the colors they had before
    /// the flash started.
    /// </summary>
    private void RestoreBaseColors()
    {
        for (int i = 0; i < this.flashRenderers.Count; i++)
        {
            if (this.flashRenderers[i] != null)
            {
                this.flashRenderers[i].color = this.baseColors[i];
            }
        }
    }

    /// <summary>
    /// Tints all flashing sprite renderers, where zero is their own color and
    /// one is the hurt color.
    /// </summary>
    /// <param name="amount">How far to tint towards the hurt color.</param>
    private void SetTint(float amount)
    {
        for (int i = 0; i < this.flashRenderers.Count; i++)
        {
            if (this.flashRenderers[i] != null)
            {
                this.flashRenderers[i].color = Color.Lerp(
                    this.baseColors[i],
                    this.hurtColor,
                    amount);
            }
        }
    }

    /// <summary>
    /// Tweens the sprite renderers to the hurt color and back to the colors
    /// that were captured when the flash started.
    /// </summary>
    /// <returns>An enumerator for the hurt effect coroutine.</returns>
    private IEnumerator HurtFlashRoutine()
    {
        float halfDuration = Mathf.Max(0.01f, this.hurtFlashDuration / 2f);
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            this.SetTint(Mathf.Clamp01(elapsed / halfDuration));
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            this.SetTint(1f - Mathf.Clamp01(elapsed / halfDuration));
            yield return null;
        }

        this.RestoreBaseColors();
        this.hurtFlashCoroutine = null;
    }
}
