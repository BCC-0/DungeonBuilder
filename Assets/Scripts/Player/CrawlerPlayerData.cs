using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Contains all player-related stats and inventory data.
/// Handles equipped items, health, and inventory management.
/// </summary>
public class CrawlerPlayerData : SaveableEntity
{
    [Header("Player Attributes")]
    [SerializeField]
    [SaveField]
    private int maxHealth = 100;

    [SerializeField]
    [SaveField]
    private int currentHealth = 100;

    [SerializeField]
    private float moveSpeed = 5f;

    [Header("Inventory")]
    [SerializeField]
    private List<Item> inventory = new List<Item>();

    [SerializeField]
    private GameObject inventoryCanvas;

    [SerializeField]
    private TextMeshProUGUI inventoryDescriptionText;

    [SerializeField]
    private InventoryUI inventoryWeaponsUI;
    [SerializeField]
    private InventoryUI inventoryToolsUI;

    [Header("Equipped Items")]
    [SerializeField]
    private Weapon equippedWeapon;

    [SerializeField]
    private Tool equippedTool;

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
    /// Gets the player's move speed.
    /// </summary>
    public float MoveSpeed => this.moveSpeed;

    /// <summary>
    /// Gets the player's inventory.
    /// </summary>
    public List<Item> Inventory => this.inventory;

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
    /// Heals the player.
    /// </summary>
    /// <param name="amount">Amount to heal.</param>
    public void Heal(int amount)
    {
        this.currentHealth = Mathf.Min(this.currentHealth + amount, this.maxHealth);
        Debug.Log($"Player healed: {amount}. Current health: {this.currentHealth}");
    }

    /// <summary>
    /// Damages the player.
    /// </summary>
    /// <param name="amount">Amount of damage.</param>
    public void TakeDamage(int amount)
    {
        this.currentHealth = Mathf.Max(this.currentHealth - amount, 0);
        Debug.Log($"Player took {amount} damage. Current health: {this.currentHealth}");
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

        if (item is Weapon weapon)
        {
            this.equippedWeapon = weapon;
            this.inventoryDescriptionText.text = weapon.GetRuntimeEditableDescription();
        }
        else if (item is Tool tool)
        {
            this.equippedTool = tool;
            this.inventoryDescriptionText.text = tool.Description;
        }

        var uibuttons = FindObjectsByType<ItemButtonUI>(FindObjectsSortMode.InstanceID);
        foreach (var btn in uibuttons)
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
        Debug.Log($"Added to inventory: {item.ItemName}");

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
    /// Opens or closes the inventory depending on if it is opened.
    /// </summary>
    /// <returns>Returns whether the inventory is opened after this.</returns>
    public bool OpenInventory()
    {
        // If it was not active yet, opened becomes true since we open it.
        bool opened = !this.inventoryCanvas.activeSelf;
        Time.timeScale = opened ? 0f : 1f;
        this.inventoryCanvas.SetActive(opened);

        // Populate both weapons and tools parts.
        if (opened)
        {
            this.OnInventoryChange();
        }

        return opened;
    }

    private void OnInventoryChange()
    {
        List<Item> weapons = this.inventory
            .Where(item => item.ItemType == ItemType.Weapon)
            .ToList();

        List<Item> tools = this.inventory
            .Where(item => item.ItemType == ItemType.Tool)
            .ToList();

        // Populate UI sections
        this.inventoryWeaponsUI.PopulateInventory(this, weapons.ToArray());
        this.inventoryToolsUI.PopulateInventory(this, tools.ToArray());
    }

    private void Start()
    {
        this.currentHealth = this.maxHealth;

        // Close inventory if it is opened.
        if (this.inventoryCanvas.activeSelf)
        {
            this.OpenInventory();
        }
    }
}
