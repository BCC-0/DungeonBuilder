using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// The Builder can create a PlaytestManager for testing the built map.
/// The PlaytestManager saves location and state of the player in the builder to restore quickly.
/// The PlaytestManager goes over switching between builder and crawler scene.
/// </summary>
public class PlaytestManager : MonoBehaviour
{
    [SerializeField]
    private string mapName;

    private BuilderState storedBuilderState;

    /// <summary>
    /// Gets or sets the map name we are in.
    /// </summary>
    public string MapName
    {
        get => this.mapName; set { this.mapName = value; }
    }

    /// <summary>
    /// Verifies if the map is playable first.
    /// If it is, saves the map as it currently is and starts the Crawler scene.
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
    /// Returns the player back to the builder mode.
    /// </summary>
    public void StopPlaytest()
    {
        this.StartCoroutine(this.LoadBuilderAsync());
    }

    /// <summary>
    /// Returns the player back to the builder mode with the given camera position.
    /// </summary>
    public void EditHere()
    {
        this.storedBuilderState.CameraPosition = Camera.main.transform.position;
        this.StartCoroutine(this.LoadBuilderAsync());

        // TODO: RestoreBuilder() should be called once the builder scene finishes loading
    }

    /// <summary>
    /// Starts the crawler with the currently loaded map.
    /// </summary>
    public void StartCrawler()
    {
        this.StartCoroutine(this.LoadCrawlerAsync());

        // TODO: Add a loading scene screen here.
    }

    private void StoreBuilder()
    {
        var cam = Camera.main;
        this.storedBuilderState = new BuilderState
        {
            CameraPosition = cam.transform.position,
            CameraSize = cam.orthographicSize,
            SelectedTile = FindAnyObjectByType<TileEditorController>()?.SelectedTile,
            SelectedPrefab = FindAnyObjectByType<EntityEditorController>()?.SelectedPrefab,
            ActiveLayer = MapEditorManager.Instance.CurrentLayer,
            ActiveTool = MapEditorManager.Instance.CurrentTool,
        };
    }

    private void RestoreBuilder()
    {
        var cam = Camera.main;
        cam.transform.position = this.storedBuilderState.CameraPosition;
        cam.orthographicSize = this.storedBuilderState.CameraSize;

        var tileEditor = FindAnyObjectByType<TileEditorController>();
        if (tileEditor != null)
        {
            tileEditor.SelectedTile = this.storedBuilderState.SelectedTile;
        }

        var entityEditor = FindAnyObjectByType<EntityEditorController>();
        if (entityEditor != null)
        {
            entityEditor.SelectedPrefab = this.storedBuilderState.SelectedPrefab;
        }

        if (MapEditorManager.Instance.CurrentLayer != this.storedBuilderState.ActiveLayer)
        {
            MapEditorManager.Instance.ToggleLayer();
        }

        switch (this.storedBuilderState.ActiveTool)
        {
            case EditorTool.Drag: MapEditorManager.Instance.SelectDrag(); break;
            case EditorTool.Brush: MapEditorManager.Instance.SelectBrush(); break;
            case EditorTool.Eraser: MapEditorManager.Instance.SelectEraser(); break;
            case EditorTool.Selection: MapEditorManager.Instance.SelectSelection(); break;
        }
    }

    private IEnumerator LoadCrawlerAsync()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync("CrawlerMode");

        while (!op.isDone)
        {
            yield return null;
        }

        SaveManager.LoadMap(this.mapName);
    }

    private IEnumerator LoadBuilderAsync()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync("BuilderMode");

        while (!op.isDone)
        {
            yield return null;
        }

        // Wait extra frame for setup.
        yield return null;

        // Loading is already done by MapEditorManager.
        this.RestoreBuilder();
    }

    private void VerifyMap()
    {
        List<string> errors = new List<string>();

        int playerCount = 0;
        foreach (var entity in BuilderRegistry.GetAll())
        {
            if (entity.CompareTag("PlayerEntity"))
            {
                playerCount++;
            }
        }

        if (playerCount == 0)
        {
            errors.Add("Map must contain exactly one player spawn point. Currently there are none.");
        }
        else if (playerCount > 1)
        {
            errors.Add($"Map must contain exactly one player spawn point. Currently there are {playerCount}.");
        }

        // TODO: Call more check methods for checking if the map is legal.
        if (errors.Count > 0)
        {
            throw new PlaytestException(errors);
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(this);
    }

    private struct BuilderState
    {
        public Vector3 CameraPosition;
        public float CameraSize;
        public TileBase SelectedTile;
        public GameObject SelectedPrefab;
        public EditLayer ActiveLayer;
        public EditorTool ActiveTool;
    }
}
