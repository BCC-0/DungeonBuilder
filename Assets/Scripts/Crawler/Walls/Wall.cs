using UnityEngine;

/// <summary>
/// A wall in the game. Use this class as it is for walls without behaviour,
/// or extend it for walls that do something. The sprite follows the
/// neighbouring walls, and the saved orientation is used when there are none.
/// </summary>
public class Wall : SaveableEntity
{
    /// <summary>
    /// The name under which the orientation is stored in the builder entity.
    /// </summary>
    public const string OrientationFieldName = nameof(orientation);

    [Header("Wall")]
    [SerializeField]
    private WallSpriteSet spriteSet;

    [SerializeField]
    private float cellSize = 1f;

    [SaveField]
    [SerializeField]
    private WallOrientation orientation = WallOrientation.Down;

    private Vector2Int cell;
    private bool registered;

    /// <summary>
    /// Gets the sprites this wall uses.
    /// </summary>
    public WallSpriteSet SpriteSet => this.spriteSet;

    /// <summary>
    /// Gets the size of one grid cell in world units.
    /// </summary>
    public float CellSize => this.cellSize;

    /// <summary>
    /// Registers the wall once all entities of the map exist.
    /// </summary>
    public override void OnFinishMapLoad()
    {
        base.OnFinishMapLoad();

        this.RegisterInGrid();
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
    /// Updates the sprite to match the neighbouring walls.
    /// </summary>
    protected virtual void RefreshSprite()
    {
        if (!this.registered ||
            this.spriteSet == null ||
            this.SpriteRenderer == null)
        {
            return;
        }

        Sprite sprite =
            this.spriteSet.GetSprite(
                WallGrid.Game.GetConnections(this.cell),
                this.orientation);

        if (sprite != null)
        {
            this.SpriteRenderer.sprite = sprite;
        }
    }

    /// <summary>
    /// Puts the wall in the grid cell it is standing in and updates the
    /// sprite of this wall and its neighbours. Call it again after moving
    /// the wall.
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
