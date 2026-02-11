using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains all player-related stats and inventory data.
/// Handles equipped items, health, and inventory management.
/// </summary>
public class CrawlerPlayerData: MonoBehaviour
{
    [Header("Player Attributes")]
    [SerializeField]
    private int maxHealth = 100;

    [SerializeField]
    private int currentHealth;

    [SerializeField]
    private float moveSpeed = 5f;

    [Header("Inventory")]
    [SerializeField]
    private List<Item> inventory = new List<Item>();

    [SerializeField]
    private GameObject inventoryCanvas;

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
        if (item is Weapon weapon)
        {
            this.equippedWeapon = weapon;
            Debug.Log($"Equipped weapon: {weapon.ItemName}");
        }
        else if (item is Tool tool)
        {
            this.equippedTool = tool;
            Debug.Log($"Equipped tool: {tool.ItemName}");
        }
        else
        {
            // Should never happen! (Can only happen if the player somehow obtains a consumable item).
            Debug.LogWarning($"Cannot equip item of type {item.ItemType}");
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

        // Auto-equip if slot is empty
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
    public void UseWeapon()
    {
        this.equippedWeapon?.Use(this);
    }

    /// <summary>
    /// Uses the currently equipped tool.
    /// </summary>
    public void UseTool()
    {
        this.equippedWeapon?.Use(this);
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
    public void OpenInventory()
    {
        Time.timeScale = this.inventoryCanvas.activeSelf ? 1f : 0f;
        this.inventoryCanvas.SetActive(!this.inventoryCanvas.activeSelf);
    }

    private void Awake()
    {
        this.currentHealth = this.maxHealth;
    }
}
