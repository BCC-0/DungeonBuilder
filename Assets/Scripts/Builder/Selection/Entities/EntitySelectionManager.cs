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
    private SpriteFontText referenceOverlayText;

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

    private Vector2 referenceTextVisiblePosition;

    private EntitySelectionVisualizer selectionVisualizer;

    private Dictionary<SaveableEntity, Vector2Int> moveStartCells = new ();

    private Dictionary<SaveableEntity, Vector3> moveStartPositions = new ();

    private Dictionary<SaveableEntity, Vector2Int> extraMoveStartCells = new ();

    private UndoRedoManager undoRedoManager;

    /// <summary>
    /// Cell under the pointer when movement started.
    /// </summary>
    private Vector2Int moveStartPointerCell;

    /// <summary>
    /// Prevents confirming immediately on the same frame movement starts.
    /// </summary>
    private bool startedMovingThisFrame;

    /// <inheritdoc/>
    protected override bool HasSelection =>
        MapEditorManager.Instance.SelectedEntities != null &&
        MapEditorManager.Instance.SelectedEntities.Count > 0;

    /// <inheritdoc/>
    public override void DeleteSelected()
    {
        if (this.IsReferenceSelectionMode)
        {
            return;
        }

        List<SaveableEntity> toDelete =
            MapEditorManager.Instance.SelectedEntities
                .Where(e => e != null)
                .ToList();

        if (toDelete.Count > 0)
        {
            MapEditorManager.Instance.RegisterAction(
                EntityPresenceAction.DeleteNow(toDelete));
        }

        this.ClearSelection();
    }

    /// <inheritdoc/>
    public override void MoveSelected()
    {
        if (this.IsMovementMode || this.IsReferenceSelectionMode)
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

        this.moveStartPositions.Clear();

        foreach (SaveableEntity entity in selectedEntities)
        {
            Vector2Int cell =
                (Vector2Int)this.Grid.WorldToCell(entity.transform.position);

            this.moveStartCells[entity] = cell;
            this.moveStartPositions[entity] = entity.transform.position;
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
                this.GetEntityWorldPosition(previewCell);
        }

        foreach (SaveableEntity entity in this.extraMoveStartCells.Keys)
        {
            if (entity != null)
            {
                this.RefreshBuilderWall(entity);
            }
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

        List<SaveableEntity> replaced = new List<SaveableEntity>();
        MoveEntitiesAction move = new MoveEntitiesAction();

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
                        other.ExistsInWorld &&
                        (Vector2Int)this.Grid.WorldToCell(
                            other.transform.position) == finalCell);

            if (entityAtDestination != null &&
                !replaced.Contains(entityAtDestination))
            {
                replaced.Add(entityAtDestination);
            }

            if (this.moveStartPositions.TryGetValue(
                    entity,
                    out Vector3 startPosition))
            {
                move.Add(entity, startPosition, entity.transform.position);
            }
        }

        // Registered in this order, so undo moves the entities back first
        // and then brings the replaced entities back.
        MapEditorManager manager = MapEditorManager.Instance;
        manager.BeginTransaction();

        if (replaced.Count > 0)
        {
            manager.RegisterAction(EntityPresenceAction.DeleteNow(replaced));
        }

        if (move.HasChanges)
        {
            manager.RegisterAction(move);
        }

        manager.EndTransaction();

        this.IsMovementMode = false;
        this.IsMoving = false;
        this.moveStartCells.Clear();
        this.moveStartPositions.Clear();
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
                this.GetEntityWorldPosition(
                    (Vector3Int)entry.Value);
        }

        foreach (SaveableEntity entity in this.moveStartCells.Keys)
        {
            if (entity != null)
            {
                this.RefreshBuilderWall(entity);
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

    /// <summary>
    /// Re-applies the selection stored in <see cref="MapEditorManager"/> so the
    /// visualizer and selection buttons match it. Used after the selection was
    /// changed from outside, for example by reference navigation.
    /// </summary>
    public void RefreshSelection()
    {
        this.SetSelection(MapEditorManager.Instance.SelectedEntities);
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

    /// <summary>
    /// Selects the entity whose visible sprite is under the click. When no sprite
    /// contains the click, the closest entity within <see cref="entitySelectRadius"/>
    /// is still selected. Overlapping sprites are resolved by draw order.
    /// </summary>
    /// <param name="position">The position that was clicked.</param>
    /// <returns>The picked entity, or an empty list when nothing is under the click.</returns>
    protected override List<SaveableEntity> PickEntitiesAt(Vector2 position)
    {
        SaveableEntity closestEntity = null;
        bool closestIsInsideSprite = false;
        int closestSortingLayer = default(int);
        int closestSortingOrder = default(int);
        float closestDistance = float.MaxValue;

        foreach (SaveableEntity entity in FindObjectsByType<SaveableEntity>())
        {
            if (!entity.ExistsInWorld ||
                IsTilemapEntity(entity))
            {
                continue;
            }

            bool isInsideSprite =
                TryGetTopmostSpriteAt(
                    entity,
                    position,
                    out SpriteRenderer hitRenderer);

            float distance =
                Vector2.Distance(
                    GetEntityVisualCenter(entity),
                    position);

            if (!isInsideSprite &&
                distance > this.entitySelectRadius)
            {
                continue;
            }

            int sortingLayer =
                hitRenderer != null
                    ? hitRenderer.sortingLayerID
                    : default(int);

            int sortingOrder =
                hitRenderer != null
                    ? hitRenderer.sortingOrder
                    : default(int);

            if (closestEntity != null)
            {
                if (isInsideSprite != closestIsInsideSprite)
                {
                    if (!isInsideSprite)
                    {
                        continue;
                    }
                }
                else if (sortingLayer != closestSortingLayer)
                {
                    if (sortingLayer < closestSortingLayer)
                    {
                        continue;
                    }
                }
                else if (sortingOrder != closestSortingOrder)
                {
                    if (sortingOrder < closestSortingOrder)
                    {
                        continue;
                    }
                }
                else if (distance >= closestDistance)
                {
                    continue;
                }
            }

            closestEntity = entity;
            closestIsInsideSprite = isInsideSprite;
            closestSortingLayer = sortingLayer;
            closestSortingOrder = sortingOrder;
            closestDistance = distance;
        }

        if (closestEntity == null)
        {
            return new List<SaveableEntity>();
        }

        return new List<SaveableEntity> { closestEntity, };
    }

    /// <summary>
    /// Selects every entity whose visible sprite overlaps the dragged rectangle.
    /// Entities without a visible sprite are still picked by their position.
    /// </summary>
    /// <param name="rect">The world-space rectangle of the drag.</param>
    /// <returns>The entities inside the rectangle.</returns>
    protected override List<SaveableEntity> PickEntitiesIn(
        Rect rect)
    {
        List<SaveableEntity> picked = new List<SaveableEntity>();

        foreach (SaveableEntity entity in FindObjectsByType<SaveableEntity>())
        {
            if (!entity.ExistsInWorld ||
                IsTilemapEntity(entity))
            {
                continue;
            }

            if (rect.Contains(entity.transform.position) ||
                HasSpriteOverlappingRect(entity, rect))
            {
                picked.Add(entity);
            }
        }

        return picked;
    }

    /// <inheritdoc/>
    protected override bool HasSelectableAt(Vector2 position)
    {
        return this.PickEntitiesAt(position).Count > 0;
    }

    /// <inheritdoc/>
    protected override void ContinueMove(Vector2 worldPos)
    {
        if (!this.IsMovementMode)
        {
            return;
        }

        this.moveStartPointerCell = (Vector2Int)this.Grid.WorldToCell(worldPos);

        foreach (SaveableEntity entity
            in this.extraMoveStartCells.Keys.ToList())
        {
            if (entity == null)
            {
                continue;
            }

            this.extraMoveStartCells[entity] = (Vector2Int)this.Grid.WorldToCell(entity.transform.position);
        }

        this.CurrentPos = worldPos;
        this.startedMovingThisFrame = true;
        this.IsMoving = true;
    }

    /// <summary>
    /// Gets the current rect of the selection. Consumed entities have no
    /// world position of their own and are ignored.
    /// </summary>
    /// <returns>A rect containing the selected entities that exist in the world.</returns>
    protected override Rect GetCurrentSelectionBounds()
    {
        List<Vector2> positions =
            MapEditorManager.Instance.SelectedEntities
                .Where(e => e != null && e.ConsumedBy == null)
                .Select(e => (Vector2)e.transform.position)
                .ToList();

        return positions.Count > 0
            ? this.GetBoundingRect(positions)
            : default(Rect);
    }

    /// <summary>
    /// Selects the closest entity within range of the click, if any.
    /// When that entity is already the only selected entity, the selection is cleared instead.
    /// </summary>
    /// <param name="position">The position that was clicked.</param>
    protected override void OnClickSelect(Vector2 position)
    {
        List<SaveableEntity> picked = this.PickEntitiesAt(position);

        if (picked.Count == 0)
        {
            this.ClearSelection();
            return;
        }

        List<SaveableEntity> current =
            MapEditorManager.Instance.SelectedEntities;

        if (current != null &&
            current.Count == 1 &&
            current[0] == picked[0])
        {
            this.ClearSelection();
            return;
        }

        this.SetSelection(picked);
    }

    /// <summary>
    /// Selects every entity whose position falls within the dragged rectangle.
    /// </summary>
    /// <param name="rect">The world-space rectangle of the drag.</param>
    protected override void OnBoxSelect(Rect rect)
    {
        this.SetSelection(this.PickEntitiesIn(rect));
    }

    /// <inheritdoc/>
    protected override void Awake()
    {
        this.selectionVisualizer =
            FindAnyObjectByType<EntitySelectionVisualizer>();

        this.SetupReferenceOverlay();
        this.HideReferenceOverlay();

        base.Awake();

        this.undoRedoManager =
            FindAnyObjectByType<UndoRedoManager>();

        if (this.undoRedoManager != null)
        {
            this.undoRedoManager.HistoryChanged +=
                this.OnHistoryChanged;
        }
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
    /// Gets the visual center of an entity for selection.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The center of its rendered sprite, or its transform position if no renderer exists.</returns>
    private static Vector2 GetEntityVisualCenter(SaveableEntity entity)
    {
        foreach (SpriteRenderer spriteRenderer in
                 entity.GetComponentsInChildren<SpriteRenderer>())
        {
            if (IsSelectableSprite(spriteRenderer))
            {
                return spriteRenderer.bounds.center;
            }
        }

        return entity.transform.position;
    }

    /// <summary>
    /// Finds the topmost visible sprite of an entity that contains a world position.
    /// Selection outlines and renderers without a sprite are ignored, so they do
    /// not widen the clickable area.
    /// </summary>
    /// <param name="entity">The entity to test.</param>
    /// <param name="position">The world position to test.</param>
    /// <param name="hitRenderer">The topmost sprite under the position, or null when there is none.</param>
    /// <returns>True when one of the entity's visible sprites contains the position.</returns>
    private static bool TryGetTopmostSpriteAt(
        SaveableEntity entity,
        Vector2 position,
        out SpriteRenderer hitRenderer)
    {
        hitRenderer = null;

        foreach (SpriteRenderer renderer in
                 entity.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!IsSelectableSprite(renderer) ||
                !ContainsWorldPoint(renderer.bounds, position))
            {
                continue;
            }

            if (hitRenderer != null &&
                (hitRenderer.sortingLayerID > renderer.sortingLayerID ||
                 (hitRenderer.sortingLayerID == renderer.sortingLayerID &&
                  hitRenderer.sortingOrder > renderer.sortingOrder)))
            {
                continue;
            }

            hitRenderer = renderer;
        }

        return hitRenderer != null;
    }

    /// <summary>
    /// Checks whether any visible sprite of an entity overlaps a world-space rectangle.
    /// </summary>
    /// <param name="entity">The entity to test.</param>
    /// <param name="rect">The world-space rectangle to test.</param>
    /// <returns>True when one of the entity's visible sprites overlaps the rectangle.</returns>
    private static bool HasSpriteOverlappingRect(
        SaveableEntity entity,
        Rect rect)
    {
        foreach (SpriteRenderer renderer in
                 entity.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!IsSelectableSprite(renderer))
            {
                continue;
            }

            Bounds bounds = renderer.bounds;

            if (rect.Overlaps(
                    new Rect(
                        bounds.min.x,
                        bounds.min.y,
                        bounds.size.x,
                        bounds.size.y)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a sprite renderer can be used for selecting its entity.
    /// Selection outlines and renderers without a sprite are ignored.
    /// </summary>
    /// <param name="renderer">The renderer to check.</param>
    /// <returns>True when the renderer is enabled and shows a sprite that is not a selection outline.</returns>
    private static bool IsSelectableSprite(SpriteRenderer renderer)
    {
        return renderer.enabled &&
            renderer.sprite != null &&
            renderer.GetComponent<EntitySelectionOutline>() == null;
    }

    /// <summary>
    /// Checks whether a world position lies inside sprite bounds, ignoring the
    /// bounds' depth because 2D sprites have no thickness.
    /// </summary>
    /// <param name="bounds">The sprite bounds.</param>
    /// <param name="position">The world position to test.</param>
    /// <returns>True when the position is inside the bounds on both axes.</returns>
    private static bool ContainsWorldPoint(Bounds bounds, Vector2 position)
    {
        return position.x >= bounds.min.x &&
            position.x <= bounds.max.x &&
            position.y >= bounds.min.y &&
            position.y <= bounds.max.y;
    }

    /// <summary>
    /// Converts a grid cell to the entity's world-position convention.
    /// Entities are positioned at the cell's integer coordinates,
    /// with a half-cell horizontal offset to center the sprite.
    /// </summary>
    /// <param name="cell">The grid cell.</param>
    /// <returns>The entity's world position.</returns>
    private Vector3 GetEntityWorldPosition(Vector3Int cell)
    {
        Vector3 cellPosition = this.Grid.CellToWorld(cell);

        return new Vector3(
            cellPosition.x + 0.5f,
            cellPosition.y,
            cellPosition.z);
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

        this.referenceOverlayText.SetText("Select entities");
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

        this.referenceOverlayText.SetText("Select entities");

        this.referenceTextSequence =
            DOTween.Sequence();

        this.referenceTextSequence
            .AppendInterval(
                this.referenceTextAnimationInterval)
            .AppendCallback(() =>
            {
                this.referenceOverlayText.SetText("Select entities.");
            })
            .AppendInterval(
                this.referenceTextAnimationInterval)
            .AppendCallback(() =>
            {
                this.referenceOverlayText.SetText("Select entities..");
            })
            .AppendInterval(
                this.referenceTextAnimationInterval)
            .AppendCallback(() =>
            {
                this.referenceOverlayText.SetText("Select entities...");
            })
            .AppendInterval(
                this.referenceTextAnimationInterval)
            .AppendCallback(() =>
            {
                this.referenceOverlayText.SetText("Select entities");
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
    /// the visualizer. Consumed entities stay selected so their inspector remains open.
    /// </summary>
    /// <param name="entities">The entities to select.</param>
    private void SetSelection(List<SaveableEntity> entities)
    {
        entities = entities?
            .Where(e =>
                e != null &&
                (e.ExistsInWorld || e.ConsumedBy != null))
            .ToList()
            ?? new List<SaveableEntity>();

        MapEditorManager.Instance.SelectedEntities =
            entities;

        this.selectionVisualizer.Refresh();

        if (this.IsReferenceSelectionMode)
        {
            return;
        }

        if (entities.Count == 0 ||
            entities.Any(e => e.ConsumedBy != null))
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

    private void RefreshBuilderWall(SaveableEntity entity)
    {
        BuilderEntity builderEntity =
            entity.GetComponent<BuilderEntity>();

        if (builderEntity != null)
        {
            builderEntity.RefreshWallPosition();
        }
    }

    private void OnDestroy()
    {
        if (this.undoRedoManager != null)
        {
            this.undoRedoManager.HistoryChanged -=
                this.OnHistoryChanged;
        }
    }

    private void OnHistoryChanged()
    {
        if (this.IsMovementMode ||
            this.IsReferenceSelectionMode)
        {
            return;
        }

        this.RefreshSelection();

        if (RuntimePropertyEditor.Instance != null)
        {
            RuntimePropertyEditor.Instance.RefreshPosition();
        }
    }
}
