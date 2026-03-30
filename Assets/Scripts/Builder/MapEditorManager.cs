using System.Collections.Generic;
using DG.Tweening;
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

    [SerializeField]
    private GameObject[] toolOutline;

    [SerializeField]
    private string mapName;

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

        Debug.Log("Selected " + this.CurrentLayer);

        // Determine target alpha
        float targetAlpha = this.CurrentLayer == EditLayer.Foreground ? 1f : 0.2f;

        foreach (var entity in FindObjectsByType<SaveableEntity>())
        {
            SpriteRenderer sr = entity.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Kill any existing tween on the color to avoid conflicts
                sr.DOKill();

                sr.DOFade(targetAlpha, 0.25f);
            }
        }
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

    private void Awake()
    {
        Instance = this;
        this.SelectDrag();
    }

    private void Start()
    {
        // After awake, to give the registry time to register all objects.
        SaveManager.LoadBuilderMap(this.mapName);
    }

    private void SelectTool(EditorTool selectedTool, int selectedIndex)
    {
        this.CurrentTool = selectedTool;

        for (int i = 0; i < 4; i++)
        {
            this.toolOutline[i].SetActive(i == selectedIndex ? true : false);
        }
    }
}