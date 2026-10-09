using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Manages playtesting and restores the builder state after returning
/// from crawler mode.
/// </summary>
public class PlaytestManager : MonoBehaviour
{
    private static PlaytestManager instance;

    [SerializeField]
    private string mapName;

    private BuilderState storedBuilderState;

    private TileEditorController tileEditorController;
    private EntityEditorController entityEditorController;
    private InventoryController inventoryController;

    /// <summary>
    /// Gets the instance of the PlaytestManager.
    /// </summary>
    public static PlaytestManager Instance
    {
        get => instance;
        private set => instance = value;
    }

    /// <summary>
    /// Gets or sets the map name we are in.
    /// </summary>
    public string MapName
    {
        get => this.mapName;
        set => this.mapName = value;
    }

    /// <summary>
    /// Validates the map, saves it, captures the builder state,
    /// and starts the crawler scene.
    /// </summary>
    public void PlayTest()
    {
        try
        {
            this.VerifyMap();

            SaveManager.SaveBuilderMap(this.mapName);

            this.StoreBuilder();

            this.StartCrawler();
        }
        catch (PlaytestException exception)
        {
            Debug.LogError(exception);
        }
    }

    /// <summary>
    /// Returns to builder mode.
    /// </summary>
    public void StopPlaytest()
    {
        this.StartCoroutine(this.LoadBuilderAsync());
    }

    /// <summary>
    /// Returns to builder mode, using the current camera position.
    /// </summary>
    public void EditHere()
    {
        if (Camera.main != null)
        {
            this.storedBuilderState.CameraPosition =
                Camera.main.transform.position;
        }

        this.StartCoroutine(this.LoadBuilderAsync());
    }

    /// <summary>
    /// Starts crawler mode.
    /// </summary>
    public void StartCrawler()
    {
        this.StartCoroutine(this.LoadCrawlerAsync());

        // TODO: Add a loading scene screen here.
    }

    /// <summary>
    /// Captures the builder camera, editor, and inventory state.
    /// </summary>
    private void StoreBuilder()
    {
        Camera cam = Camera.main;

        this.tileEditorController =
            FindAnyObjectByType<TileEditorController>();

        this.entityEditorController =
            FindAnyObjectByType<EntityEditorController>();

        this.inventoryController =
            FindAnyObjectByType<InventoryController>();

        this.storedBuilderState = new BuilderState
        {
            CameraPosition = cam != null
                ? cam.transform.position
                : new Vector3(0, 0, -10),

            CameraSize = cam != null
                ? cam.orthographicSize
                : 5f,

            SelectedTile =
                this.tileEditorController?.SelectedTile,

            SelectedPrefab =
                this.entityEditorController?.SelectedPrefab,

            ActiveLayer =
                MapEditorManager.Instance.CurrentLayer,

            ActiveTool =
                MapEditorManager.Instance.CurrentTool,

            InventoryItems =
                this.inventoryController != null
                    ? this.inventoryController.GetSlotItems()
                    : null,

            InventorySizes =
                this.inventoryController != null
                    ? this.inventoryController.GetSlotInventorySizes()
                    : null,

            SelectedSlot =
                this.inventoryController != null
                    ? this.inventoryController.GetSelectedSlot()
                    : -1,
        };
    }

    /// <summary>
    /// Restores the builder map, camera, editor selections,
    /// toolbar contents, and active editor layer/tool.
    /// </summary>
    private void RestoreBuilder()
    {
        // Reacquire controllers because the previous scene was destroyed.
        this.tileEditorController =
            FindAnyObjectByType<TileEditorController>();

        this.entityEditorController =
            FindAnyObjectByType<EntityEditorController>();

        this.inventoryController =
            FindAnyObjectByType<InventoryController>();

        MapEditorManager.Instance.MapName = this.mapName;

        SaveManager.LoadBuilderMap(this.mapName);

        Camera cam = Camera.main;

        if (cam != null)
        {
            cam.transform.position =
                this.storedBuilderState.CameraPosition;

            cam.orthographicSize =
                this.storedBuilderState.CameraSize;
        }

        if (this.tileEditorController != null)
        {
            this.tileEditorController.SelectedTile =
                this.storedBuilderState.SelectedTile;
        }

        if (this.entityEditorController != null)
        {
            this.entityEditorController.SelectedPrefab =
                this.storedBuilderState.SelectedPrefab;
        }

        // Restore the toolbar after its scene objects have been recreated.
        if (this.inventoryController != null &&
            this.storedBuilderState.InventoryItems != null)
        {
            this.inventoryController.RestoreSlotItems(
                this.storedBuilderState.InventoryItems,
                this.storedBuilderState.InventorySizes,
                this.storedBuilderState.SelectedSlot);
        }

        switch (this.storedBuilderState.ActiveTool)
        {
            case EditorTool.Drag:
                MapEditorManager.Instance.SelectDrag();
                break;

            case EditorTool.Brush:
                MapEditorManager.Instance.SelectBrush();
                break;

            case EditorTool.Eraser:
                MapEditorManager.Instance.SelectEraser();
                break;

            case EditorTool.Selection:
                MapEditorManager.Instance.SelectSelection();
                break;
        }
    }

    /// <summary>
    /// Loads the crawler scene and then loads the map into it.
    /// </summary>
    /// <returns>The coroutine enumerator.</returns>
    private IEnumerator LoadCrawlerAsync()
    {
        AsyncOperation op =
            SceneManager.LoadSceneAsync("CrawlerMode");

        while (!op.isDone)
        {
            yield return null;
        }

        SaveManager.LoadMap(this.mapName);
    }

