using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Inspector row used for editing the position of selected entities or tiles.
/// </summary>
public class RuntimePositionField : MonoBehaviour
{
    [Header("Position")]
    [SerializeField]
    private TMP_InputField positionX;

    [SerializeField]
    private TMP_InputField positionY;

    private IReadOnlyList<SaveableEntity> targets;

    private IReadOnlyList<Vector2Int> tileTargets;

    private SaveableTilemap tilemap;

    private bool tileMode;

    private bool updating;

    /// <summary>
    /// Initializes the position inspector for entities.
    /// </summary>
    /// <param name="entities">The entities to edit the position for.</param>
    public void Initialize(
        IReadOnlyList<SaveableEntity> entities)
    {
        this.tileMode = false;
        this.tilemap = null;
        this.tileTargets = null;
        this.targets = entities;

        this.Refresh();
        this.RegisterListeners();
    }

    /// <summary>
    /// Initializes the position inspector for tiles.
    /// </summary>
    /// <param name="positions">The tile positions to edit.</param>
    /// <param name="tilemap">The tilemap containing the tiles.</param>
    public void Initialize(
        IReadOnlyList<Vector2Int> positions,
        SaveableTilemap tilemap)
    {
        this.tileMode = true;
        this.tilemap = tilemap;
        this.tileTargets = positions;
        this.targets = null;

        this.Refresh();
        this.RegisterListeners();
    }

    /// <summary>
    /// Refreshes all position values.
    /// </summary>
    public void Refresh()
    {
        if (this.tileMode)
        {
            this.RefreshTiles();
            return;
        }

        this.RefreshEntities();
    }

    /// <summary>
    /// Refreshes the position values for selected entities.
    /// </summary>
    private void RefreshEntities()
    {
        if (this.targets == null ||
            this.targets.Count == 0)
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
            SaveableEntity entity =
                this.targets[0];

            if (entity == null)
            {
                this.updating = false;
                return;
            }

            Transform transform =
                entity.transform;

            this.positionX.text =
                this.WorldToGrid(
                    transform.position.x).ToString();

            this.positionY.text =
                this.WorldToGrid(
                    transform.position.y).ToString();

            this.SetInteractable(true);
        }

        this.updating = false;
    }

    /// <summary>
    /// Refreshes the position values for selected tiles.
    /// </summary>
    private void RefreshTiles()
    {
        if (this.tileTargets == null ||
            this.tileTargets.Count == 0 ||
            this.tilemap == null)
        {
            return;
        }

        this.updating = true;

        if (this.tileTargets.Count > 1)
        {
            this.positionX.text = "-";
            this.positionY.text = "-";

            this.SetInteractable(false);
        }
        else
        {
            Vector2Int position =
                this.tileTargets[0];

            this.positionX.text =
                position.x.ToString();

            this.positionY.text =
                position.y.ToString();

            this.SetInteractable(true);
        }

        this.updating = false;
    }

    /// <summary>
    /// Registers input callbacks.
    /// </summary>
    private void RegisterListeners()
    {
        this.positionX.onEndEdit.RemoveAllListeners();
        this.positionY.onEndEdit.RemoveAllListeners();

        this.positionX.onEndEdit.AddListener(
            _ => this.Apply());

        this.positionY.onEndEdit.AddListener(
            _ => this.Apply());
    }

    /// <summary>
    /// Enables or disables position editing.
    /// </summary>
    private void SetInteractable(bool interactable)
    {
        this.positionX.interactable =
            interactable;

        this.positionY.interactable =
            interactable;
    }

    /// <summary>
    /// Applies the current position to the selected target.
    /// </summary>
    private void Apply()
    {
        if (this.updating)
        {
            return;
        }

        if (this.tileMode)
        {
            this.ApplyTilePosition();
            return;
        }

        this.ApplyEntityPosition();
    }

    /// <summary>
    /// Applies the current grid position to the selected entity.
    /// </summary>
    private void ApplyEntityPosition()
    {
        if (this.targets == null ||
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

            SaveableEntity entity =
                this.targets[0];

            if (entity == null)
            {
                return;
            }

            Vector2Int destinationCell =
                new Vector2Int(
                    Mathf.RoundToInt(gridPosition.x),
                    Mathf.RoundToInt(gridPosition.y));

            SaveableEntity entityAtDestination =
                this.FindEntityAtPosition(
                    destinationCell,
                    entity);

            if (entityAtDestination != null)
            {
                Destroy(entityAtDestination.gameObject);
            }

            Vector2 worldPosition =
                this.GridToWorld(gridPosition);

            entity.transform.position =
                new Vector3(
                    worldPosition.x,
                    worldPosition.y,
                    entity.transform.position.z);

            // Keep the selection outline on the entity.
            EntitySelectionVisualizer visualizer =
                FindAnyObjectByType<EntitySelectionVisualizer>();

            if (visualizer != null)
            {
                visualizer.Refresh();
            }
        }
        catch (EditException exception)
        {
            Debug.LogWarning(
                $"RuntimePositionField: {exception.Message}");

            this.Refresh();
        }
    }

    /// <summary>
    /// Applies the current grid position to the selected tile.
    /// </summary>
    private void ApplyTilePosition()
    {
        if (this.tileTargets == null ||
            this.tileTargets.Count != 1 ||
            this.tilemap == null)
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

            Vector2Int oldPosition =
                this.tileTargets[0];

            Vector2Int newPosition =
                new Vector2Int(
                    Mathf.RoundToInt(gridPosition.x),
                    Mathf.RoundToInt(gridPosition.y));

            if (oldPosition == newPosition)
            {
                return;
            }

            if (!this.tilemap.MoveTile(
                    oldPosition,
                    newPosition))
            {
                throw new EditException(
                    $"Could not move tile from " +
                    $"({oldPosition.x}, {oldPosition.y}) " +
                    $"to ({newPosition.x}, {newPosition.y}).");
            }
        }
        catch (EditException exception)
        {
            Debug.LogWarning(
                $"RuntimePositionField: {exception.Message}");

            this.Refresh();
        }
    }

    /// <summary>
    /// Finds an entity at the given position.
    /// The tilemap entity is ignored, otherwise it would be found (and
    /// destroyed) for every position close to the origin.
    /// </summary>
    /// <param name="cell">The cell to find at.</param>
    /// <param name="ignore">An entity to ignore. (The one we are trying to place there)</param>
    /// <returns>The found entity.</returns>
    private SaveableEntity FindEntityAtPosition(
        Vector2Int cell,
        SaveableEntity ignore)
    {
        return FindObjectsByType<SaveableEntity>()
            .FirstOrDefault(other =>
                other != ignore &&
                other.GetComponent<Tilemap>() == null &&
                new Vector2Int(
                    Mathf.FloorToInt(other.transform.position.x),
                    Mathf.FloorToInt(other.transform.position.y)) == cell);
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

        if (!int.TryParse(
                x.text,
                out int xValue))
        {
            return false;
        }

        if (!int.TryParse(
                y.text,
                out int yValue))
        {
            return false;
        }

        result = new Vector2(
            xValue,
            yValue);

        return true;
    }
}