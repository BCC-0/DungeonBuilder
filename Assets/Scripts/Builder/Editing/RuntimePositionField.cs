using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Inspector row used for editing the position of selected entities.
/// </summary>
public class RuntimePositionField : MonoBehaviour
{
    [Header("Position")]
    [SerializeField]
    private TMP_InputField positionX;

    [SerializeField]
    private TMP_InputField positionY;

    private IReadOnlyList<SaveableEntity> targets;

    private bool updating;

    /// <summary>
    /// Initializes the position inspector.
    /// </summary>
    /// <param name="entities">The entities to edit the position for.</param>
    public void Initialize(IReadOnlyList<SaveableEntity> entities)
    {
        this.targets = entities;

        this.Refresh();
        this.RegisterListeners();
    }

    /// <summary>
    /// Refreshes all position values.
    /// </summary>
    private void Refresh()
    {
        if (this.targets == null || this.targets.Count == 0)
        {
            return;
        }

        this.updating = true;

        if (this.targets.Count > 1)
        {
            this.positionX.text = "-";
            this.positionY.text = "-";

            this.SetInteractable(false);
        }
        else
        {
            SaveableEntity entity = this.targets[0];

            if (entity == null)
            {
                this.updating = false;
                return;
            }

            Transform transform = entity.transform;

            this.positionX.text =
                this.WorldToGrid(transform.position.x).ToString();

            this.positionY.text =
                this.WorldToGrid(transform.position.y).ToString();

            this.SetInteractable(true);
        }

        this.updating = false;
    }

    /// <summary>
    /// Registers input callbacks.
    /// </summary>
    private void RegisterListeners()
    {
        this.positionX.onEndEdit.AddListener(_ => this.Apply());
        this.positionY.onEndEdit.AddListener(_ => this.Apply());
    }

    /// <summary>
    /// Enables or disables position editing.
    /// </summary>
    private void SetInteractable(bool interactable)
    {
        this.positionX.interactable = interactable;
        this.positionY.interactable = interactable;
    }

    /// <summary>
    /// Applies the current grid position to the selected entity.
    /// </summary>
    private void Apply()
    {
        if (this.updating ||
            this.targets == null ||
            this.targets.Count != 1)
        {
            return;
        }

        try
        {
            if (!this.TryReadGridPosition(
                    this.positionX,
                    this.positionY,
                    out Vector2 gridPosition))
            {
                throw new EditException(
                    "Position must contain whole numbers.");
            }

            SaveableEntity entity = this.targets[0];

            if (entity == null)
            {
                return;
            }

            Vector2 worldPosition =
                this.GridToWorld(gridPosition);

            entity.transform.position = new Vector3(
                worldPosition.x,
                worldPosition.y,
                entity.transform.position.z);
        }
        catch (EditException exception)
        {
            Debug.LogWarning(
                $"RuntimePositionField: {exception.Message}");

            this.Refresh();
        }
    }

    /// <summary>
    /// Converts a world coordinate to the grid coordinate
    /// displayed by the editor.
    /// </summary>
    private int WorldToGrid(float worldPosition)
    {
        return Mathf.FloorToInt(worldPosition);
    }

    /// <summary>
    /// Converts a grid coordinate to the corresponding world
    /// position at the center of the tile.
    /// </summary>
    private Vector2 GridToWorld(Vector2 gridPosition)
    {
        return new Vector2(
            gridPosition.x + 0.5f,
            gridPosition.y + 0.5f);
    }

    /// <summary>
    /// Reads the input fields as an integer grid position.
    /// </summary>
    private bool TryReadGridPosition(
        TMP_InputField x,
        TMP_InputField y,
        out Vector2 result)
    {
        result = Vector2.zero;

        if (!int.TryParse(x.text, out int xValue))
        {
            return false;
        }

        if (!int.TryParse(y.text, out int yValue))
        {
            return false;
        }

        result = new Vector2(
            xValue,
            yValue);

        return true;
    }
}