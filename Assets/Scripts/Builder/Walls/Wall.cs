
using System.Collections;
using UnityEngine;

/// <summary>
/// A wall in the game. Use this class as it is for walls without behaviour,
/// or extend it for walls that do something. The wall has one fixed sprite,
/// and an overlay sprite on a child object that is picked from the
/// neighbouring walls and the saved orientation. Once the walls around it
/// have been registered, the overlay is frozen and never changes again, even
/// when walls are destroyed or spawned next to it.
/// </summary>
public class Wall : SaveableEntity
{
    /// <summary>
    /// The name under which the orientation is stored in the builder entity.
    /// </summary>
    public const string OrientationFieldName = nameof(orientation);

    [Header("Wall")]
    [SerializeField]
    private SpriteRenderer overlayRenderer;

    [SerializeField]
    private float cellSize = 1f;

    [SerializeField]
    private bool rotateSpriteWithOverlay;

    [SaveField]
    [SerializeField]
    private WallOrientation orientation = WallOrientation.Up;

    [Tooltip("Optional. Replaces the base sprite on an outer corner. Used as it is " +
             "on an outer corner (two adjacent connections with a wall on the " +
             "diagonal between them). Only used when Rotate Sprite With Overlay " +
             "is enabled. Draw it in the same orientation as the two-sided " +
             "corner shape in the WallSpriteSet.")]
    [SerializeField]
    private Sprite cornerSprite;

    [Tooltip("Optional. Replaces the base sprite on an inner corner. Used directly " +
             "without additional rotation. An inner corner has two adjacent " +
             "connections with no wall on the diagonal, or connections on all " +
             "four sides with one diagonal missing. Only used when Rotate Sprite " +
             "With Overlay is enabled. Draw it in the orientation you want it " +
             "to appear.")]
    [SerializeField]
    private Sprite innerCornerSprite;

    private Sprite baseSprite;

    private Vector2Int cell;
    private bool registered;
    private bool frozen;
    private bool freezeScheduled;
    private bool overlayResolved;

    private Vector3 baseLocalPosition;
    private bool baseLocalPositionCached;

    /// <summary>
    /// Gets the sprites this wall uses.
    /// </summary>
    public WallSpriteSet SpriteSet => WallSpriteSet.Instance;

    /// <summary>
    /// Gets the renderer that draws the overlay sprite.
    /// </summary>
    public SpriteRenderer OverlayRenderer => this.overlayRenderer;

    /// <summary>
    /// Gets the size of one grid cell in world units.
    /// </summary>
    public float CellSize => this.cellSize;

    /// <summary>
    /// Gets a value indicating whether the base sprite rotates with the overlay.
    /// </summary>
    public bool RotateSpriteWithOverlay => this.rotateSpriteWithOverlay;

    /// <summary>
    /// Gets a value indicating whether the overlay is fixed.
    /// </summary>
    public bool IsFrozen => this.frozen;

    /// <summary>
    /// Gets the optional sprite used instead of the base sprite on outer corners.
    /// </summary>
    public Sprite CornerSprite => this.cornerSprite;

    /// <summary>
    /// Gets the optional sprite used instead of the base sprite on inner corners.
    /// </summary>
    public Sprite InnerCornerSprite => this.innerCornerSprite;

    /// <summary>
    /// Registers the wall once all entities of the map exist.
    /// </summary>
    public override void OnFinishMapLoad()
    {
        base.OnFinishMapLoad();

        this.RegisterInGrid();
    }

    /// <summary>
    /// Fixes the overlay in its current sprite and rotation. Neighbours that
    /// appear or disappear afterwards no longer change it.
    /// </summary>
    public void FreezeOverlay()
    {
        this.frozen = true;
    }

    /// <summary>
    /// Registers the wall when it was not registered by a map load, for
    /// example when it is spawned while playing. Overrides must call the
    /// base method.
    /// </summary>
    protected virtual void Start()
    {
        this.RegisterInGrid();
    }

    /// <summary>
    /// Removes the wall from the grid. The neighbours keep their sprites, so a
    /// wall that is destroyed in the game does not change the walls around it.
    /// Overrides must call the base method.
    /// </summary>
    protected virtual void OnDestroy()
    {
        this.UnregisterFromGrid();
    }

