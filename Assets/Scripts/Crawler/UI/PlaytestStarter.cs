using UnityEngine;

/// <summary>
/// Utility script to start the playtester.
/// </summary>
public class PlaytestStarter : MonoBehaviour
{
    /// <summary>
    /// Starts the playtester.
    /// </summary>
    public void PlaytestStart()
    {
        PlaytestManager.Instance.PlayTest();
    }
}
