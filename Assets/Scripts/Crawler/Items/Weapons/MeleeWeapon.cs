using UnityEngine;

/// <summary>
/// A weapon that damages targets through a hitbox on its prefab.
/// </summary>
[CreateAssetMenu(fileName = "NewMeleeWeapon", menuName = "Items/Weapons/Melee Weapon")]
public class MeleeWeapon : Weapon
{
    [Header("Melee Settings")]
    [SerializeField]
    private float activeDuration = 0.2f;

    private WeaponHitbox hitbox;

    /// <summary>
    /// Gets the weapon hitbox of the prefab.
    /// </summary>
    public override void Initialize()
    {
        // Handled via Equip
    }

    /// <summary>
    /// Equips and instantiates the weapon prefab under the specified parent placeholder.
    /// </semantics>
    /// <param name="weaponPlaceholder">The place where the weapon prefab should exist</param>
    public override void Equip(Transform weaponPlaceholder)
    {
        base.Equip(weaponPlaceholder);

        if (this.SpawnedInstance != null)
        {
            this.hitbox = this.SpawnedInstance.GetComponent<WeaponHitbox>();
        }
    }

    /// <summary>
    /// Unequips and destroys the spawned weapon instance.
    /// </summary>
    public override void Unequip()
    {
        base.Unequip();
        this.hitbox = null;
    }

    /// <summary>
    /// Activates the hitbox for the attack duration.
    /// </summary>
    /// <param name="direction">The direction to attack in.</param>
    /// <param name="playerHandler">The playerhandler that attacks.</param>
    protected override void PerformAttack(Vector2 direction, CrawlerPlayerHandler playerHandler)
    {
        if (this.hitbox != null)
        {
            this.hitbox.Activate(this.CreateDamage(direction), this.activeDuration);
        }
        else
        {
            Debug.LogError("MeleeWeapon hitbox is missing! Make sure the weapon was equipped properly.");
        }
    }
}