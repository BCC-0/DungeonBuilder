
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The editor tools that can be selected in the builder.
/// </summary>
public enum EditorTool
{
    /// <summary>
    /// The drag tool is used for navigating by dragging instead of the usual method (WASD).
    /// </summary>
    Drag,

    /// <summary>
    /// The brush tool draws the currently selected item.
    /// </summary>
    Brush,

    /// <summary>
    /// The eraser tool removes the hovered item.
    /// </summary>
    Eraser,

    /// <summary>
    /// The selection tool allows us to select one or multiple placed objects for editing.
    /// If the entities are of the same type, we can edit the attributes at the same time.
    /// </summary>
    Selection,

    /// <summary>
    /// The pick tool selects an existing entity or tile for the item bar.
    /// </summary>
    Pick,
}

/// <summary>
/// The layer we are currently editing.
/// </summary>
public enum EditLayer
{
    /// <summary>
    /// The background contains tiles and walls.
    /// </summary>
    Background,

    /// <summary>
    /// The foreground contains entities.
    /// </summary>
    Foreground,
}

/// <summary>
/// The map editor manager manages the tools used in the builder.
/// </summary>
public class MapEditorManager : MonoBehaviour
{
    /// <summary>
    /// Time in seconds the clipboard toast takes to fade in and out.
    /// </summary>
    private const float ClipboardToastFadeDuration = 0.15f;

    /// <summary>
    /// Seconds the entity fade of a layer change takes.
    /// </summary>
    private const float LayerFadeDuration = 0.25f;

    /// <summary>
    /// Alpha of the entities of the layer that is being edited.
    /// </summary>
    private const float ShownLayerAlpha = 1f;

    /// <summary>
    /// Alpha of the entities of the layer that is not being edited.
    /// </summary>
    private const float HiddenLayerAlpha = 0.2f;

    private static MapEditorManager instance;

    private EditorControllerBase tileController;
    private EditorControllerBase entityController;

    private UndoRedoManager undoRedoManager;

    private EditorTool currentTool;
    private EditLayer currentLayer;

    private List<Vector2Int> selectedTiles = new List<Vector2Int>();
    private List<SaveableEntity> selectedEntities = new List<SaveableEntity>();

    [SerializeField]
    private GameObject[] toolOutline;

    private EditorControllerBase activeController;

    private bool canSwitch = true;

    private string mapName;

    [Header("Layer Button Visuals")]
    [SerializeField]
    private Image backgroundLayerImage;
    [SerializeField]
    private Image foregroundLayerImage;

    [SerializeField]
    private float layerTransitionDuration = 0.25f;

    [Header("Clipboard Feedback")]
    [SerializeField]
    private CanvasGroup clipboardToastCanvasGroup;

    [SerializeField]
    private SpriteFontText clipboardToastText;

    [SerializeField]
    private float clipboardToastDuration = 1.4f;

    private Sequence clipboardToastSequence;

    /// <summary>
    /// Renderers the layer fade writes to, collected once per layer change.
    /// </summary>
    private List<SpriteRenderer> layerRenderers = new ();

    private Tween layerFadeTween;

    /// <summary>
    /// Alpha the entities are currently faded to.
    /// </summary>
    private float layerAlpha = 1f;

    /// <summary>
    /// Gets the instance of the MapEditorManager.
    /// </summary>
    public static MapEditorManager Instance
    {
        get => instance;
        private set => instance = value;
    }

    /// <summary>
    /// Gets the active controller.
    /// </summary>
    public EditorControllerBase ActiveController => this.activeController;

    /// <summary>
    /// Gets a value indicating whether a selection is currently being moved in either layer.
    /// </summary>
    public bool IsMovingSelection =>
        this.tileController.IsMovingSelection ||
        this.entityController.IsMovingSelection;

    /// <summary>
    /// Gets a value indicating whether either layer is busy with work spread over
    /// several frames, such as pasting many entities. Nothing may be edited or undone
    /// while this is true.
    /// </summary>
    public bool IsBusy =>
        this.tileController.IsBusy ||
        this.entityController.IsBusy;

    /// <summary>
    /// Gets or sets a value indicating whether the inventory is open covering the
    /// editor. Editor visuals that draw above the inventory, such as the selection
    /// resize handles, are hidden while this is true.
    /// </summary>
    public bool IsInventoryOpen { get; set; }

    /// <summary>
    /// Gets a value indicating whether a selection resize handle is currently dragged.
    /// </summary>
    public bool IsResizingSelection =>
        this.ActiveController != null &&
        this.ActiveController.IsSelectionResizing;

