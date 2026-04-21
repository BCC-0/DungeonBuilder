using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The Builder can create a PlaytestManager for testing the built map.
/// The PlaytestManager saves location and state of the player in the builder to restore quickly.
/// The PlaytestManager goes over switching between builder and crawler scene.
/// </summary>
public class PlaytestManager : MonoBehaviour
{
    [SerializeField]
    private string mapName;

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

    private void StoreBuilder()
    {
        // TODO: Store builder metadata.
    }

    private void RestoreBuilder()
    {
        // TODO: Restore builder metadata.
    }

    private void StartCrawler()
    {
        SceneManager.LoadScene("CrawlerMode");

        SaveManager.LoadMap(this.mapName);

        // TODO: Set play test buttons in crawler.
    }

    private void VerifyMap()
    {
        List<string> errors = new List<string>();

        // TODO: Add all errors to the list here.
        if (errors.Count > 0)
        {
            throw new PlaytestException(errors);
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(this);
    }
}
