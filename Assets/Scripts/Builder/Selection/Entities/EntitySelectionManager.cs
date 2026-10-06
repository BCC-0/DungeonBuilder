using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Selection manager for entities.
/// </summary>
public class EntitySelectionManager : SelectionManagerBase
{
    /// <summary>
    /// Max distance from a click position for an entity to be considered "under" it.
    /// </summary>
    [SerializeField]
    private float entitySelectRadius = 0.5f;

    [Header("Reference Overlay")]
    [SerializeField]
    private GameObject referenceOverlay;

    [SerializeField]
    private RectTransform referenceTextBlock;

    [SerializeField]
    private TextMeshProUGUI referenceOverlayText;

    [SerializeField]
    private float referenceOverlayFadeDuration = 0.25f;

    [SerializeField]
    private float referenceTextSlideDuration = 0.4f;

    [SerializeField]
    private float referenceTextAnimationInterval = 0.5f;

    [Tooltip("How far below the screen the text block starts before sliding in.")]
    [SerializeField]
    private float referenceTextStartOffset = 300f;

    private CanvasGroup referenceOverlayCanvasGroup;

    private Sequence referenceTextSequence;

    /// <summary>
    /// The position of the reference text block as configured in the scene.
    /// This position is used whenever the reference overlay is visible.
    /// </summary>
    private Vector2 referenceTextVisiblePosition;

    /// <summary>
    /// Visualizer that outlines the current entity selection.
    /// </summary>
    private EntitySelectionVisualizer selectionVisualizer;

    /// <summary>
    /// Cell positions of the selected entities when movement started.
    /// </summary>
    private Dictionary<SaveableEntity, Vector2Int> moveStartCells = new();

    /// <summary>
    /// Cell positions of the selected entities when movement is continued on mobile.
    /// </summary>
    private Dictionary<SaveableEntity, Vector2Int> extraMoveStartCells = new();

    /// <summary>
    /// Cell under the pointer when movement started.
    /// </summary>
    private Vector2Int moveStartPointerCell;

    /// <summary>
    /// Prevents confirming immediately on the same frame movement starts.
    /// </summary>
    private bool startedMovingThisFrame;

    /// <inheritdoc/>
    public override void DeleteSelected()
    {
        foreach (SaveableEntity entity in MapEditorManager.Instance.SelectedEntities)
        {
            if (entity != null)
            {
                Destroy(entity.gameObject);
            }
        }

        this.ClearSelection();
    }

    /// <inheritdoc/>
    public override void MoveSelected()
    {
        if (this.IsMovementMode)
        {
            return;
        }

        List<SaveableEntity> selectedEntities =
            MapEditorManager.Instance.SelectedEntities;

        if (selectedEntities == null || selectedEntities.Count == 0)
        {
            return;
        }

        this.startedMovingThisFrame = true;

        this.IsMovementMode = true;
        this.IsMoving = true;

        this.moveStartCells.Clear();

        this.moveStartPointerCell =
            (Vector2Int)this.Grid.WorldToCell(this.CurrentPos);

        foreach (SaveableEntity entity in selectedEntities)
        {
            Vector2Int cell =
                (Vector2Int)this.Grid.WorldToCell(entity.transform.position);

            this.moveStartCells[entity] = cell;
        }

        this.extraMoveStartCells =
            new Dictionary<SaveableEntity, Vector2Int>(
                this.moveStartCells);

        this.DisableSelectionButtons();

        this.EnableMoveButtons(
            this.GetBoundingRect(
                selectedEntities.Select(
                    e => (Vector2)e.transform.position)));
    }

