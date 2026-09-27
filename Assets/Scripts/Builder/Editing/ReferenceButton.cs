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
    private Sprite sharedIcon;

    [SerializeField]
    private Sprite consumingIcon;

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
    /// Sets the visual indicator for the reference mode.
    /// </summary>
    /// <param name="mode">The reference mode.</param>
    public void SetReferenceMode(ReferenceMode mode)
    {
        this.modeIndicator.sprite =
            mode == ReferenceMode.Consuming
                ? this.consumingIcon
                : this.sharedIcon;

        this.modeIndicator.gameObject.SetActive(true);
    }
}