    /// <summary>
    /// Loads the builder scene, waits for its UI layout to settle,
    /// and then restores the stored builder state.
    /// </summary>
    /// <returns>The coroutine enumerator.</returns>
    private IEnumerator LoadBuilderAsync()
    {
        AsyncOperation op =
            SceneManager.LoadSceneAsync("BuilderMode");

        while (!op.isDone)
        {
            yield return null;
        }

        // Wait for scene Start methods to initialize.
        yield return null;
        yield return null;

        // Wait for Unity to finish calculating UI layout.
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        this.RestoreBuilder();

        // Allow restored UI changes to be laid out.
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        MapEditorManager.Instance.SetLayer(
            this.storedBuilderState.ActiveLayer);
    }

    /// <summary>
    /// Validates the builder map and collects all problems found.
    /// </summary>
    /// <exception cref="PlaytestException">Thrown when the map has one or more errors.</exception>
    private void VerifyMap()
    {
        List<string> errors = new List<string>();

        int playerCount = 0;

        foreach (BuilderEntity entity in BuilderRegistry.GetAll())
        {
            if (entity.CompareTag("PlayerEntity"))
            {
                playerCount++;
            }
        }

        foreach (BuilderEntity entity in BuilderRegistry.GetAll())
        {
            if (entity.PrefabID != null &&
                SaveRegistry.GetPrefab(entity.PrefabID)
                    ?.GetComponent<ItemObject>() != null)
            {
                FieldInfo f = typeof(BuilderEntity).GetField(
                    "originalItem",
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);

                Item item = f != null
                    ? (Item)f.GetValue(entity)
                    : null;

                if (item == null ||
                    string.IsNullOrEmpty(item.ItemID))
                {
                    errors.Add(
                        $"Item entity '{entity.PrefabID}' at " +
                        $"{entity.transform.position} has no valid ItemID.");
                }
            }
        }

        if (playerCount == 0)
        {
            errors.Add(
                "Map must contain exactly one player spawn point. " +
                "Currently there are none.");
        }
        else if (playerCount > 1)
        {
            errors.Add(
                $"Map must contain exactly one player spawn point. " +
                $"Currently there are {playerCount}.");
        }

        this.VerifyAttributes(errors);

        if (errors.Count > 0)
        {
            throw new PlaytestException(errors);
        }
    }

    /// <summary>
    /// Checks the reference counts of all fields marked with SaveFieldAttribute.
    /// </summary>
    /// <param name="errors">The list that validation errors are added to.</param>
    private void VerifyAttributes(List<string> errors)
    {
        foreach (BuilderEntity entity in BuilderRegistry.GetAll())
        {
            FieldInfo[] fields = entity.GetType().GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (FieldInfo field in fields)
            {
                SaveFieldAttribute attribute =
                    field.GetCustomAttribute<SaveFieldAttribute>();

                if (attribute == null)
                {
                    continue;
                }

                if (typeof(SaveableEntity).IsAssignableFrom(
                    field.FieldType))
                {
                    SaveableEntity reference =
                        field.GetValue(entity) as SaveableEntity;

                    int referenceCount =
                        reference != null ? 1 : 0;

                    this.VerifyReferenceCount(
                        entity,
                        field,
                        attribute,
                        referenceCount,
                        errors);

                    continue;
                }

                if (typeof(IList).IsAssignableFrom(field.FieldType))
                {
                    IList list =
                        field.GetValue(entity) as IList;

                    int referenceCount =
                        list != null ? list.Count : 0;

                    this.VerifyReferenceCount(
                        entity,
                        field,
                        attribute,
                        referenceCount,
                        errors);
                }
            }
        }
    }

    /// <summary>
    /// Adds an error if a field has fewer or more references than its attribute allows.
    /// </summary>
    /// <param name="entity">The entity that owns the field.</param>
    /// <param name="field">The field being checked.</param>
    /// <param name="attribute">The attribute holding the minimum and maximum reference counts.</param>
    /// <param name="referenceCount">The number of references the field currently holds.</param>
    /// <param name="errors">The list that validation errors are added to.</param>
    private void VerifyReferenceCount(
        BuilderEntity entity,
        FieldInfo field,
        SaveFieldAttribute attribute,
        int referenceCount,
        List<string> errors)
    {
        if (referenceCount < attribute.MinReferences)
        {
            errors.Add(
                $"Entity '{entity.name}' requires at least " +
                $"{attribute.MinReferences} reference(s) for " +
                $"'{field.Name}', but has {referenceCount}.");
        }

        if (attribute.MaxReferences >= 0 &&
            referenceCount > attribute.MaxReferences)
        {
            errors.Add(
                $"Entity '{entity.name}' allows at most " +
                $"{attribute.MaxReferences} reference(s) for " +
                $"'{field.Name}', but has {referenceCount}.");
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);

        this.storedBuilderState = new BuilderState
        {
            CameraPosition = new Vector3(0, 0, -10),
            CameraSize = 5f,
            ActiveLayer = EditLayer.Foreground,
            ActiveTool = EditorTool.Drag,
            SelectedSlot = -1,
        };

        // Capture the initial scene's controllers.
        this.tileEditorController =
            FindAnyObjectByType<TileEditorController>();

        this.entityEditorController =
            FindAnyObjectByType<EntityEditorController>();

        this.inventoryController =
            FindAnyObjectByType<InventoryController>();

        this.RestoreBuilder();
    }

    private struct BuilderState
    {
        public Vector3 CameraPosition;
        public float CameraSize;

        public TileBase SelectedTile;
        public GameObject SelectedPrefab;

        public EditLayer ActiveLayer;
        public EditorTool ActiveTool;

        public InventoryItem[] InventoryItems;
        public Vector2[] InventorySizes;
        public int SelectedSlot;
    }
}
