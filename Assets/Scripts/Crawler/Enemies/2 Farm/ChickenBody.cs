using UnityEngine;

/// <summary>
/// The chicken enemy body behaviour.
/// </summary>
public class ChickenBody : EnemyBody
{
    [Header("Movement")]
    [SaveField(min: 1, max: 10)]
    private int minDistance = 3;

    private float distanceTolerance = 0.2f;
    private float randomWalkDuration = 1f;

    [Header("Animation")]
    [SerializeField]
    private Animator bodyAnimator;

    private Direction lastAnimationDirection;
    private bool lastAnimationMoving;
    private bool animationInitialized;

    private Vector2 movement;
    private float randomWalkTimer;

    /// <summary>
    /// Gets the animator.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// The chicken body. tries to walk towards a min distance from the player and attack from there.
    /// </summary>
    protected override void UpdateMovement()
    {
        Transform playerTransform = this.PlayerTransform;

        if (playerTransform == null || this.Rb == null)
        {
            // No player to react to: stand still.
            this.movement = Vector2.zero;
            this.IsMoving = false;
            this.UpdateAnimation();
            return;
        }

        Vector2 toPlayer = (Vector2)playerTransform.position - this.Rb.position;
        float distance = toPlayer.magnitude;

        if (distance > this.minDistance + this.distanceTolerance)
        {
            // Too far away: approach the player.
            this.MoveAlongOneAxis(toPlayer);
        }
        else if (distance < this.minDistance - this.distanceTolerance)
        {
            // Too close: move away from the player.
            this.MoveAlongOneAxis(-toPlayer);
        }
        else
        {
            // Close enough to the preferred distance.
            this.movement = Vector2.zero;
            this.IsMoving = false;
        }

        this.ApplyMovement();
        this.UpdateAnimation();
    }

    private void MoveAlongOneAxis(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            this.movement = new Vector2(Mathf.Sign(direction.x), 0f);
            this.FacingDirection = direction.x >= 0
                ? Direction.Right
                : Direction.Left;
        }
        else
        {
            this.movement = new Vector2(0f, Mathf.Sign(direction.y));
            this.FacingDirection = direction.y >= 0
                ? Direction.Up
                : Direction.Down;
        }

        this.IsMoving = this.movement != Vector2.zero;
    }

    private void MoveRandomly()
    {
        this.randomWalkTimer -= Time.fixedDeltaTime;

        if (this.randomWalkTimer <= 0f)
        {
            // Pick a random cardinal direction, or stand still.
            int choice = Random.Range(0, 5);

            this.movement = choice switch
            {
                0 => Vector2.up,
                1 => Vector2.down,
                2 => Vector2.left,
                3 => Vector2.right,
                _ => Vector2.zero
            };

            if (this.movement == Vector2.up)
            {
                this.FacingDirection = Direction.Up;
            }
            else if (this.movement == Vector2.down)
            {
                this.FacingDirection = Direction.Down;
            }
            else if (this.movement == Vector2.left)
            {
                this.FacingDirection = Direction.Left;
            }
            else if (this.movement == Vector2.right)
            {
                this.FacingDirection = Direction.Right;
            }

            this.randomWalkTimer = this.randomWalkDuration;
        }

        this.IsMoving = this.movement != Vector2.zero;
        this.ApplyMovement();
        this.UpdateAnimation();
    }

    private void ApplyMovement()
    {
        if (this.Rb == null)
        {
            return;
        }

        Vector2 nextPosition = this.Rb.position
            + (this.movement * this.MoveSpeed * Time.fixedDeltaTime);

        this.Rb.MovePosition(nextPosition);
    }

    private void UpdateAnimation()
    {
        if (this.bodyAnimator == null)
        {
            return;
        }

        if (this.animationInitialized &&
            this.FacingDirection == this.lastAnimationDirection &&
            this.IsMoving == this.lastAnimationMoving)
        {
            return;
        }

        string stateName = (this.IsMoving ? "Walk" : "Idle") + this.FacingDirection;
        this.bodyAnimator.Play(stateName);

        this.lastAnimationDirection = this.FacingDirection;
        this.lastAnimationMoving = this.IsMoving;
        this.animationInitialized = true;
    }
}