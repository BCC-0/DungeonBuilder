using System.Collections.Generic;
using UnityEngine;

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
    private static MapEditorManager instance;
    private EditorTool currentTool;
    private EditLayer currentLayer;

    private SaveableEntity selectedEntity;
    private List<SaveableEntity> selectedEntities = new List<SaveableEntity>();

    /// <summary>
    /// Gets the instance of the MapEditorManager.
    /// </summary>
    public static MapEditorManager Instance
    {
        get => instance;
        private set => instance = value;
    }

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
    /// Gets or sets the selected entity.
    /// </summary>
    public SaveableEntity SelectedEntity
    {
        get => this.selectedEntity;
        set => this.selectedEntity = value;
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
    /// Switched the editing layer between fore- and background.
    /// </summary>
    public void ToggleLayer()
    {
        this.CurrentLayer = this.CurrentLayer == EditLayer.Background
            ? EditLayer.Foreground
            : EditLayer.Background;
    }

    private void Awake()
    {
        Instance = this;
        this.CurrentTool = EditorTool.Brush;
    }
}