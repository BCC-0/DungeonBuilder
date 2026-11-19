using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the visual state of a reference button.
/// </summary>
public class ReferenceButton : MonoBehaviour
{
    [SerializeField]
    private Image entityImage;

    [SerializeField]
    private Image modeIndicator;

    [SerializeField]
    private Button removeButton;

    private SaveableEntity entity;
    private AspectRatioFitter aspectRatioFitter;

    /// <summary>
    /// Sets the entity displayed and navigated to by this button.
    /// </summary>
    public void SetEntity(SaveableEntity entity)
    {
        this.entity = entity;

        if (entity == null)
        {
            this.entityImage.sprite = null;
            return;
        }

        SpriteRenderer renderer =
            entity.GetComponentInChildren<SpriteRenderer>(true);

        Sprite sprite =
            renderer != null ? renderer.sprite : null;

        this.entityImage.sprite = sprite;
        this.entityImage.preserveAspect = true;
        this.UpdateAspectRatio(sprite);
    }

    /// <summary>
    /// Navigates to the entity represented by this reference button.
    /// Assign this method to the Button's OnClick event in the Inspector.
    /// </summary>
    public void NavigateToEntity()
    {
        Debug.Log("A");
        if (this.entity == null)
        {
            Debug.Log("Entity = null");
            return;
        }

        if (RuntimePropertyEditor.Instance != null)
        {
            RuntimePropertyEditor.Instance.NavigateToReference(this.entity);
        }
    }

    /// <summary>
    /// Sets whether the entity is consumed.
    /// </summary>
    public void SetConsumed(bool consumed)
    {
        if (this.modeIndicator != null)
        {
            this.modeIndicator.gameObject.SetActive(consumed);
        }
    }

    /// <summary>
    /// Sets the action called when the remove button is pressed.
    /// </summary>
    public void SetRemoveAction(Action action)
    {
        if (this.removeButton == null)
        {
            return;
        }

        this.removeButton.onClick.RemoveAllListeners();

        if (action != null)
        {
            this.removeButton.onClick.AddListener(() => action());
        }
    }

    /// <summary>
    /// Keeps the image inside its parent area while preserving the ratio
    /// of the sprite.
    /// </summary>
    private void UpdateAspectRatio(Sprite sprite)
    {
        if (this.entityImage == null)
        {
            return;
        }

        if (this.aspectRatioFitter == null)
        {
            this.aspectRatioFitter =
                this.entityImage.GetComponent<AspectRatioFitter>();

            if (this.aspectRatioFitter == null)
            {
                this.aspectRatioFitter =
                    this.entityImage.gameObject.AddComponent<AspectRatioFitter>();
            }
        }

        this.aspectRatioFitter.aspectMode =
            AspectRatioFitter.AspectMode.FitInParent;

        if (sprite == null || sprite.rect.height <= 0f)
        {
            this.aspectRatioFitter.aspectRatio = 1f;
            return;
        }

        this.aspectRatioFitter.aspectRatio =
            sprite.rect.width / sprite.rect.height;
    }
}