    /// <summary>
    /// Updates the preview position of the selected entities based on the pointer position.
    /// </summary>
    public void Update()
    {
        if (!this.IsMovementMode || !this.IsMoving)
        {
            return;
        }

        if (this.startedMovingThisFrame)
        {
            this.startedMovingThisFrame = false;
            return;
        }

        Vector2Int currentPointerCell =
            (Vector2Int)this.Grid.WorldToCell(this.CurrentPos);

        Vector2Int cellDelta =
            currentPointerCell - this.moveStartPointerCell;

        foreach (KeyValuePair<SaveableEntity, Vector2Int> entry
            in this.extraMoveStartCells)
        {
            SaveableEntity entity = entry.Key;

            if (entity == null)
            {
                continue;
            }

            Vector3Int previewCell =
                (Vector3Int)(entry.Value + cellDelta);

            entity.transform.position =
                this.Grid.GetCellCenterWorld(previewCell);
        }

        this.selectionVisualizer.Refresh();

        if (RuntimePropertyEditor.Instance != null)
        {
            RuntimePropertyEditor.Instance.RefreshPosition();
        }
    }

    /// <inheritdoc/>
    public override void ConfirmMovingSelected()
    {
        if (this.startedMovingThisFrame)
        {
            this.startedMovingThisFrame = false;
            return;
        }

        if (!this.IsMovementMode)
        {
            return;
        }

        List<SaveableEntity> selectedEntities =
            MapEditorManager.Instance.SelectedEntities;

        HashSet<SaveableEntity> movingEntities =
            new HashSet<SaveableEntity>(selectedEntities);

        foreach (KeyValuePair<SaveableEntity, Vector2Int> entry
            in this.extraMoveStartCells)
        {
            SaveableEntity entity = entry.Key;

            if (entity == null)
            {
                continue;
            }

            Vector2Int finalCell =
                (Vector2Int)this.Grid.WorldToCell(
                    entity.transform.position);

            SaveableEntity entityAtDestination =
                FindObjectsByType<SaveableEntity>()
                    .FirstOrDefault(other =>
                        other != entity &&
                        !movingEntities.Contains(other) &&
                        !IsTilemapEntity(other) &&
                        (Vector2Int)this.Grid.WorldToCell(
                            other.transform.position) == finalCell);

            if (entityAtDestination != null)
            {
                Destroy(entityAtDestination.gameObject);
            }
        }

        this.IsMovementMode = false;
        this.IsMoving = false;
        this.moveStartCells.Clear();
        this.extraMoveStartCells.Clear();

        this.DisableMoveButtons();
        this.SetSelection(
            MapEditorManager.Instance.SelectedEntities);

        if (RuntimePropertyEditor.Instance != null)
        {
            RuntimePropertyEditor.Instance.Rebuild();
        }
    }

    /// <inheritdoc/>
    public override void CancelMovingSelected()
    {
        if (!this.IsMovementMode)
        {
            return;
        }

        foreach (KeyValuePair<SaveableEntity, Vector2Int> entry
            in this.moveStartCells)
        {
            SaveableEntity entity = entry.Key;

            if (entity == null)
            {
                continue;
            }

            entity.transform.position =
                this.Grid.GetCellCenterWorld(
                    (Vector3Int)entry.Value);
        }

        this.IsMovementMode = false;
        this.IsMoving = false;
        this.moveStartCells.Clear();
        this.extraMoveStartCells.Clear();

        this.DisableMoveButtons();
        this.SetSelection(
            MapEditorManager.Instance.SelectedEntities);

        if (RuntimePropertyEditor.Instance != null)
        {
            RuntimePropertyEditor.Instance.Rebuild();
        }
    }

    /// <inheritdoc/>
    protected override void ClearSelection()
    {
        this.SetSelection(new List<SaveableEntity>());
    }

    /// <inheritdoc/>
    protected override void OnReferenceSelectionStarted()
    {
        this.ShowReferenceOverlay();
    }

    /// <inheritdoc/>
    protected override void OnReferenceSelectionEnded()
    {
        this.HideReferenceOverlay();
        this.SetSelection(
            MapEditorManager.Instance.SelectedEntities);
    }

    /// <inheritdoc/>
    protected override List<SaveableEntity> PickEntitiesAt(
        Vector2 position)
    {
        SaveableEntity closestEntity =
            FindObjectsByType<SaveableEntity>()
                .Where(e =>
                    e.ExistsInWorld &&
                    !IsTilemapEntity(e))
                .OrderBy(e =>
                    Vector2.Distance(
                        e.transform.position,
                        position))
                .FirstOrDefault();

        if (closestEntity != null &&
            Vector2.Distance(
                closestEntity.transform.position,
                position) <= this.entitySelectRadius)
        {
            return new List<SaveableEntity>
            {
                closestEntity,
            };
        }

        return new List<SaveableEntity>();
    }

