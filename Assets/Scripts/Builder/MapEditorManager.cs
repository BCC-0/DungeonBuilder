using System.Collections;
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

    private EditorControllerBase tileController;
    private EditorControllerBase entityController;

    private EditorTool currentTool;
    private EditLayer currentLayer;

    private List<Vector3Int> selectedTiles = new List<Vector3Int>();
    private List<SaveableEntity> selectedEntities = new List<SaveableEntity>();

    [SerializeField]
    private GameObject[] toolOutline;

    private EditorControllerBase activeController;

    private bool canSwitch = true;

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
    /// Gets the active controller.
    /// </summary>
    public EditorControllerBase ActiveController => this.activeController;

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
    public List<Vector3Int> SelectedTiles
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
        this.currentLayer = layer;

        this.activeController =
            layer == EditLayer.Background
                ? this.tileController
                : this.entityController;

        this.StartCoroutine(this.WaitForSwitch());

        float targetAlpha = layer == EditLayer.Foreground ? 1f : 0.2f;

        foreach (SaveableEntity entity in FindObjectsByType<SaveableEntity>())
        {
            SpriteRenderer sr = entity.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.DOKill();
                sr.DOFade(targetAlpha, 0.25f);
            }
        }
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
        this.tileController = FindAnyObjectByType<TileEditorController>();
        this.entityController = FindAnyObjectByType<EntityEditorController>();
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
    }

    private IEnumerator WaitForSwitch()
    {
        this.canSwitch = false;
        yield return new WaitForSeconds(0.1f);
        this.canSwitch = true;
    }
}