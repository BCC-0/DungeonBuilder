using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Inspector row used for editing the transform of selected entities.
/// </summary>
public class RuntimeTransformEditor : MonoBehaviour
{
    [Header("Position")]
    [SerializeField]
    private InputField positionX;

    [SerializeField]
    private InputField positionY;

    [SerializeField]
    private InputField positionZ;

    [Header("Rotation")]
    [SerializeField]
    private InputField rotationX;

    [SerializeField]
    private InputField rotationY;

    [SerializeField]
    private InputField rotationZ;

    [Header("Scale")]
    [SerializeField]
    private InputField scaleX;

    [SerializeField]
    private InputField scaleY;

    [SerializeField]
    private InputField scaleZ;

    private IReadOnlyList<SaveableEntity> targets;

    private bool updating;

    /// <summary>
    /// Initializes the transform inspector.
    /// </summary>
    public void Initialize(IReadOnlyList<SaveableEntity> entities)
    {
        this.targets = entities;

        this.Refresh();

        this.RegisterListeners();
    }

    /// <summary>
    /// Refreshes all transform values.
    /// </summary>
    private void Refresh()
    {
        if (this.targets == null || this.targets.Count == 0)
        {
            return;
        }

        SaveableEntity entity = this.targets[0];

        if (entity == null)
        {
            return;
        }

        Transform transform = entity.transform;

        this.updating = true;

        this.positionX.text = transform.position.x.ToString();
        this.positionY.text = transform.position.y.ToString();
        this.positionZ.text = transform.position.z.ToString();

        this.rotationX.text = transform.eulerAngles.x.ToString();
        this.rotationY.text = transform.eulerAngles.y.ToString();
        this.rotationZ.text = transform.eulerAngles.z.ToString();

        this.scaleX.text = transform.localScale.x.ToString();
        this.scaleY.text = transform.localScale.y.ToString();
        this.scaleZ.text = transform.localScale.z.ToString();

        this.updating = false;
    }

    /// <summary>
    /// Registers input callbacks.
    /// </summary>
    private void RegisterListeners()
    {
        this.positionX.onEndEdit.AddListener(_ => this.Apply());
        this.positionY.onEndEdit.AddListener(_ => this.Apply());
        this.positionZ.onEndEdit.AddListener(_ => this.Apply());

        this.rotationX.onEndEdit.AddListener(_ => this.Apply());
        this.rotationY.onEndEdit.AddListener(_ => this.Apply());
        this.rotationZ.onEndEdit.AddListener(_ => this.Apply());

        this.scaleX.onEndEdit.AddListener(_ => this.Apply());
        this.scaleY.onEndEdit.AddListener(_ => this.Apply());
        this.scaleZ.onEndEdit.AddListener(_ => this.Apply());
    }

    /// <summary>
    /// Applies the current transform values to all selected entities.
    /// </summary>
    private void Apply()
    {
        if (this.updating || this.targets == null)
        {
            return;
        }

        if (!this.TryReadVector3(
                this.positionX,
                this.positionY,
                this.positionZ,
                out Vector3 position))
        {
            return;
        }

        if (!this.TryReadVector3(
                this.rotationX,
                this.rotationY,
                this.rotationZ,
                out Vector3 rotation))
        {
            return;
        }

        if (!this.TryReadVector3(
                this.scaleX,
                this.scaleY,
                this.scaleZ,
                out Vector3 scale))
        {
            return;
        }

        foreach (SaveableEntity entity in this.targets)
        {
            if (entity == null)
            {
                continue;
            }

            entity.transform.position = position;
            entity.transform.eulerAngles = rotation;
            entity.transform.localScale = scale;
        }
    }

    /// <summary>
    /// Reads three input fields as a Vector3.
    /// </summary>
    private bool TryReadVector3(
        InputField x,
        InputField y,
        InputField z,
        out Vector3 result)
    {
        result = Vector3.zero;

        if (!float.TryParse(x.text, out float xValue) ||
            !float.TryParse(y.text, out float yValue) ||
            !float.TryParse(z.text, out float zValue))
        {
            return false;
        }

        result = new Vector3(xValue, yValue, zValue);
        return true;
    }
}