    /// <summary>
    /// Updates the overlay sprite and its rotation to match the neighbouring
    /// walls and the orientation. Does nothing once the overlay is frozen.
    /// </summary>
    protected virtual void RefreshSprite()
    {
        WallSpriteSet spriteSet = WallSpriteSet.Instance;

        if (this.frozen ||
            !this.registered ||
            spriteSet == null ||
            this.overlayRenderer == null)
        {
            return;
        }

        WallConnections connections = WallGrid.Game.GetConnections(this.cell);
        WallCorners corners = WallGrid.Game.GetCorners(this.cell);

        if (!spriteSet.TryGetOverlay(
            connections,
            corners,
            this.orientation,
            out Sprite sprite,
            out Quaternion rotation))
        {
            return;
        }

        this.overlayRenderer.sprite = sprite;
        this.overlayRenderer.transform.localRotation = rotation;
        this.ApplyBaseRotation(spriteSet, rotation, connections, corners);
        this.overlayResolved = true;
    }

    /// <summary>
    /// Puts the wall in the grid cell it is standing in and updates the
    /// sprite of this wall and its neighbours. The overlay is frozen at the
    /// end of the frame, after every wall of the map has registered.
    /// </summary>
    protected void RegisterInGrid()
    {
        Vector2Int newCell =
            WallGrid.ToCell(
                this.transform.position,
                this.cellSize);

        if (this.registered &&
            newCell == this.cell)
        {
            return;
        }

        this.UnregisterFromGrid();

        this.cell = newCell;
        this.registered = true;

        WallGrid.Game.Register(this.cell, this, this.RefreshSprite);

        this.RefreshSprite();

        WallGrid.Game.RefreshNeighbours(this.cell);

        if (!this.frozen &&
            !this.freezeScheduled &&
            this.isActiveAndEnabled)
        {
            this.freezeScheduled = true;
            this.StartCoroutine(this.FreezeAtEndOfFrame());
        }
    }

    /// <summary>
    /// Rotates the base sprite together with the overlay when enabled. On an
    /// outer or inner corner the matching corner sprite is used instead of the
    /// base sprite, when one is set. Inner corner sprites are used directly,
    /// without an additional 180-degree rotation. The sprite is rotated around
    /// its visual center instead of its pivot, so the position is corrected for
    /// the offset between the two.
    /// </summary>
    /// <param name="spriteSet">The sprites that pick the rotations.</param>
    /// <param name="overlayRotation">The rotation that was chosen for the overlay.</param>
    /// <param name="connections">The sides that connect to another wall.</param>
    /// <param name="corners">The diagonals that contain a wall.</param>
    private void ApplyBaseRotation(
        WallSpriteSet spriteSet,
        Quaternion overlayRotation,
        WallConnections connections,
        WallCorners corners)
    {
        if (!this.rotateSpriteWithOverlay ||
            this.SpriteRenderer == null)
        {
            return;
        }

        if (this.baseSprite == null)
        {
            this.baseSprite = this.SpriteRenderer.sprite;
        }

        if (this.baseSprite == null)
        {
            return;
        }

        Quaternion rotation = overlayRotation;
        Sprite chosenSprite = this.baseSprite;

        if (spriteSet.TryGetCornerRotation(
                connections,
                corners,
                this.orientation,
                out WallCornerKind kind,
                out Quaternion cornerRotation))
        {
            rotation = cornerRotation;

            if (kind == WallCornerKind.Outer &&
                this.cornerSprite != null)
            {
                chosenSprite = this.cornerSprite;
            }
            else if (kind == WallCornerKind.Inner &&
                     this.innerCornerSprite != null)
            {
                chosenSprite = this.innerCornerSprite;
            }
        }

        this.SpriteRenderer.sprite = chosenSprite;

        Transform baseTransform = this.SpriteRenderer.transform;

        if (!this.baseLocalPositionCached)
        {
            this.baseLocalPosition = baseTransform.localPosition;
            this.baseLocalPositionCached = true;
        }

        Vector3 center =
            Vector3.Scale(
                this.SpriteRenderer.sprite.bounds.center,
                baseTransform.localScale);

        baseTransform.localRotation = rotation;

        baseTransform.localPosition =
            this.baseLocalPosition + center - (rotation * center);
    }

    private IEnumerator FreezeAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();

        // Only freeze when the overlay was really calculated. Without the
        // sprite set the wall stays open, so it can still be filled in.
        this.frozen = this.overlayResolved;
        this.freezeScheduled = false;
    }

    private void UnregisterFromGrid()
    {
        if (!this.registered)
        {
            return;
        }

        this.registered = false;

        WallGrid.Game.Unregister(this.cell, this);
    }
}
