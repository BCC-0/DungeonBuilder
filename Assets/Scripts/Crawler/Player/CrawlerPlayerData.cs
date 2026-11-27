using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contains all player-related stats and inventory data.
/// Handles equipped items, health, and inventory management.
/// </summary>
public class CrawlerPlayerData : StatusEffectEntity
{
    [Header("Player Attributes")]
    [SerializeField]
    [SaveField(1, 200, displayName: "health", description: "The max amount of health the player can have.")]
    private int maxHealth = 100;

    [SerializeField]
    private int currentHealth = 100;

    [SerializeField]
    private Slider healthSlider;
    [SerializeField]
    private SpriteFontText healthText;

    [SerializeField]
    [SaveField(1, 12, displayName: "speed", description: "The speed the player walks at.")]
    private float moveSpeed = 5f;

    [Header("Inventory")]
    private List<Item> inventory = new List<Item>();

    [SerializeField]
    private GameObject inventoryCanvas;

    [SerializeField]
    private SpriteFontText inventoryDescriptionText;

    [SerializeField]
    private InventoryUI inventoryWeaponsUI;
    [SerializeField]
    private InventoryUI inventoryToolsUI;

    [Header("Equipped Items")]
    [SerializeField]
    private Weapon equippedWeapon;

    [SerializeField]
    private Tool equippedTool;

    [Header("Equipment Placeholders")]
    [SerializeField]
    private Transform weaponPlaceholder;

    [SerializeField]
    private Transform toolPlaceholder;

    private float lastUseTime = 0;

    /// <summary>
    /// Gets the player's maximum health.
    /// </summary>
    public int MaxHealth => this.maxHealth;

    /// <summary>
    /// Gets the player's current health.
    /// </summary>
    public int CurrentHealth => this.currentHealth;

    /// <summary>
    /// Gets the player's move speed, including status effect multipliers. It
    /// is zero while a status effect prevents movement.
    /// </summary>
    public float MoveSpeed
    {
        get
        {
            if (this.StatusEffects == null)
            {
                return this.moveSpeed;
            }

            if (this.StatusEffects.PreventsMovement)
            {
                return 0f;
            }

            return this.moveSpeed * this.StatusEffects.MoveSpeedMultiplier;
        }
    }

    /// <summary>
    /// Gets the player's inventory.
    /// </summary>
    public List<Item> Inventory => this.inventory;

    /// <summary>
    /// Gets whether the inventory is currently open.
    /// </summary>
    public bool IsInventoryOpen => this.inventoryCanvas.activeSelf;

    /// <summary>
    /// Gets the currently equipped weapon.
    /// </summary>
    public Weapon EquippedWeapon => this.equippedWeapon;

    /// <summary>
    /// Gets the currently equipped tool.
    /// </summary>
    public Tool EquippedTool => this.equippedTool;

    /// <summary>
    /// Gets the last use time.
    /// </summary>
    public float LastUseTime => this.lastUseTime;

    /// <summary>
    /// Gets the team of the player. Reflected damage is dealt with it.
    /// </summary>
    public override Team Team => Team.Player;

    /// <summary>
    /// Heals the player.
    /// </summary>
    /// <param name="amount">Amount to heal.</param>
    /// <returns>The amount of health left after taking damage.</returns>
    public int Heal(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        this.currentHealth = Mathf.Min(this.currentHealth + amount, this.maxHealth);
        this.SetHealthSlider();
        return this.currentHealth;
    }