    /// <summary>
    /// Gets or sets the map name we are editing.
    /// </summary>
    public string MapName { get => this.mapName; set => this.mapName = value; }

    /// <summary>
    /// Gets or sets the current tool.
    /// </summary>
    public EditorTool CurrentTool
    {
        get => this.currentTool;
        set => this.currentTool = value;
    }

    /// <summary>
    /// Gets or sets the current layer.
    /// </summary>
    public EditLayer CurrentLayer
    {
        get => this.currentLayer;
        set => this.currentLayer = value;
    }

    /// <summary>
    /// Gets or sets the currently selected tiles.
    /// </summary>
    public List<Vector2Int> SelectedTiles
    {
        get => this.selectedTiles;
        set => this.selectedTiles = value;
    }

    /// <summary>
    /// Gets or sets the selected entity list.
    /// </summary>
    public List<SaveableEntity> SelectedEntities
    {
        get => this.selectedEntities;
        set => this.selectedEntities = value;
    }

    /// <summary>
    /// Sets the layer to either the fore or background.
    /// </summary>
    /// <param name="layer">Which layer to set to.</param>
    public void SetLayer(EditLayer layer)
    {
        EditorControllerBase previousController = this.activeController;

        this.currentLayer = layer;

        this.activeController =
            layer == EditLayer.Background
                ? this.tileController
                : this.entityController;

        if (previousController != null && previousController != this.activeController)
        {
            previousController.ResetSelectionTool();
        }

        this.StartCoroutine(this.WaitForSwitch());

        this.FadeEntityLayer(
            layer == EditLayer.Foreground
                ? ShownLayerAlpha
                : HiddenLayerAlpha);

        this.UpdateLayerButtonVisuals(layer);
    }

    /// <summary>
    /// Switched the editing layer between fore- and background.
    /// </summary>
    public void ToggleLayer()
    {
        if (!this.canSwitch)
        {
            return;
        }

        EditLayer newLayer = this.CurrentLayer == EditLayer.Background
            ? EditLayer.Foreground
            : EditLayer.Background;

        this.SetLayer(newLayer);
    }

    /// <summary>
    /// Switches to the next tool.
    /// </summary>
    public void SelectNextTool()
    {
        EditorTool[] tools = (EditorTool[])System.Enum.GetValues(typeof(EditorTool));

        int currentIndex = System.Array.IndexOf(tools, this.CurrentTool);

        int nextIndex = (currentIndex + 1) % tools.Length;

        this.SelectTool(tools[nextIndex], nextIndex);
    }

    /// <summary>
    /// Saves the map using the mapName as filename.
    /// </summary>
    public void SaveMap()
    {
        // TODO: Should add username to path before releasing.
        SaveManager.SaveBuilderMap(this.mapName);
    }

    /// <summary>
    /// Registers an action to the undoredomanager.
    /// </summary>
    /// <param name="action">The action to register.</param>
    public void RegisterAction(IUndoableAction action) => this.undoRedoManager.RegisterAction(action);

    /// <summary>
    /// Starts a transaction for the undoredomanager.
    /// </summary>
    public void BeginTransaction() => this.undoRedoManager.BeginTransaction();

    /// <summary>
    /// Ends a transaction for the undoredomanager.
    /// </summary>
    public void EndTransaction() => this.undoRedoManager.EndTransaction();

    /// <summary>
    /// Cancels the open transaction and reverts everything registered in it.
    /// </summary>
    public void CancelTransaction() => this.undoRedoManager.CancelTransaction();

    /// <summary>
    /// Undoes last action.
    /// </summary>
    public void Undo()
    {
        // A move or paste is uncommitted until it is confirmed, so history must
        // not step over it.
        if (this.IsMovingSelection || this.IsBusy || this.IsResizingSelection)
        {
            return;
        }

        this.undoRedoManager.Undo();
        this.RefreshAfterHistory();
    }

    /// <summary>
    /// Redoes last action.
    /// </summary>
    public void Redo()
    {
        if (this.IsMovingSelection || this.IsBusy || this.IsResizingSelection)
        {
            return;
        }

        this.undoRedoManager.Redo();
        this.RefreshAfterHistory();
    }

