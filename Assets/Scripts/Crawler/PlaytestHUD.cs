using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the playtest UI when playtesting in crawler mode.
/// </summary>
public class PlaytestHUD : MonoBehaviour
{
    private PlaytestManager playtester;

    /// <summary>
    /// Returns the player back to the builder mode.
    /// </summary>
    public void StopPlayTest()
    {
        if (this.playtester != null)
        {
            this.playtester.StopPlaytest();
        }
    }

    /// <summary>
    /// Restarts the map in the crawler mode from the beginning.
    /// </summary>
    public void RestartPlaytest()
    {
        if (this.playtester != null)
        {
            this.playtester.StartCrawler();
        }
    }

    /// <summary>
    /// Returns the player back to the builder mode with the given camera position.
    /// </summary>
    public void EditHere()
    {
        if (this.playtester != null)
        {
            this.playtester.EditHere();
        }
    }

    private void Start()
    {
        this.playtester = FindAnyObjectByType<PlaytestManager>();
        this.gameObject.SetActive(this.playtester != null);
    }
}