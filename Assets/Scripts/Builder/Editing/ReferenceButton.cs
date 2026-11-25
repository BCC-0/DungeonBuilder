using System;
using System.Collections.Generic;
using System.Reflection;
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
    private IReadOnlyList<SaveableEntity> referenceTargets;
    private AspectRatioFitter aspectRatioFitter;
    private Image wallOverlayImage;
    private Image headImage;

    /// <summary>
    /// Sets the entity displayed and navigated to by this button.
    /// </summary>
    /// <param name="entity">The entity to display.</param>
    public void SetEntity(SaveableEntity entity)
    {
        this.entity = entity;

        this.CreateOverlayImages();
        this.HideOverlay(this.wallOverlayImage);
        this.HideOverlay(this.headImage);

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

        this.UpdateWallImage(entity);
        this.UpdateHeadImage(entity, sprite);
    }

    /// <summary>
    /// Sets the entities that are selected when this button is clicked.
    /// </summary>
    /// <param name="targets">The referenced entities to navigate to.</param>
    public void SetReferenceTargets(IReadOnlyList<SaveableEntity> targets)
    {
        this.referenceTargets = targets;
    }

    /// <summary>
    /// Navigates to the entities represented by this reference button. When the
    /// button stands for the references of several selected entities, all of them
    /// are selected so they can be edited together.
    /// Assign this method to the Button's OnClick event in the Inspector.
    /// </summary>
    public void NavigateToEntity()
    {
        if (this.referenceTargets != null &&
            this.referenceTargets.Count > 1)
        {
            if (RuntimePropertyEditor.Instance != null)
            {
                RuntimePropertyEditor.Instance.NavigateToReferences(
                    this.referenceTargets);
            }

            return;
        }

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
    /// <param name="consumed">Whether the entity is consumed.</param>
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
    /// <param name="action">The action to invoke when the remove button is pressed.</param>
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
    /// Gets all instance fields declared on a type and its base types.
    /// </summary>
    /// <param name="type">The type whose fields should be retrieved.</param>
    /// <returns>All instance fields declared on the type and its base types.</returns>
    private static List<FieldInfo> GetAllFields(Type type)
    {
        List<FieldInfo> fields = new List<FieldInfo>();

        while (type != null)
        {
            fields.AddRange(
                type.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly));

            type = type.BaseType;
        }

        return fields;
    }

    /// <summary>
    /// Displays the consumed enemy head when the entity is an enemy body or a builder entity holding one.
    /// </summary>
    /// <param name="entity">The entity whose consumed head should be displayed.</param>
    /// <param name="bodySprite">The sprite of the entity body.</param>
    private void UpdateHeadImage(SaveableEntity entity, Sprite bodySprite)
    {
        if (bodySprite == null)
        {
            return;
        }

        SaveableEntity head = null;
        Vector3 headDownPosition = Vector3.zero;

        if (entity is BuilderEntity builderEntity)
        {
            head = builderEntity.GetConsumedHead();

            FieldInfo positionField =
                typeof(BuilderEntity).GetField(
                    "headDownPosition",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (positionField != null)
            {
                headDownPosition =
                    (Vector3)positionField.GetValue(builderEntity);
            }
        }
        else if (entity is EnemyBody)
        {
            FieldInfo positionField =
                typeof(EnemyBody).GetField(
                    "headDownPosition",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (positionField != null)
            {
                headDownPosition =
                    (Vector3)positionField.GetValue(entity);
            }

            foreach (FieldInfo field in GetAllFields(entity.GetType()))
            {
                if (!typeof(EnemyHead).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                SaveFieldAttribute attribute =
                    field.GetCustomAttribute<SaveFieldAttribute>();

                if (attribute == null ||
                    attribute.ReferenceMode != ReferenceMode.Consuming)
                {
                    continue;
                }

                head = field.GetValue(entity) as SaveableEntity;

                if (head != null)
                {
                    break;
                }
            }
        }

        if (head == null)
        {
            return;
        }

        SpriteRenderer headRenderer =
            head.GetComponentInChildren<SpriteRenderer>(true);

        if (headRenderer == null || headRenderer.sprite == null)
        {
            return;
        }

        Bounds bodyBounds = bodySprite.bounds;
        Bounds headBounds = headRenderer.sprite.bounds;

        if (bodyBounds.size.x <= 0f || bodyBounds.size.y <= 0f)
        {
            return;
        }

        Vector2 normalizedCenter = new Vector2(
            (headDownPosition.x + headBounds.center.x - bodyBounds.min.x) / bodyBounds.size.x,
            (headDownPosition.y + headBounds.center.y - bodyBounds.min.y) / bodyBounds.size.y);

        Vector2 halfSize = new Vector2(
            headBounds.size.x / bodyBounds.size.x * 0.5f,
            headBounds.size.y / bodyBounds.size.y * 0.5f);

        this.headImage.sprite = headRenderer.sprite;
        this.headImage.color = headRenderer.color;
        this.headImage.preserveAspect = true;

        RectTransform headRect = this.headImage.rectTransform;
        headRect.anchorMin = normalizedCenter - halfSize;
        headRect.anchorMax = normalizedCenter + halfSize;
        headRect.offsetMin = Vector2.zero;
        headRect.offsetMax = Vector2.zero;

        this.headImage.gameObject.SetActive(true);
    }

    /// <summary>
    /// Keeps the image inside its parent area while preserving the ratio
    /// of the sprite.
    /// </summary>
    /// <param name="sprite">The sprite whose aspect ratio should be applied.</param>
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

    /// <summary>
    /// Creates the additional UI images used to display entity overlays.
    /// </summary>
    private void CreateOverlayImages()
    {
        if (this.wallOverlayImage == null)
        {
            this.wallOverlayImage = this.CreateOverlayImage("WallOverlayImage");
        }

        if (this.headImage == null)
        {
            this.headImage = this.CreateOverlayImage("HeadImage");
        }
    }

    /// <summary>
    /// Creates a child image of the entity image that fills it.
    /// </summary>
    /// <param name="imageName">The name of the image object to create.</param>
    /// <returns>The created UI image.</returns>
    private Image CreateOverlayImage(string imageName)
    {
        GameObject imageObject = new GameObject(
            imageName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        imageObject.transform.SetParent(this.entityImage.transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = imageObject.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;

        return image;
    }

    /// <summary>
    /// Clears and disables an overlay image so it is not drawn as a blank square.
    /// </summary>
    /// <param name="overlay">The overlay image to hide.</param>
    private void HideOverlay(Image overlay)
    {
        overlay.sprite = null;
        overlay.gameObject.SetActive(false);
    }

    /// <summary>
    /// Displays the wall overlay, stretched to the size of the wall, when the entity is a wall or a builder entity holding one.
    /// </summary>
    /// <param name="entity">The entity whose wall overlay should be displayed.</param>
    private void UpdateWallImage(SaveableEntity entity)
    {
        SpriteRenderer overlayRenderer = null;

        if (entity is Wall wall)
        {
            overlayRenderer = wall.OverlayRenderer;
        }
        else if (entity is BuilderEntity)
        {
            FieldInfo overlayField =
                entity.GetType().GetField(
                    "wallOverlayRenderer",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (overlayField != null)
            {
                overlayRenderer =
                    overlayField.GetValue(entity) as SpriteRenderer;
            }
        }

        if (overlayRenderer == null || overlayRenderer.sprite == null)
        {
            return;
        }

        this.wallOverlayImage.sprite = overlayRenderer.sprite;
        this.wallOverlayImage.color = overlayRenderer.color;
        this.wallOverlayImage.preserveAspect = false;
        this.wallOverlayImage.gameObject.SetActive(true);
    }
}