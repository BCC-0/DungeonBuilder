using System.Collections.Generic;
using System.Linq;
using TMPro;
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

    [SerializeField]
    private TextMeshProUGUI inventoryDescriptionText;

    [SerializeField]
    private InventoryUI inventoryWeaponsUI;
    [SerializeField]
    private InventoryUI inventoryToolsUI;

    [Header("Equipped Items")]
    [SerializeField]
    private Weapon equippedWeapon;
    private ItemButtonUI equippedWeaponButton;

    [SerializeField]
    private Tool equippedTool;
    private ItemButtonUI equippedToolButton;

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
    /// <param name="itemButtonUI">The button of the item to equip. Saved for outline removal.</param>
    /// <param name="item">Item to equip.</param>
    public void EquipItem(ItemButtonUI itemButtonUI, Item item)
    {
        if (item is Weapon weapon)
        {
            if (this.equippedWeaponButton != null)
            {
                this.equippedWeaponButton.Deselect();
            }

            this.equippedWeaponButton = itemButtonUI;
            this.equippedWeapon = weapon;
            this.inventoryDescriptionText.text = weapon.Description;
            Debug.Log($"Equipped weapon: {weapon.ItemName}");
        }
        else if (item is Tool tool)
        {
            if (this.equippedToolButton != null)
            {
                this.equippedToolButton.Deselect();
            }

            this.equippedToolButton = itemButtonUI;
            this.equippedTool = tool;
            this.inventoryDescriptionText.text = tool.Description;
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
        // if (this.equippedWeapon == null && item is Weapon weapon)
        // {
        //    // TODO: Auto-equip first gotten weapon/tool and find the UI button from a function in inventory UI.
        //    // this.EquipItem(weapon);
        // }
        // else if (this.equippedTool == null && item is Tool tool)
        // {
        //    // this.EquipItem(tool);
        // }
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

    private void Awake()
    {
        this.currentHealth = this.maxHealth;

        // Close inventory if it is opened.
        if (this.inventoryCanvas.activeSelf)
        {
            this.OpenInventory();
        }

        //Debugging:
        for(int i = 0; i < 200; i++)
        {
            inventory.Add(equippedWeapon);
        }
    }
}
