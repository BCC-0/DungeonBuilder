using UnityEngine;

/// <summary>
/// A potion that applies status effects to the player and optionally heals.
/// Create one asset per effect for the simplest way to apply status effects
/// during play.
/// </summary>
[CreateAssetMenu(fileName = "NewPotion", menuName = "Items/Consumables/Potion")]
public class PotionItem : ConsumableItem
{
    [Header("Potion Effects")]
    [SerializeField]
    private StatusEffectApplication[] effects;

    [SerializeField]
    [Min(0)]
    private int healAmount;

    /// <summary>
    /// Gets the effects this potion applies.
    /// </summary>
    public StatusEffectApplication[] Effects => this.effects;

    /// <summary>
    /// Applies the effects of this potion to the player that uses it.
    /// </summary>
    /// <param name="playerHandler">The player handler that uses the potion.</param>
    /// <param name="playerData">The player data that receives the effects.</param>
    public override void Use(CrawlerPlayerHandler playerHandler, CrawlerPlayerData playerData)
    {
        if (playerData == null)
        {
            return;
        }

        playerData.ApplyStatusEffects(this.effects);

        if (this.healAmount > 0)
        {
            playerData.Heal(this.healAmount);
        }
    }
}