    /// <summary>
    /// Shows a short message about a clipboard action, such as how many items
    /// were copied. Falls back to the console when no toast is assigned.
    /// </summary>
    /// <param name="message">The message to show.</param>
    public void ShowClipboardMessage(string message)
    {
        if (this.clipboardToastText != null)
        {
            this.clipboardToastText.SetText(message);
        }

        if (this.clipboardToastCanvasGroup == null)
        {
            Debug.Log(message);
            return;
        }

        this.clipboardToastSequence?.Kill();

        this.clipboardToastCanvasGroup.gameObject.SetActive(true);
        this.clipboardToastCanvasGroup.alpha = 0f;

        this.clipboardToastSequence = DOTween.Sequence();

        this.clipboardToastSequence
            .Append(
                this.clipboardToastCanvasGroup.DOFade(
                    1f,
                    ClipboardToastFadeDuration))
            .AppendInterval(this.clipboardToastDuration)
            .Append(
                this.clipboardToastCanvasGroup.DOFade(
                    0f,
                    ClipboardToastFadeDuration))
            .OnComplete(() =>
                this.clipboardToastCanvasGroup.gameObject.SetActive(false));
    }

    /// <summary>
    /// Selects an entity for reference navigation without changing
    /// the active tool or editing layer.
    /// </summary>
    /// <param name="entity">The entity to select.</param>
    public void SelectEntityForReference(SaveableEntity entity)
    {
        if (entity == null)
        {
            return;
        }

        this.selectedTiles.Clear();
        this.selectedEntities.Clear();
        this.selectedEntities.Add(entity);

        EntitySelectionVisualizer visualizer =
            FindAnyObjectByType<EntitySelectionVisualizer>();

        if (visualizer != null)
        {
            visualizer.Refresh();
        }
    }

    /// <summary>
    /// Selects several entities for reference navigation without changing
    /// the active tool or editing layer.
    /// </summary>
    /// <param name="entities">The entities to select.</param>
    public void SelectEntitiesForReference(IReadOnlyList<SaveableEntity> entities)
    {
        if (entities == null || entities.Count == 0)
        {
            return;
        }

        this.selectedTiles.Clear();
        this.selectedEntities.Clear();

        foreach (SaveableEntity entity in entities)
        {
            if (entity != null &&
                !this.selectedEntities.Contains(entity))
            {
                this.selectedEntities.Add(entity);
            }
        }

        EntitySelectionVisualizer visualizer =
            FindAnyObjectByType<EntitySelectionVisualizer>();

        if (visualizer != null)
        {
            visualizer.Refresh();
        }
    }

    /// <summary>
    /// Selects the drag tool.
    /// </summary>
    public void SelectDrag() => this.SelectTool(EditorTool.Drag, 0);

    /// <summary>
    /// Selects the brush tool.
    /// </summary>
    public void SelectBrush() => this.SelectTool(EditorTool.Brush, 1);

    /// <summary>
    /// Selects the eraser tool.
    /// </summary>
    public void SelectEraser() => this.SelectTool(EditorTool.Eraser, 2);

    /// <summary>
    /// Selects the selection tool.
    /// </summary>
    public void SelectSelection() => this.SelectTool(EditorTool.Selection, 3);

    /// <summary>
    /// Selects the pick tool.
    /// </summary>
    public void SelectPick() => this.SelectTool(EditorTool.Pick, 4);

    /// <summary>
    /// Selects the entity or tile under the pointer without leaving the current tool.
    /// Entities are preferred over tiles. When there is nothing to select, the selection is cleared.
    /// </summary>
    /// <param name="worldPos">The world position that was clicked.</param>
    /// <param name="selectionMode">How the pick combines with the current selection.</param>
    public void SelectAtPointer(
        Vector2 worldPos,
        SelectionMode selectionMode = SelectionMode.Replace)
    {
        EditLayer? targetLayer = null;

        if (this.entityController.CanSelectAt(worldPos))
        {
            targetLayer = EditLayer.Foreground;
        }
        else if (this.tileController.CanSelectAt(worldPos))
        {
            targetLayer = EditLayer.Background;
        }

        if (targetLayer == null)
        {
            // Clicking empty space drops the selection but keeps the tool.
            // An additive click leaves the selection as it is instead.
            if (selectionMode == SelectionMode.Replace)
            {
                this.activeController.ResetSelectionTool();
            }

            return;
        }

        if (this.currentLayer != targetLayer.Value)
        {
            this.SetLayer(targetLayer.Value);
        }

        this.activeController.SelectAt(worldPos, selectionMode);
    }

    /// <summary>
    /// Selects the entity under the pointer without leaving the foreground layer.
    /// The brush leaves an entity it clicks standing instead of overwriting it, so
    /// that click can select the entity just like a click in the drag tool. Unlike
    /// <see cref="SelectAtPointer"/>, clicking empty space leaves the selection as it is.
    /// </summary>
    /// <param name="worldPos">The world position that was clicked.</param>
    /// <param name="selectionMode">How the pick combines with the current selection.</param>
    public void SelectEntityAtPointer(
        Vector2 worldPos,
        SelectionMode selectionMode = SelectionMode.Replace)
    {
        if (this.currentLayer != EditLayer.Foreground ||
            !this.entityController.CanSelectAt(worldPos))
        {
            return;
        }

        this.entityController.SelectAt(worldPos, selectionMode);
    }

