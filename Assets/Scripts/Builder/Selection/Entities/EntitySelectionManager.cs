using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Seconds of loading work one frame may do before it yields, so the loading
    /// overlay keeps drawing while a large selection is built.
    /// </summary>
    private const float LoadingFrameBudgetSeconds = 0.012f;

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

    /// <summary>
    /// Whether the current movement was started by a paste. Cancelling that
    /// movement must also remove the pasted entities again.
    /// </summary>
    private bool pasteTransactionOpen;

    /// <summary>
    /// Whether the clipboard is currently being turned into entities over several
    /// frames. Pasting again is refused while this is true.
    /// </summary>
    private bool pasteInProgress;

    /// <summary>
    /// Whether a previewed move is currently being committed over several frames.
    /// Confirming or cancelling again is refused while this is true.
    /// </summary>
    private bool placementInProgress;

    /// <summary>
    /// Entities created by the previewed resize. They are removed again when the
    /// preview is cancelled and become undoable when it is confirmed.
    /// </summary>
    private List<SaveableEntity> resizePreviewCreatedEntities = new ();

    /// <summary>
    /// Cells that the previewed resize fills with block content: either a copy
    /// of a source cell or a source cell that moved there.
    /// </summary>
    private List<Vector2Int> resizePreviewFilledCells = new ();

    /// <summary>
    /// Deactivates the entities that the previewed resize removes, and brings
    /// them back when it is cancelled.
    /// </summary>
    private EntityPresenceAction resizePreviewDeleteAction;

    /// <summary>
    /// Moves the entities that survive a shrinking resize, applied on confirm.
    /// </summary>
    private MoveEntitiesAction resizePreviewMoveAction;

    /// <summary>
    /// Where the entities that survive a shrinking resize were before it started.
    /// </summary>
    private Dictionary<SaveableEntity, Vector3> resizePreviewStartPositions = new ();

    /// <summary>
    /// Entities that the previewed resize drops. They are only deactivated and
    /// keep their old position, so they must not stay in the selection once the
    /// resize is applied.
    /// </summary>
    private HashSet<SaveableEntity> resizePreviewRemovedEntities = new ();

    /// <summary>
    /// The selection before the resize, so it can be restored when the preview
    /// is cancelled.
    /// </summary>
    private List<SaveableEntity> resizeSourceSelection = new ();

    /// <inheritdoc/>
    public override bool IsBusy => this.pasteInProgress || this.placementInProgress;

    /// <inheritdoc/>
    protected override bool HasSelection =>
        MapEditorManager.Instance.SelectedEntities != null &&
        MapEditorManager.Instance.SelectedEntities.Any(
            entity => entity != null && entity.gameObject.activeSelf);

    /// <inheritdoc/>
    protected override bool CanUseSelectionActions =>
        this.HasSelection &&
        MapEditorManager.Instance.SelectedEntities
            .All(e => e != null && e.ConsumedBy == null);

    /// <summary>
    /// Copies the selected entities to the clipboard, including their editable values.
    /// </summary>
    public override void CopySelected()
    {
        if (this.IsReferenceSelectionMode)
        {
            return;
        }

        List<BuilderEntity> entities =
            MapEditorManager.Instance.SelectedEntities
                .OfType<BuilderEntity>()
                .Where(e =>
                    e != null &&
                    e.ExistsInWorld &&
                    !string.IsNullOrEmpty(e.PrefabID))
                .ToList();

        if (entities.Count == 0)
        {
            return;
        }

        AddConsumedEntities(entities);

        BuilderClipboard.StoreEntities(this.CreateClipboardEntities(entities));

        MapEditorManager.Instance.ShowClipboardMessage(
            $"Copied {entities.Count} {(entities.Count == 1 ? "entity" : "entities")}");

        this.DisableSelectionButtons();
    }

    /// <summary>
    /// Copies the selected entities and deletes them as one undo step.
    /// </summary>
    public override void CutSelected()
    {
        if (this.IsReferenceSelectionMode)
        {
            return;
        }

        MapEditorManager manager = MapEditorManager.Instance;

        manager.BeginTransaction();
        this.CopySelected();
        this.DeleteSelected();
        manager.EndTransaction();
    }

    /// <summary>
    /// Creates the clipboard entities at the pointer and starts moving them, so the
    /// position can be chosen before the paste is confirmed. Confirming places them,
    /// cancelling removes them again. The copies are built over several frames, so a
    /// large paste does not freeze the game.
    /// </summary>
    public override void PasteAtPointer()
    {
        if (this.IsMovementMode ||
            this.IsReferenceSelectionMode ||
            this.pasteInProgress)
        {
            return;
        }

        ClipboardEntities clipboard = BuilderClipboard.Entities;

        if (clipboard == null || clipboard.Entries.Count == 0)
        {
            return;
        }

        Transform parent = this.GetEntityParent();

        if (parent == null)
        {
            Debug.LogWarning("Cannot paste entities: no 'Entity parent' object found.");
            return;
        }

        Vector2Int targetCell =
            (Vector2Int)this.Grid.WorldToCell(this.CurrentPos);

        this.StartCoroutine(
            this.PasteEntitiesRoutine(clipboard, targetCell, parent));
    }

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

        if (!this.IsMovementMode || this.placementInProgress)
        {
            return;
        }

        this.StartCoroutine(this.ConfirmMoveRoutine());
    }

    /// <inheritdoc/>
    public override void CancelMovingSelected()
    {
        if (!this.IsMovementMode || this.placementInProgress)
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

        this.CancelPasteTransaction();
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
            if (picked.Count >= this.MaxSelectedCells)
            {
                break;
            }

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
    /// Gets the grid cells the selected entities cover.
    /// </summary>
    /// <returns>The selected cells.</returns>
    protected override RectInt GetSelectedCellRect()
    {
        List<SaveableEntity> selected =
            MapEditorManager.Instance.SelectedEntities;

        if (selected == null || selected.Count == 0 || this.Grid == null)
        {
            return default(RectInt);
        }

        bool hasCell = false;
        Vector2Int min = Vector2Int.zero;
        Vector2Int max = Vector2Int.zero;

        foreach (SaveableEntity entity in selected)
        {
            if (entity == null ||
                entity.ConsumedBy != null ||
                !entity.gameObject.activeSelf)
            {
                continue;
            }

            Vector2Int cell =
                (Vector2Int)this.Grid.WorldToCell(entity.transform.position);

            if (!hasCell)
            {
                min = cell;
                max = cell;
                hasCell = true;
                continue;
            }

            min = Vector2Int.Min(min, cell);
            max = Vector2Int.Max(max, cell);
        }

        return hasCell
            ? new RectInt(min.x, min.y, max.x - min.x + 1, max.y - min.y + 1)
            : default(RectInt);
    }

    /// <summary>
    /// Gets the world rectangle covered by a block of cells.
    /// </summary>
    /// <param name="cells">The cells to cover.</param>
    /// <returns>The world rectangle of the cells.</returns>
    protected override Rect GetCellsWorldRect(RectInt cells)
    {
        if (cells.width <= 0 || cells.height <= 0 || this.Grid == null)
        {
            return default(Rect);
        }

        // One selected cell is one cell of the grid, so the box is the footprint
        // of the selected cells. Sprites are not measured here: a renderer can be
        // larger than the entity's footprint, which made the box twice its size.
        Vector3 min = this.Grid.CellToWorld(
            new Vector3Int(cells.xMin, cells.yMin, 0));

        Vector3 max = this.Grid.CellToWorld(
            new Vector3Int(cells.xMax, cells.yMax, 0));

        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }

    /// <summary>
    /// Builds the resized block without touching the undo history: entities that
    /// disappear are only deactivated, survivors are only moved and the extra
    /// entities are created unregistered. Confirming turns all of it into one
    /// undo step, cancelling puts everything back.
    /// </summary>
    /// <param name="source">The cells of the current selection.</param>
    /// <param name="target">The cells the selection is resized to.</param>
    protected override void BeginResizePreview(RectInt source, RectInt target)
    {
        this.CancelResizePreview();

        if (this.Grid == null || source.width <= 0 || source.height <= 0)
        {
            return;
        }

        this.resizeSourceSelection = new List<SaveableEntity>(
            MapEditorManager.Instance.SelectedEntities);

        List<BuilderEntity> blockEntities =
            MapEditorManager.Instance.SelectedEntities
                .OfType<BuilderEntity>()
                .Where(e =>
                    e != null &&
                    e.ExistsInWorld &&
                    !string.IsNullOrEmpty(e.PrefabID))
                .ToList();

        AddConsumedEntities(blockEntities);

        Dictionary<SaveableEntity, ClipboardEntityEntry> entries =
            new Dictionary<SaveableEntity, ClipboardEntityEntry>();

        foreach (ClipboardEntityEntry entry in
                 this.CreateClipboardEntities(blockEntities).Entries)
        {
            if (entry.Source != null)
            {
                entries[entry.Source] = entry;
            }
        }

        Dictionary<Vector2Int, List<BuilderEntity>> cells = new ();

        foreach (BuilderEntity entity in blockEntities)
        {
            if (entity.ConsumedBy != null)
            {
                continue;
            }

            Vector2Int cell =
                (Vector2Int)this.Grid.WorldToCell(entity.transform.position);

            if (!cells.TryGetValue(cell, out List<BuilderEntity> entities))
            {
                entities = new List<BuilderEntity>();
                cells[cell] = entities;
            }

            entities.Add(entity);
        }

        List<SaveableEntity> removed = new ();
        HashSet<Vector2Int> keptCells = new ();

        this.resizePreviewMoveAction = new MoveEntitiesAction();

        foreach (KeyValuePair<Vector2Int, List<BuilderEntity>> entry in cells)
        {
            bool survivesX = SelectionResize.TryTargetIndexForSource(
                entry.Key.x,
                target.xMin,
                target.width,
                source.xMin,
                source.width,
                out int targetX);

            bool survivesY = SelectionResize.TryTargetIndexForSource(
                entry.Key.y,
                target.yMin,
                target.height,
                source.yMin,
                source.height,
                out int targetY);

            if (!survivesX || !survivesY)
            {
                removed.AddRange(entry.Value);
                this.resizePreviewRemovedEntities.UnionWith(entry.Value);
                continue;
            }

            Vector2Int targetCell = new (targetX, targetY);

            keptCells.Add(targetCell);

            if (targetCell == entry.Key)
            {
                continue;
            }

            // The block lands on this cell now, so anything already standing
            // there has to make way, just like it does on a cell that gets a copy.
            this.resizePreviewFilledCells.Add(targetCell);

            foreach (BuilderEntity entity in entry.Value)
            {
                Vector3 before = entity.transform.position;

                this.resizePreviewStartPositions[entity] = before;

                entity.transform.position =
                    this.GetEntityWorldPosition((Vector3Int)targetCell);

                this.RefreshBuilderWall(entity);

                this.resizePreviewMoveAction.Add(
                    entity,
                    before,
                    entity.transform.position);
            }
        }

        if (removed.Count > 0)
        {
            this.resizePreviewDeleteAction =
                EntityPresenceAction.DeleteNow(removed);
        }

        // Every cell has its own copies, which only take part in their own
        // consuming relationships, so they are grouped per cell.
        Dictionary<Vector2Int, Dictionary<SaveableEntity, SaveableEntity>> copiesByCell =
            new ();

        for (int x = target.xMin; x < target.xMax; x++)
        {
            for (int y = target.yMin; y < target.yMax; y++)
            {
                Vector2Int cell = new (x, y);

                if (keptCells.Contains(cell))
                {
                    continue;
                }

                int sourceX = SelectionResize.SourceIndexForTarget(
                    x, target.xMin, target.width, source.xMin, source.width);

                int sourceY = SelectionResize.SourceIndexForTarget(
                    y, target.yMin, target.height, source.yMin, source.height);

                if (!cells.TryGetValue(
                        new Vector2Int(sourceX, sourceY),
                        out List<BuilderEntity> sourceEntities))
                {
                    continue;
                }

                if (!copiesByCell.TryGetValue(
                        cell,
                        out Dictionary<SaveableEntity, SaveableEntity> copies))
                {
                    copies = new Dictionary<SaveableEntity, SaveableEntity>();
                    copiesByCell[cell] = copies;
                }

                this.resizePreviewFilledCells.Add(cell);

                foreach (BuilderEntity sourceEntity in sourceEntities)
                {
                    this.CreateResizeCopy(sourceEntity, cell, entries, copies);

                    foreach (BuilderEntity consumed in GetConsumedEntities(sourceEntity))
                    {
                        this.CreateResizeCopy(consumed, cell, entries, copies);
                    }
                }
            }
        }

        foreach (Dictionary<SaveableEntity, SaveableEntity> copies in copiesByCell.Values)
        {
            foreach (KeyValuePair<SaveableEntity, SaveableEntity> pair in copies)
            {
                if (pair.Value is not BuilderEntity copy ||
                    !entries.TryGetValue(pair.Key, out ClipboardEntityEntry entry))
                {
                    continue;
                }

                foreach (KeyValuePair<string, object> field in entry.FieldValues)
                {
                    copy.SetRuntimeEditableValue(
                        field.Key,
                        RemapFieldValue(field.Value, copies));
                }
            }

            ApplyCopiedConsumption(copies);
        }

        List<SaveableEntity> selection = new ();

        foreach (SaveableEntity entity in blockEntities)
        {
            if (entity != null && entity.ExistsInWorld && entity.ConsumedBy == null)
            {
                selection.Add(entity);
            }
        }

        selection.AddRange(
            this.resizePreviewCreatedEntities.Where(
                e => e != null && e.ConsumedBy == null));

        this.SetSelection(selection);
    }

    /// <summary>
    /// Turns the previewed resize into one undo step: the removed entities stay
    /// removed, the survivors stay moved, and the created entities stay placed.
    /// </summary>
    protected override void ApplyResizePreview()
    {
        MapEditorManager manager = MapEditorManager.Instance;

        List<SaveableEntity> replaced = this.FindReplacedEntities();

        manager.BeginTransaction();

        if (replaced.Count > 0)
        {
            manager.RegisterAction(EntityPresenceAction.DeleteNow(replaced));
        }

        if (this.resizePreviewDeleteAction != null)
        {
            manager.RegisterAction(this.resizePreviewDeleteAction);
        }

        if (this.resizePreviewMoveAction != null &&
            this.resizePreviewMoveAction.HasChanges)
        {
            manager.RegisterAction(this.resizePreviewMoveAction);
        }

        foreach (SaveableEntity entity in this.resizePreviewCreatedEntities)
        {
            if (entity != null)
            {
                manager.RegisterAction(EntityPresenceAction.Placed(entity));
            }
        }

        manager.EndTransaction();

        // Entities dropped by a shrinking resize are only deactivated, so they
        // would keep the selection on the old, larger block and the handles are
        // placed from exactly that selection.
        List<SaveableEntity> selection =
            MapEditorManager.Instance.SelectedEntities
                .Where(e => e != null && !this.resizePreviewRemovedEntities.Contains(e))
                .ToList();

        this.ClearResizePreviewState();

        this.SetSelection(selection);

        if (RuntimePropertyEditor.Instance != null)
        {
            RuntimePropertyEditor.Instance.Rebuild();
        }
    }

    /// <summary>
    /// Puts the world back the way it was before the resize was previewed.
    /// </summary>
    protected override void CancelResizePreview()
    {
        foreach (SaveableEntity copy in this.resizePreviewCreatedEntities)
        {
            if (copy != null)
            {
                Destroy(copy.gameObject);
            }
        }

        if (this.resizePreviewDeleteAction != null)
        {
            this.resizePreviewDeleteAction.Undo();
        }

        foreach (KeyValuePair<SaveableEntity, Vector3> entry in this.resizePreviewStartPositions)
        {
            if (entry.Key == null)
            {
                continue;
            }

            entry.Key.transform.position = entry.Value;

            this.RefreshBuilderWall(entry.Key);
        }

        List<SaveableEntity> restore = new (this.resizeSourceSelection);

        this.ClearResizePreviewState();

        if (restore.Count > 0)
        {
            this.SetSelection(restore);
        }
    }

    /// <summary>
    /// Selects the closest entity within range of the click, if any.
    /// A plain click replaces the selection, and clicking the only selected entity
    /// clears it. Ctrl toggles the clicked entity and Shift adds it.
    /// </summary>
    /// <param name="position">The position that was clicked.</param>
    /// <param name="selectionMode">How the pick combines with the current selection.</param>
    protected override void OnClickSelect(Vector2 position, SelectionMode selectionMode)
    {
        List<SaveableEntity> picked = this.PickEntitiesAt(position);
        List<SaveableEntity> current =
            MapEditorManager.Instance.SelectedEntities;

        if (selectionMode != SelectionMode.Replace)
        {
            if (picked.Count == 0)
            {
                return;
            }

            this.SetSelection(
                selectionMode == SelectionMode.Toggle
                    ? this.ToggleInSelection(current, picked[0])
                    : this.AddToSelection(current, picked));

            return;
        }

        if (picked.Count == 0)
        {
            this.ClearSelection();
            return;
        }

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
    /// With Ctrl the box contents are toggled: entities that were already selected
    /// are dropped and the others are added. Shift only adds.
    /// </summary>
    /// <param name="rect">The world-space rectangle of the drag.</param>
    /// <param name="selectionMode">How the box contents combine with the current selection.</param>
    protected override void OnBoxSelect(Rect rect, SelectionMode selectionMode)
    {
        List<SaveableEntity> picked = this.PickEntitiesIn(rect);

        if (selectionMode == SelectionMode.Replace)
        {
            this.SetSelection(picked);
            return;
        }

        List<SaveableEntity> current =
            MapEditorManager.Instance.SelectedEntities;

        this.SetSelection(
            selectionMode == SelectionMode.Toggle
                ? this.ToggleInSelection(current, picked)
                : this.AddToSelection(current, picked));
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

        this.DisableSelectionButtons();
    }

    /// <summary>
    /// Gets the transform that pasted entities are parented to.
    /// </summary>
    /// <returns>The entity parent transform, or null when it is missing.</returns>
    private Transform GetEntityParent()
    {
        GameObject parent = GameObject.FindWithTag("Entity parent");

        return parent != null ? parent.transform : null;
    }

    /// <summary>
    /// Builds the clipboard content for the given entities, using the lowest cell of
    /// the visible entities as the clipboard origin. Consumed entities keep the cell of
    /// their consumer, because their own position says nothing about where they belong.
    /// </summary>
    /// <param name="entities">The entities to copy.</param>
    /// <returns>The clipboard content.</returns>
    private ClipboardEntities CreateClipboardEntities(List<BuilderEntity> entities)
    {
        Dictionary<BuilderEntity, Vector2Int> cells = new ();
        Vector2Int origin = Vector2Int.zero;
        bool hasOrigin = false;

        foreach (BuilderEntity entity in entities)
        {
            if (entity.ConsumedBy != null)
            {
                continue;
            }

            Vector2Int cell =
                (Vector2Int)this.Grid.WorldToCell(entity.transform.position);

            cells[entity] = cell;

            origin = hasOrigin ? Vector2Int.Min(origin, cell) : cell;
            hasOrigin = true;
        }

        List<ClipboardEntityEntry> entries = new ();

        foreach (BuilderEntity entity in entities)
        {
            Vector2Int cell;

            if (!cells.TryGetValue(entity, out cell))
            {
                BuilderEntity consumer = entity.ConsumedBy as BuilderEntity;

                cell = consumer != null &&
                    cells.TryGetValue(consumer, out Vector2Int consumerCell)
                        ? consumerCell
                        : origin;
            }

            entries.Add(
                new ClipboardEntityEntry(
                    entity,
                    entity.PrefabID,
                    cell - origin,
                    SnapshotFieldValues(entity)));
        }

        return new ClipboardEntities(entries);
    }

    /// <summary>
    /// Adds the entities that are consumed by the copied ones, so a copied enemy body
    /// brings its own head instead of pointing at the original. A consumed entity can
    /// be consumed again, so this keeps walking until nothing new is found.
    /// </summary>
    /// <param name="entities">The entities being copied. Grows while walking.</param>
    private static void AddConsumedEntities(List<BuilderEntity> entities)
    {
        for (int i = 0; i < entities.Count; i++)
        {
            BuilderEntity consumer = entities[i];

            foreach (BuilderEntity candidate in BuilderRegistry.GetAll())
            {
                if (candidate == null ||
                    candidate.ConsumedBy != consumer ||
                    entities.Contains(candidate) ||
                    string.IsNullOrEmpty(candidate.PrefabID))
                {
                    continue;
                }

                entities.Add(candidate);
            }
        }
    }

    /// <summary>
    /// Recreates the consuming relationships between the pasted entities. The copies
    /// hold references to other copies, so a consumed copy also has to learn which
    /// entity consumes it, otherwise it would show up as a separate object on the map.
    /// </summary>
    /// <param name="copies">Map from the copied entities to their new copies.</param>
    private static void ApplyCopiedConsumption(
        Dictionary<SaveableEntity, SaveableEntity> copies)
    {
        foreach (SaveableEntity entity in copies.Values)
        {
            BuilderEntity consumer = entity as BuilderEntity;

            if (consumer == null)
            {
                continue;
            }

            foreach (KeyValuePair<string, RuntimeEditableValue> entry in
                     consumer.RuntimeEditableFields)
            {
                if (entry.Value == null || entry.Value.Field == null)
                {
                    continue;
                }

                SaveFieldAttribute attribute =
                    entry.Value.Field.GetCustomAttribute<SaveFieldAttribute>();

                if (attribute == null ||
                    attribute.ReferenceMode != ReferenceMode.Consuming)
                {
                    continue;
                }

                ConsumeFieldValue(entry.Value.Value, consumer, entry.Key);
            }
        }
    }

    /// <summary>
    /// Marks every entity inside a single or list consuming field as consumed by the
    /// given consumer.
    /// </summary>
    /// <param name="value">The value of the consuming field.</param>
    /// <param name="consumer">The entity that consumes it.</param>
    /// <param name="fieldName">The name of the consuming field.</param>
    private static void ConsumeFieldValue(
        object value,
        SaveableEntity consumer,
        string fieldName)
    {
        SaveableEntity single = value as SaveableEntity;

        if (single != null)
        {
            single.SetConsumedBy(consumer, fieldName);
            return;
        }

        if (value is IList list)
        {
            foreach (object item in list)
            {
                SaveableEntity referenced = item as SaveableEntity;

                if (referenced != null)
                {
                    referenced.SetConsumedBy(consumer, fieldName);
                }
            }
        }
    }

    /// <summary>
    /// Takes a copy of an entity's runtime editable values, so editing the
    /// original afterwards does not change what is on the clipboard.
    /// </summary>
    /// <param name="entity">The entity to snapshot.</param>
    /// <returns>The field values by field name.</returns>
    private static Dictionary<string, object> SnapshotFieldValues(
        BuilderEntity entity)
    {
        Dictionary<string, object> values = new ();

        foreach (KeyValuePair<string, RuntimeEditableValue> entry in
                 entity.RuntimeEditableFields)
        {
            if (entry.Value == null)
            {
                continue;
            }

            object value = entry.Value.Value;

            values[entry.Key] =
                value is IList list && IsReferenceList(list)
                    ? new List<SaveableEntity>(list.Cast<SaveableEntity>())
                    : value;
        }

        return values;
    }

    /// <summary>
    /// Points references that were inside the copied set at the new copies.
    /// References to entities outside the set are left alone.
    /// </summary>
    /// <param name="value">The copied field value.</param>
    /// <param name="copies">Map from the copied entities to their new copies.</param>
    /// <returns>The value to write on the copy.</returns>
    private static object RemapFieldValue(
        object value,
        Dictionary<SaveableEntity, SaveableEntity> copies)
    {
        if (value is SaveableEntity entity)
        {
            return copies.TryGetValue(entity, out SaveableEntity copy)
                ? copy
                : value;
        }

        if (value is IList list && IsReferenceList(list))
        {
            List<SaveableEntity> remapped = new (list.Count);

            foreach (object item in list)
            {
                SaveableEntity reference = item as SaveableEntity;

                remapped.Add(
                    reference != null &&
                    copies.TryGetValue(reference, out SaveableEntity copy)
                        ? copy
                        : reference);
            }

            return remapped;
        }

        return value;
    }

    /// <summary>
    /// Checks whether a list holds entity references. Those are the only lists
    /// that have to be copied and remapped.
    /// </summary>
    /// <param name="list">The list to check.</param>
    /// <returns>True when the list is empty or holds entity references.</returns>
    private static bool IsReferenceList(IList list)
    {
        return list.Count == 0 || list[0] is SaveableEntity;
    }

    /// <summary>
    /// Places the selection where it is being previewed, spreading the work over
    /// several frames so placing a large selection does not freeze the game.
    /// </summary>
    /// <returns>The steps that commit the move.</returns>
    private IEnumerator ConfirmMoveRoutine()
    {
        this.placementInProgress = true;

        // The preview must stay where it is while it is committed, so the pointer
        // may not drag it any more.
        this.IsMoving = false;

        List<SaveableEntity> selectedEntities =
            MapEditorManager.Instance.SelectedEntities;

        HashSet<SaveableEntity> movingEntities =
            new HashSet<SaveableEntity>(selectedEntities);

        List<SaveableEntity> replaced = new List<SaveableEntity>();
        MoveEntitiesAction move = new MoveEntitiesAction();

        // Built once, instead of scanning the whole scene for every moved entity.
        Dictionary<Vector2Int, List<SaveableEntity>> entitiesByCell =
            this.BuildEntityCellMap();

        int totalSteps = this.extraMoveStartCells.Count;
        int completedSteps = default(int);

        float frameStart = Time.realtimeSinceStartup;

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

            SaveableEntity entityAtDestination = null;

            if (entitiesByCell.TryGetValue(
                    finalCell,
                    out List<SaveableEntity> entitiesInCell))
            {
                foreach (SaveableEntity candidate in entitiesInCell)
                {
                    if (candidate != entity &&
                        !movingEntities.Contains(candidate))
                    {
                        entityAtDestination = candidate;
                        break;
                    }
                }
            }

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

            completedSteps++;

            if (this.ShouldYieldLoadingFrame(frameStart, completedSteps, totalSteps))
            {
                yield return null;
                frameStart = Time.realtimeSinceStartup;
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
        this.CommitPasteTransaction();
        this.SetSelection(
            MapEditorManager.Instance.SelectedEntities);

        if (RuntimePropertyEditor.Instance != null)
        {
            RuntimePropertyEditor.Instance.Rebuild();
        }

        BuilderLoadingOverlay.Hide();
        this.placementInProgress = false;
    }

    /// <summary>
    /// Groups the entities that exist in the world by the cell they stand in, so a
    /// move can find what it lands on without scanning the scene once per entity.
    /// Entities that are being moved are left in, because the caller knows which
    /// of them are moving.
    /// </summary>
    /// <returns>The entities by grid cell.</returns>
    private Dictionary<Vector2Int, List<SaveableEntity>> BuildEntityCellMap()
    {
        Dictionary<Vector2Int, List<SaveableEntity>> cells = new ();

        foreach (SaveableEntity entity in FindObjectsByType<SaveableEntity>())
        {
            if (!entity.ExistsInWorld ||
                IsTilemapEntity(entity))
            {
                continue;
            }

            Vector2Int cell =
                (Vector2Int)this.Grid.WorldToCell(entity.transform.position);

            if (!cells.TryGetValue(cell, out List<SaveableEntity> entities))
            {
                entities = new List<SaveableEntity>();
                cells[cell] = entities;
            }

            entities.Add(entity);
        }

        return cells;
    }

    /// <summary>
    /// Rebuilds the clipboard entities, spreading the work over as many frames as
    /// needed so the editor keeps rendering. The loading overlay is shown as soon as
    /// one frame's budget is used up.
    /// </summary>
    /// <param name="clipboard">The clipboard content to paste.</param>
    /// <param name="targetCell">The cell the clipboard's origin cell is pasted at.</param>
    /// <param name="parent">The transform the copies are parented to.</param>
    /// <returns>The steps that create the copies and start moving them.</returns>
    private IEnumerator PasteEntitiesRoutine(
        ClipboardEntities clipboard,
        Vector2Int targetCell,
        Transform parent)
    {
        this.pasteInProgress = true;

        List<SaveableEntity> pasted = new ();
        Dictionary<SaveableEntity, SaveableEntity> copies = new ();

        MapEditorManager manager = MapEditorManager.Instance;

        manager.BeginTransaction();

        int totalSteps = clipboard.Entries.Count * 2;
        int completedSteps = default(int);

        float frameStart = Time.realtimeSinceStartup;

        foreach (ClipboardEntityEntry entry in clipboard.Entries)
        {
            if (entry.Source == null)
            {
                continue;
            }

            GameObject created = new GameObject(entry.PrefabID);

            created.transform.position =
                this.GetEntityWorldPosition(
                    (Vector3Int)(targetCell + entry.Offset));

            created.transform.SetParent(parent);

            BuilderEntity builder = created.AddComponent<BuilderEntity>();

            builder.Initialize(entry.PrefabID);

            copies[entry.Source] = builder;
            pasted.Add(builder);

            completedSteps++;

            if (this.ShouldYieldLoadingFrame(frameStart, completedSteps, totalSteps))
            {
                yield return null;
                frameStart = Time.realtimeSinceStartup;
            }
        }

        foreach (ClipboardEntityEntry entry in clipboard.Entries)
        {
            if (entry.Source == null ||
                !copies.TryGetValue(entry.Source, out SaveableEntity copied) ||
                copied is not BuilderEntity copy)
            {
                continue;
            }

            foreach (KeyValuePair<string, object> field in entry.FieldValues)
            {
                copy.SetRuntimeEditableValue(
                    field.Key,
                    RemapFieldValue(field.Value, copies));
            }

            completedSteps++;

            if (this.ShouldYieldLoadingFrame(frameStart, completedSteps, totalSteps))
            {
                yield return null;
                frameStart = Time.realtimeSinceStartup;
            }
        }

        ApplyCopiedConsumption(copies);

        if (pasted.Count == 0)
        {
            manager.CancelTransaction();

            BuilderLoadingOverlay.Hide();
            this.pasteInProgress = false;

            yield break;
        }

        foreach (SaveableEntity entity in pasted)
        {
            manager.RegisterAction(EntityPresenceAction.Placed(entity));
        }

        this.pasteTransactionOpen = true;

        // Consumed copies are hidden behind their consumer, so only the visible copies
        // become the new selection.
        this.SetSelection(pasted.Where(e => e.ConsumedBy == null).ToList());
        this.MoveSelected();

        manager.ShowClipboardMessage(
            $"Pasting {pasted.Count} {(pasted.Count == 1 ? "entity" : "entities")}");

        BuilderLoadingOverlay.SetProgress(1f);
        BuilderLoadingOverlay.Hide();

        this.pasteInProgress = false;
    }

    /// <summary>
    /// Reports the progress of a long builder task and tells the caller to wait for
    /// the next frame once the current frame's budget is used up.
    /// </summary>
    /// <param name="frameStart">When the current frame's slice of work started.</param>
    /// <param name="completedSteps">The number of finished steps.</param>
    /// <param name="totalSteps">The number of steps in total.</param>
    /// <returns>True when the task must continue on the next frame.</returns>
    private bool ShouldYieldLoadingFrame(
        float frameStart,
        int completedSteps,
        int totalSteps)
    {
        if (Time.realtimeSinceStartup - frameStart < LoadingFrameBudgetSeconds)
        {
            return false;
        }

        BuilderLoadingOverlay.Show();
        BuilderLoadingOverlay.SetProgress(completedSteps / (float)totalSteps);

        return true;
    }

    /// <summary>
    /// Commits the transaction a paste opened, so the creation of the copies and
    /// their placement become one undo step.
    /// </summary>
    private void CommitPasteTransaction()
    {
        if (!this.pasteTransactionOpen)
        {
            return;
        }

        this.pasteTransactionOpen = false;
        MapEditorManager.Instance.EndTransaction();
    }

    /// <summary>
    /// Cancels the transaction a paste opened, removing the pasted entities again.
    /// </summary>
    private void CancelPasteTransaction()
    {
        if (!this.pasteTransactionOpen)
        {
            return;
        }

        this.pasteTransactionOpen = false;
        MapEditorManager.Instance.CancelTransaction();

        this.ClearSelection();

        if (RuntimePropertyEditor.Instance != null)
        {
            RuntimePropertyEditor.Instance.Rebuild();
        }
    }

    /// <summary>
    /// Creates the copy of one entity that a resized block needs in a cell.
    /// </summary>
    /// <param name="source">The entity to copy.</param>
    /// <param name="cell">The cell of the new block the copy is placed in.</param>
    /// <param name="entries">The clipboard entries that hold the values to copy.</param>
    /// <param name="copies">The copies of the block the copy belongs to.</param>
    /// <returns>The copy, or null when the entity cannot be copied.</returns>
    private SaveableEntity CreateResizeCopy(
        BuilderEntity source,
        Vector2Int cell,
        Dictionary<SaveableEntity, ClipboardEntityEntry> entries,
        Dictionary<SaveableEntity, SaveableEntity> copies)
    {
        if (source == null ||
            !entries.TryGetValue(source, out ClipboardEntityEntry entry))
        {
            return null;
        }

        GameObject created = new (entry.PrefabID);

        created.transform.position =
            this.GetEntityWorldPosition((Vector3Int)cell);

        created.transform.SetParent(this.GetEntityParent());

        BuilderEntity copy = created.AddComponent<BuilderEntity>();

        copy.Initialize(entry.PrefabID);

        copies[source] = copy;
        this.resizePreviewCreatedEntities.Add(copy);

        return copy;
    }

    /// <summary>
    /// Gets the entities that consume the given one, so they are copied with it.
    /// </summary>
    /// <param name="consumer">The entity that consumes others.</param>
    /// <returns>The entities it consumes.</returns>
    private List<BuilderEntity> GetConsumedEntities(BuilderEntity consumer)
    {
        List<BuilderEntity> consumed = new ();

        foreach (BuilderEntity candidate in BuilderRegistry.GetAll())
        {
            if (candidate != null && candidate.ConsumedBy == consumer)
            {
                consumed.Add(candidate);
            }
        }

        return consumed;
    }

    /// <summary>
    /// Finds the entities that stand on a cell the resize fills with a copy, so
    /// they are removed with the resize like a move removes what it lands on.
    /// </summary>
    /// <returns>The entities that are replaced by the new block.</returns>
    private List<SaveableEntity> FindReplacedEntities()
    {
        List<SaveableEntity> replaced = new ();

        if (this.resizePreviewFilledCells.Count == 0)
        {
            return replaced;
        }

        HashSet<Vector2Int> cells = new (this.resizePreviewFilledCells);
        HashSet<SaveableEntity> owned = new (this.resizePreviewCreatedEntities);

        // Survivors that moved already stand on the cell they moved to, and
        // dropped ones are only deactivated, so neither may be replaced.
        owned.UnionWith(this.resizePreviewStartPositions.Keys);
        owned.UnionWith(this.resizePreviewRemovedEntities);

        foreach (SaveableEntity entity in FindObjectsByType<SaveableEntity>())
        {
            if (!entity.ExistsInWorld ||
                entity.ConsumedBy != null ||
                IsTilemapEntity(entity) ||
                owned.Contains(entity))
            {
                continue;
            }

            Vector2Int cell =
                (Vector2Int)this.Grid.WorldToCell(entity.transform.position);

            if (cells.Contains(cell))
            {
                replaced.Add(entity);
            }
        }

        return replaced;
    }

    /// <summary>
    /// Drops everything that belongs to the previewed resize without touching
    /// the world.
    /// </summary>
    private void ClearResizePreviewState()
    {
        this.resizePreviewCreatedEntities.Clear();
        this.resizePreviewFilledCells.Clear();
        this.resizePreviewStartPositions.Clear();
        this.resizePreviewRemovedEntities.Clear();
        this.resizeSourceSelection.Clear();

        this.resizePreviewDeleteAction = null;
        this.resizePreviewMoveAction = null;
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
