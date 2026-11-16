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

    [SaveField]
    [SerializeField]
    private WallOrientation orientation = WallOrientation.Down;

    private Vector2Int cell;
    private bool registered;
    private bool frozen;
    private bool freezeScheduled;
    private bool overlayResolved;

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
    /// Gets a value indicating whether the overlay is fixed.
    /// </summary>
    public bool IsFrozen => this.frozen;

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

        this.overlayRenderer.sprite =
            spriteSet.GetOverlay(
                WallGrid.Game.GetConnections(this.cell),
                this.orientation);

        this.overlayRenderer.transform.localRotation =
            WallSpriteSet.GetRotation(this.orientation);

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