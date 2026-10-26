using UnityEngine;

/// <summary>
/// A simple pitchfork weapon.
/// </summary>
[CreateAssetMenu(fileName = "NewPitchfork", menuName = "Items/Weapons/Pitchfork")]
public class Pitchfork : Weapon
{
    [Header("Directional Hitboxes")]
    [SerializeField]
    private HitboxSettings hitboxUp;
    [SerializeField]
    private HitboxSettings hitboxDown;
    [SerializeField]
    private HitboxSettings hitboxLeft;
    [SerializeField]
    private HitboxSettings hitboxRight;

    private BoxCollider2D collider;

    /// <summary>
    /// Gets the box collider of the pitchfork.
    /// </summary>
    public override void Initialize()
    {
        this.collider = this.Prefab.GetComponent<BoxCollider2D>();
    }

    /// <summary>
    /// The actual attack logic for the pitchfork.
    /// </summary>
    /// <param name="direction">The direction to attack in.</param>
    /// <param name="playerHandler">The playerhandler that attacks.</param>
    protected override void PerformAttack(Vector2 direction, CrawlerPlayerHandler playerHandler)
    {
        HitboxSettings settings = this.GetHitboxSettings(direction);

        this.collider.size = this.GetHitboxSettings(direction).Size;

    }

    private HitboxSettings GetHitboxSettings(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x > 0 ? this.hitboxRight : this.hitboxLeft;
        }

        return direction.y > 0 ? this.hitboxUp : this.hitboxDown;
    }

    /// <summary>
    /// The settings for the pitchfork collider.
    /// </summary>
    [System.Serializable]
    public struct HitboxSettings
    {
        /// <summary>
        /// The size of the hitbox.
        /// </summary>
        public Vector2 Size;

        /// <summary>
        /// The offset of the hitbox.
        /// </summary>
        public Vector2 Offset;
    }
}
