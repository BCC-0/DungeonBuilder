using System;
using TMPro;
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
    private TMP_Text entityName;

    [SerializeField]
    private Image modeIndicator;

    [SerializeField]
    private Button removeButton;

    /// <summary>
    /// Sets the entity displayed by this button.
    /// </summary>
    /// <param name="entity">The entity to display.</param>
    public void SetEntity(SaveableEntity entity)
    {
        if (entity == null)
        {
            this.entityImage.sprite = null;
            this.entityName.text = string.Empty;
            return;
        }

        SpriteRenderer renderer =
            entity.GetComponentInChildren<SpriteRenderer>(true);

        this.entityImage.sprite =
            renderer != null ? renderer.sprite : null;

        this.entityName.text = entity.name;
    }

    /// <summary>
    /// Sets whether the entity is consumed.
    /// </summary>
    /// <param name="consumed">Whether the entity is consumed.</param>
    public void SetConsumed(bool consumed)
    {
        this.modeIndicator.gameObject.SetActive(consumed);
    }

    /// <summary>
    /// Sets the action that is called when the remove button is pressed.
    /// </summary>
    /// <param name="action">The action to call.</param>
    public void SetRemoveAction(Action action)
    {
        this.removeButton.onClick.RemoveAllListeners();

        if (action != null)
        {
            this.removeButton.onClick.AddListener(() => action());
        }
    }
}