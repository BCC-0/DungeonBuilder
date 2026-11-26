using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows warnings in the builder UI. Any script can add a warning by calling
/// <see cref="Warn"/>, so no reference to the manager is needed.
/// Add one component to the warnings object in the scene and assign the
/// warning prefab.
/// </summary>
public class WarningManager : MonoBehaviour
{
    /// <summary>
    /// The first line of every warning. The warning font only has capital
    /// letters, so it is drawn as "WARNING:".
    /// </summary>
    private const string Prefix = "Warning:\n";

    private readonly List<WarningMessage> warnings = new List<WarningMessage>();

    [SerializeField]
    [Tooltip("The warning prefab with a WarningMessage component on its root.")]
    private WarningMessage warningPrefab;

    [SerializeField]
    [Tooltip("Optional. New warnings are parented here. Defaults to this object.")]
    private RectTransform warningParent;

    [SerializeField]
    [Tooltip("Seconds a warning takes to fade in.")]
    private float fadeInDuration = 0.2f;

    [SerializeField]
    [Tooltip("Seconds a warning stays fully visible, not counting the extra time per character.")]
    private float visibleDuration = 2.5f;

    [SerializeField]
    [Tooltip("Extra visible seconds per character, so a long warning stays up longer.")]
    private float visibleSecondsPerCharacter = 0.025f;

    [SerializeField]
    [Tooltip("Seconds a warning takes to fade out.")]
    private float fadeOutDuration = 0.4f;

    [SerializeField]
    [Tooltip("Space between two stacked warnings.")]
    private float spacing = 8f;

    [SerializeField]
    [Tooltip("Distance between the lowest warning and the bottom of the parent.")]
    private float bottomPadding = 0f;

    [SerializeField]
    [Tooltip("How many warnings are on screen at once. The oldest one leaves when a new one arrives.")]
    private int maxVisible = 4;

    /// <summary>
    /// Gets the warning manager of the current scene, or null when the scene
    /// does not have one.
    /// </summary>
    public static WarningManager Instance { get; private set; }

    /// <summary>
    /// Adds a warning to the screen. The warning always starts with
    /// "Warning:" on its own line.
    /// </summary>
    /// <param name="message">The warning text, without the prefix.</param>
    public static void Warn(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (Instance == null)
        {
            Debug.LogWarning(message);
            return;
        }

        Instance.Push(message);
    }

    /// <summary>
    /// Creates a message from the warning prefab and places it at the bottom of
    /// the warnings object, pushing the warnings that are already there up.
    /// </summary>
    /// <param name="message">The warning text, without the prefix.</param>
    public void Push(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (this.warningPrefab == null)
        {
            Debug.LogWarning(
                $"WarningManager: No warning prefab is assigned. {message}",
                this);

            return;
        }

        RectTransform parent = this.warningParent != null
            ? this.warningParent
            : this.transform as RectTransform;

        if (parent == null)
        {
            Debug.LogWarning(
                $"WarningManager: The warnings need a RectTransform parent. {message}",
                this);

            return;
        }

        // The oldest warning leaves right away, so a burst of warnings never
        // fills more than maxVisible rows.
        while (this.warnings.Count >= Mathf.Max(1, this.maxVisible))
        {
            this.warnings[0].Dismiss();
        }

        WarningMessage warning = Instantiate(this.warningPrefab, parent);
        warning.name = this.warningPrefab.name;
        warning.Finished += this.HandleFinished;
        warning.SetMessage(Prefix + message);

        this.warnings.Add(warning);
        this.Reposition();

        warning.Show(
            this.fadeInDuration,
            this.GetVisibleDuration(message),
            this.fadeOutDuration);
    }

    /// <summary>
    /// Places the warnings from the bottom of the parent upwards, with the
    /// newest one closest to the bottom.
    /// </summary>
    private void Reposition()
    {
        float y = this.bottomPadding;

        for (int i = this.warnings.Count - 1; i >= 0; i--)
        {
            RectTransform rect = this.warnings[i].Rect;
            this.warnings[i].SetBasePosition(y);
            y += rect.rect.height + this.spacing;
        }
    }

    /// <summary>
    /// Removes a message that faded out and moves the remaining warnings down.
    /// </summary>
    /// <param name="warning">The message that is done.</param>
    private void HandleFinished(WarningMessage warning)
    {
        if (warning == null ||
            !this.warnings.Remove(warning))
        {
            return;
        }

        warning.Finished -= this.HandleFinished;

        Destroy(warning.gameObject);
        this.Reposition();
    }

    /// <summary>
    /// Gives longer warnings more time on screen, so they can be read.
    /// </summary>
    /// <param name="message">The warning text, without the prefix.</param>
    /// <returns>The seconds the warning stays fully visible.</returns>
    private float GetVisibleDuration(string message)
    {
        return this.visibleDuration +
               (message.Length * this.visibleSecondsPerCharacter);
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