    /// <summary>
    /// Checks whether a screen position lies on the selection buttons of either layer,
    /// so a press there is not treated as world input.
    /// </summary>
    /// <param name="screenPos">The screen position to test.</param>
    /// <returns>True when the position is on the selection buttons.</returns>
    public bool IsPointerOverSelectionButtons(Vector2 screenPos)
    {
        return this.entityController.IsPointerOverSelectionButtons(screenPos) ||
            this.tileController.IsPointerOverSelectionButtons(screenPos);
    }

    /// <summary>
    /// Starts resizing the selection when the press landed on one of its handles.
    /// </summary>
    /// <param name="screenPos">The screen position of the press.</param>
    /// <returns>True when a handle was grabbed and the press belongs to the resize.</returns>
    public bool BeginSelectionResize(Vector2 screenPos)
    {
        return this.ActiveController != null &&
            this.ActiveController.BeginSelectionResize(screenPos);
    }

    /// <summary>
    /// Ends a resize handle drag and previews the new size.
    /// </summary>
    public void EndSelectionResize()
    {
        if (this.ActiveController != null)
        {
            this.ActiveController.EndSelectionResize();
        }
    }

    /// <summary>
    /// Deletes the currently selected items.
    /// </summary>
    public void OnDeleteSelection() => this.ActiveController.OnDeleteSelection();

    /// <summary>
    /// Copies the currently selected items to the clipboard.
    /// </summary>
    public void OnCopySelection() => this.ActiveController.OnCopySelection();

    /// <summary>
    /// Cuts the currently selected items to the clipboard.
    /// </summary>
    public void OnCutSelection() => this.ActiveController.OnCutSelection();

    /// <summary>
    /// Pastes the clipboard at the pointer and starts moving the pasted items,
    /// so the player can choose the position before confirming. The editing layer
    /// and tool are switched to what the clipboard holds, so the paste can always
    /// be placed and moved right away.
    /// </summary>
    public void OnPasteSelection()
    {
        if (!BuilderClipboard.HasContent ||
            this.IsMovingSelection ||
            this.IsBusy ||
            this.IsResizingSelection)
        {
            return;
        }

        EditLayer contentLayer =
            BuilderClipboard.Entities != null
                ? EditLayer.Foreground
                : EditLayer.Background;

        if (this.currentLayer != contentLayer)
        {
            this.SetLayer(contentLayer);
        }

        this.SelectSelection();

        this.ActiveController.OnPaste();
    }

    /// <summary>
    /// Opens the selection menu at the press, or closes it when it is already open.
    /// The menu opens even without a selection; actions that need one are faded and
    /// cannot be used instead of hiding the whole menu.
    /// </summary>
    /// <param name="worldPos">The world position that was right clicked or double tapped.</param>
    /// <returns>True when the press was used by the menu and must not reach the tool.</returns>
    public bool ToggleSelectionMenu(Vector2 worldPos)
    {
        if (this.IsMovingSelection)
        {
            return false;
        }

        if (this.ActiveController.IsSelectionMenuOpen)
        {
            this.ActiveController.OnSelectionMenuDismissed();

            return true;
        }

        this.ActiveController.OnSelectionMenuRequested(worldPos);

        return true;
    }

    /// <summary>
    /// Closes the selection menu when it is open.
    /// </summary>
    public void HideSelectionMenu()
    {
        this.ActiveController.OnSelectionMenuDismissed();
    }

    /// <summary>
    /// Moves the currently selected items.
    /// </summary>
    public void OnMoveSelection() => this.ActiveController.OnMoveSelection();

    /// <summary>
    /// Confirms the move action.
    /// </summary>
    public void OnConfirmMove() => this.ActiveController.OnConfirmMove();

    /// <summary>
    /// Cancels the move action.
    /// </summary>
    public void OnCancelMove() => this.ActiveController.OnCancelMove();

    /// <summary>
    /// Refreshes the selection visuals and inspector after the selection lists were
    /// changed without a selection action, such as restoring them after a double tap.
    /// </summary>
    public void RefreshSelectionVisuals()
    {
        this.RefreshAfterHistory();
    }