    /// <inheritdoc/>
    protected override List<SaveableEntity> PickEntitiesIn(
        Rect rect)
    {
        return FindObjectsByType<SaveableEntity>()
            .Where(e =>
                e.ExistsInWorld &&
                !IsTilemapEntity(e) &&
                rect.Contains(e.transform.position))
            .ToList();
    }

    /// <inheritdoc/>
    protected override void ContinueMove(Vector2 worldPos)
    {
        if (!this.IsMovementMode)
        {
            return;
        }

        this.moveStartPointerCell =
            (Vector2Int)this.Grid.WorldToCell(worldPos);

        foreach (SaveableEntity entity
            in this.extraMoveStartCells.Keys.ToList())
        {
            if (entity == null)
            {
                continue;
            }

            this.extraMoveStartCells[entity] =
                (Vector2Int)this.Grid.WorldToCell(
                    entity.transform.position);
        }

        this.CurrentPos = worldPos;
        this.startedMovingThisFrame = true;
        this.IsMoving = true;
    }

    /// <summary>
    /// Gets the current rect of the selection.
    /// </summary>
    /// <returns>A rect containing the selected entities.</returns>
    protected override Rect GetCurrentSelectionBounds()
    {
        List<SaveableEntity> entities =
            MapEditorManager.Instance.SelectedEntities;

        return this.GetBoundingRect(
            entities
                .Where(e => e != null)
                .Select(e => (Vector2)e.transform.position));
    }

    /// <summary>
    /// Selects the closest entity within range of the click, if any.
    /// </summary>
    /// <param name="position">The position that was clicked.</param>
    protected override void OnClickSelect(Vector2 position)
    {
        List<SaveableEntity> picked = this.PickEntitiesAt(position);

        if (picked.Count > 0)
        {
            this.SetSelection(picked);
        }
        else
        {
            this.ClearSelection();
        }
    }

    /// <summary>
    /// Selects every entity whose position falls within the dragged rectangle.
    /// </summary>
    /// <param name="rect">The world-space rectangle of the drag.</param>
    protected override void OnBoxSelect(Rect rect)
    {
        this.SetSelection(
            this.PickEntitiesIn(rect));
    }

    /// <inheritdoc/>
    protected override void Awake()
    {
        this.selectionVisualizer =
            FindAnyObjectByType<EntitySelectionVisualizer>();

        this.SetupReferenceOverlay();
        this.HideReferenceOverlay();

        base.Awake();
    }

    /// <summary>
    /// Whether the entity is the tilemap, which must never be selected or replaced
    /// like a normal entity.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity is a tilemap.</returns>
    private static bool IsTilemapEntity(SaveableEntity entity)
    {
        return entity.GetComponent<Tilemap>() != null;
    }

    /// <summary>
    /// Sets up the reference overlay references and initial state.
    /// The current RectTransform position is saved as the visible position.
    /// </summary>
    private void SetupReferenceOverlay()
    {
        this.referenceOverlayCanvasGroup =
            this.referenceOverlay.GetComponent<CanvasGroup>();

        if (this.referenceOverlayCanvasGroup == null)
        {
            this.referenceOverlayCanvasGroup =
                this.referenceOverlay.AddComponent<CanvasGroup>();
        }

        // Store the position configured in the Unity scene.
        // This becomes the position used whenever the overlay is visible.
        this.referenceTextVisiblePosition =
            this.referenceTextBlock.anchoredPosition;

        this.referenceOverlayCanvasGroup.alpha = 0f;
        this.referenceOverlay.SetActive(false);

        this.referenceTextBlock.anchoredPosition =
            this.GetReferenceTextHiddenPosition();

        this.referenceOverlayText.text =
            "Select entities";
    }