    /// <summary>
    /// Damages the player.
    /// </summary>
    /// <param name="amount">Amount of damage.</param>
    /// <returns>The amount of health left after taking damage.</returns>
    public int TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        this.currentHealth = Mathf.Max(this.currentHealth - amount, 0);
        this.SetHealthSlider();
        this.FlashHurtEffect();
        return this.currentHealth;
    }

    /// <summary>
    /// Applies damage that comes from a status effect, such as poison, or from
    /// reflected damage. It does not go through the damage multipliers, so
    /// effects can never feed into each other.
    /// </summary>
    /// <param name="amount">The amount of damage.</param>
    public override void ApplyStatusDamage(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        int health = this.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(amount)));

        if (health > 0)
        {
            return;
        }

        CrawlerPlayerHandler handler = this.GetComponent<CrawlerPlayerHandler>();

        if (handler != null)
        {
            handler.Die();
        }
    }

    /// <summary>
    /// Equips the specified item in the correct slot.
    /// </summary>
    /// <param name="item">Item to equip.</param>
    public void EquipItem(Item item)
    {
        if (item == null)
        {
            return;
        }

        if (item is EquippableItem equippable)
        {
            equippable.Initialize();
            if (item is Weapon weapon)
            {
                if (this.equippedWeapon != null)
                {
                    this.equippedWeapon.Unequip();
                }

                this.equippedWeapon = weapon;
                this.equippedWeapon.Equip(this.weaponPlaceholder);
                this.inventoryDescriptionText.SetText(weapon.GetRuntimeEditableDescription());
            }
            else if (item is Tool tool)
            {
                if (this.equippedTool != null)
                {
                    this.equippedTool.Unequip();
                }

                this.equippedTool = tool;
                this.equippedTool.Equip(this.toolPlaceholder);
                this.inventoryDescriptionText.SetText(tool.Description);
            }
        }

        ItemButtonUI[] uibuttons = FindObjectsByType<ItemButtonUI>();
        foreach (ItemButtonUI btn in uibuttons)
        {
            if ((btn.ItemType == ItemType.Weapon && item is Weapon) ||
                (btn.ItemType == ItemType.Tool && item is Tool))
            {
                if (btn != null)
                {
                    btn.Deselect();
                }
            }
        }
    }

    /// <summary>
    /// Adds an item to the inventory and equips it automatically if the corresponding slot is empty.
    /// </summary>
    /// <param name="item">Item to add.</param>
    public void AddToInventory(Item item)
    {
        if (item == null)
        {
            return;
        }

        this.inventory.Add(item);

        if (item is Weapon weapon && this.equippedWeapon == null)
        {
            this.EquipItem(weapon);
        }
        else if (item is Tool tool && this.equippedTool == null)
        {
            this.EquipItem(tool);
        }
    }

    /// <summary>
    /// Uses the currently equipped weapon.
    /// </summary>
    /// <param name="playerHandler">The player handler used for animating.</param>
    public void UseWeapon(CrawlerPlayerHandler playerHandler)
    {
        this.equippedWeapon?.Use(playerHandler, this);
    }

    /// <summary>
    /// Uses the currently equipped tool.
    /// </summary>
    /// <param name="playerHandler">The player handler used for animating.</param>
    public void UseTool(CrawlerPlayerHandler playerHandler)
    {
        this.equippedTool?.Use(playerHandler, this);
    }

    /// <summary>
    /// Sets the last time an equippable item was used.
    /// </summary>
    /// <param name="time">The current time when an item is used.</param>
    public void SetLastUseTime(float time)
    {
        this.lastUseTime = time;
    }

    /// <summary>
    /// Displays the inventory and populates its contents.
    /// </summary>
    public void ShowInventory()
    {
        this.inventoryCanvas.SetActive(true);
        this.OnInventoryChange();
    }

    /// <summary>
    /// Hides the inventory.
    /// </summary>
    public void HideInventory()
    {
        this.inventoryCanvas.SetActive(false);
    }

    /// <summary>
    /// Called when the map is finished loading.
    /// </summary>
    public override void OnFinishMapLoad()
    {
        base.OnFinishMapLoad();

        this.currentHealth = this.maxHealth;

        this.healthSlider.maxValue = this.maxHealth;
        this.SetHealthSlider();

        this.Inventory.Clear();
        if (this.inventoryCanvas.activeSelf)
        {
            this.HideInventory();
        }
    }

    private void SetHealthSlider()
    {
        int damage = this.maxHealth - this.currentHealth;
        if (damage > 0f)
        {
            this.healthSlider.value = damage;
        }

        this.healthText.SetText(this.currentHealth + "/" + this.maxHealth);
    }

    private void OnInventoryChange()
    {
        List<Item> weapons = this.inventory
            .Where(item => item.ItemType == ItemType.Weapon)
            .ToList();

        List<Item> tools = this.inventory
            .Where(item => item.ItemType == ItemType.Tool)
            .ToList();

        this.inventoryWeaponsUI.PopulateInventory(this, weapons.ToArray());
        this.inventoryToolsUI.PopulateInventory(this, tools.ToArray());
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
}
