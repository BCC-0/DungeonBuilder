
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Exposes simple methods for the Game Over buttons to run.
/// </summary>
public class GameOver : MonoBehaviour
{
    private PlaytestManager playtester;

    /// <summary>
    /// Restarts the crawler, either through the playtest manager
    /// or by reloading the current scene.
    /// </summary>
    public void Retry()
    {
        if (this.playtester != null)
        {
            this.playtester.StartCrawler();
        }
        else
        {
            // TODO: Find context behaviour to reload map correctly.
            Debug.Log("RETRY");
        }
    }

    /// <summary>
    /// Quits back to either the crawler, or finds the enter crawler context and goes back to that screen.
    /// </summary>
    public void Quit()
    {
        if (this.playtester != null)
        {
            this.playtester.StopPlaytest();
        }
        else
        {
            // TODO: Find context behaviour to quit map to last screen.
            Debug.Log("QUIT");
        }
    }

    private void Start()
    {
        this.playtester = FindAnyObjectByType<PlaytestManager>();
    }
}