    /// <summary>
    /// Refreshes the selection, inspector and visuals after an undo or redo.
    /// Only the selection manager of the active layer is refreshed, because
    /// both managers share the same selection buttons and the inactive one
    /// would hide them again.
    /// </summary>
    private void RefreshAfterHistory()
    {
        this.selectedEntities.RemoveAll(e => e == null || !e.gameObject.activeSelf);

        if (this.currentLayer == EditLayer.Foreground)
        {
            EntitySelectionManager selection = FindAnyObjectByType<EntitySelectionManager>();
            if (selection != null)
            {
                selection.RefreshSelection();
            }

            EntitySelectionVisualizer visualizer = FindAnyObjectByType<EntitySelectionVisualizer>();
            if (visualizer != null)
            {
                visualizer.Refresh();
            }
        }
        else
        {
            TileSelectionManager tileSelection = FindAnyObjectByType<TileSelectionManager>();
            if (tileSelection != null)
            {
                tileSelection.RefreshSelection();
            }
        }

        if (RuntimePropertyEditor.Instance != null)
        {
            RuntimePropertyEditor.Instance.ForceRebuild();
        }
    }

    private void Awake()
    {
        Instance = this;
        this.tileController = FindAnyObjectByType<TileEditorController>();
        this.entityController = FindAnyObjectByType<EntityEditorController>();
        this.undoRedoManager = FindAnyObjectByType<UndoRedoManager>();
        this.SelectDrag();
        this.SetLayer(EditLayer.Foreground);
    }

    private void SelectTool(EditorTool selectedTool, int selectedIndex)
    {
        this.CurrentTool = selectedTool;

        this.tileController.SetTool(selectedTool);
        this.entityController.SetTool(selectedTool);

        for (int i = 0; i < this.toolOutline.Length; i++)
        {
            this.toolOutline[i].SetActive(i == selectedIndex);
        }

        if (this.activeController != null)
        {
            this.activeController.ResetSelectionTool();
        }
    }

    private IEnumerator WaitForSwitch()
    {
        this.canSwitch = false;
        yield return new WaitForSeconds(0.1f);
        this.canSwitch = true;
    }

    /// <summary>
    /// Fades the entities to the alpha of the layer that is being edited. One tween
    /// drives every renderer instead of one tween per renderer, because a large map
    /// has thousands of them and creating that many tweens freezes the editor.
    /// </summary>
    /// <param name="targetAlpha">The alpha the entities end at.</param>
    private void FadeEntityLayer(float targetAlpha)
    {
        this.layerFadeTween?.Kill();
        this.layerFadeTween = null;

        // Another item of the layer that is already being edited must not fade
        // anything, and the renderers already have the right alpha.
        if (Mathf.Approximately(this.layerAlpha, targetAlpha))
        {
            return;
        }

        this.CollectLayerRenderers();

        this.layerFadeTween = DOTween
            .To(
                () => this.layerAlpha,
                alpha => this.SetLayerAlpha(alpha),
                targetAlpha,
                LayerFadeDuration)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Collects the renderers of the entities that are in the world, including
    /// deactivated ones, so the fade can write to them without searching the scene
    /// every frame and an entity restored by undo already has the layer's alpha.
    /// </summary>
    private void CollectLayerRenderers()
    {
        this.layerRenderers.Clear();

        HashSet<SpriteRenderer> seen = new HashSet<SpriteRenderer>();

        foreach (SaveableEntity entity in
                 FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include))
        {
            foreach (SpriteRenderer renderer in
                     entity.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer == null || !seen.Add(renderer))
                {
                    continue;
                }

                this.layerRenderers.Add(renderer);
            }
        }
    }

    /// <summary>
    /// Writes the layer alpha to every collected renderer, skipping the ones that
    /// already have it and dropping the ones that were destroyed.
    /// </summary>
    /// <param name="alpha">The alpha to apply.</param>
    private void SetLayerAlpha(float alpha)
    {
        this.layerAlpha = alpha;

        for (int i = this.layerRenderers.Count - 1; i >= 0; i--)
        {
            SpriteRenderer renderer = this.layerRenderers[i];

            if (renderer == null)
            {
                this.layerRenderers.RemoveAt(i);
                continue;
            }

            Color color = renderer.color;

            if (Mathf.Approximately(color.a, alpha))
            {
                continue;
            }

            color.a = alpha;
            renderer.color = color;
        }
    }

    private void UpdateLayerButtonVisuals(EditLayer layer)
    {
        bool isForeground = layer == EditLayer.Foreground;

        this.backgroundLayerImage.DOKill();
        this.foregroundLayerImage.DOKill();

        this.backgroundLayerImage.DOFade(
            isForeground ? 0f : 1f,
            this.layerTransitionDuration);

        this.foregroundLayerImage.DOFade(
            isForeground ? 1f : 0f,
            this.layerTransitionDuration);
    }
}