    /// <summary>
    /// Shows the reference-selection overlay and starts its animations.
    /// </summary>
    private void ShowReferenceOverlay()
    {
        this.referenceTextSequence?.Kill();

        this.referenceOverlay.SetActive(true);

        this.referenceOverlayCanvasGroup.DOKill();

        this.referenceOverlayCanvasGroup.alpha = 0f;

        this.referenceOverlayCanvasGroup
            .DOFade(
                1f,
                this.referenceOverlayFadeDuration)
            .SetEase(Ease.OutQuad);

        this.referenceTextBlock.DOKill();

        this.referenceTextBlock.anchoredPosition =
            this.GetReferenceTextHiddenPosition();

        this.referenceTextBlock
            .DOAnchorPos(
                this.GetReferenceTextVisiblePosition(),
                this.referenceTextSlideDuration)
            .SetEase(Ease.OutCubic);

        this.StartReferenceTextAnimation();
    }

    /// <summary>
    /// Hides the reference-selection overlay.
    /// </summary>
    private void HideReferenceOverlay()
    {
        this.referenceTextSequence?.Kill();
        this.referenceTextSequence = null;

        this.referenceTextBlock.DOKill();

        this.referenceTextBlock
            .DOAnchorPos(
                this.GetReferenceTextHiddenPosition(),
                this.referenceTextSlideDuration)
            .SetEase(Ease.InCubic);

        this.referenceOverlayCanvasGroup.DOKill();

        this.referenceOverlayCanvasGroup
            .DOFade(
                0f,
                this.referenceOverlayFadeDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                this.referenceOverlay.SetActive(false);
            });
    }

    /// <summary>
    /// Starts the repeating "Select entities..." text animation.
    /// </summary>
    private void StartReferenceTextAnimation()
    {
        if (this.referenceOverlayText == null)
        {
            return;
        }

        this.referenceTextSequence?.Kill();

        this.referenceOverlayText.text =
            "Select entities";

        this.referenceTextSequence =
            DOTween.Sequence();

        this.referenceTextSequence
            .AppendInterval(
                this.referenceTextAnimationInterval)
            .AppendCallback(() =>
            {
                this.referenceOverlayText.text =
                    "Select entities.";
            })
            .AppendInterval(
                this.referenceTextAnimationInterval)
            .AppendCallback(() =>
            {
                this.referenceOverlayText.text =
                    "Select entities..";
            })
            .AppendInterval(
                this.referenceTextAnimationInterval)
            .AppendCallback(() =>
            {
                this.referenceOverlayText.text =
                    "Select entities...";
            })
            .AppendInterval(
                this.referenceTextAnimationInterval)
            .AppendCallback(() =>
            {
                this.referenceOverlayText.text =
                    "Select entities";
            })
            .SetLoops(-1);
    }

    /// <summary>
    /// Gets the hidden anchored position of the reference text block.
    /// The text is moved below the visible area of the screen.
    /// </summary>
    private Vector2 GetReferenceTextHiddenPosition()
    {
        return new Vector2(this.referenceTextVisiblePosition.x, -this.referenceTextBlock.rect.height - this.referenceTextStartOffset);
    }

    /// <summary>
    /// Gets the visible anchored position of the reference text block.
    /// This is the position configured on the RectTransform in the scene.
    /// </summary>
    private Vector2 GetReferenceTextVisiblePosition()
    {
        return this.referenceTextVisiblePosition;
    }

    /// <summary>
    /// Writes the given selection to <see cref="MapEditorManager"/> and refreshes
    /// the visualizer.
    /// </summary>
    /// <param name="entities">The entities to select.</param>
    private void SetSelection(List<SaveableEntity> entities)
    {
        entities = entities?
            .Where(e =>
                e != null &&
                e.ExistsInWorld)
            .ToList()
            ?? new List<SaveableEntity>();

        MapEditorManager.Instance.SelectedEntities =
            entities;

        this.selectionVisualizer.Refresh();

        if (this.IsReferenceSelectionMode)
        {
            return;
        }

        if (entities == null || entities.Count == 0)
        {
            this.DisableSelectionButtons();
        }
        else
        {
            this.EnableSelectionButtons(
                this.GetBoundingRect(
                    entities.Select(
                        e => (Vector2)e.transform.position)));
        }
    }
}
