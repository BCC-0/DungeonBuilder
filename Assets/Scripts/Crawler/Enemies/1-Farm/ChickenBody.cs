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

    [Header("Knockback")]
    [SerializeField]
    private float knockbackDuration = 0.2f;

    [Header("Animation")]
    [SerializeField]
    private Animator bodyAnimator;

    private bool lastAnimationMoving;
    private bool animationInitialized;

    private Vector2 movement;
    private float randomWalkTimer;
    private float knockbackTimer;


    /// <summary>
    /// Applies the knockback and pauses the walking, so the movement does not
    /// cancel the push.
    /// </summary>
    /// <param name="direction">The direction to push in.</param>
    /// <param name="force">The strength of the push.</param>
    public override void ApplyKnockback(Vector2 direction, float force)
    {
        if (!this.CanReceiveKnockback ||
            !this.ExistsInWorld)
        {
            return;
        }

        base.ApplyKnockback(direction, force);
        this.knockbackTimer = this.knockbackDuration;
    }

    /// <summary>
    /// Gets the animator.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// The chicken body tries to walk towards a minimum distance from the player and attack from there.
    /// Only called while the player is detected.
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

        // Make sure the random walk picks a fresh direction next time.
        this.randomWalkTimer = 0f;

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

    /// <summary>
    /// The chicken wanders by walking in random cardinal directions,
    /// or standing still, while the player is not detected.
    /// </summary>
    protected override void UpdateWander()
    {
        this.MoveRandomly();
    }

    /// <summary>
    /// Switches the body animation when the visible direction changes.
    /// The delay itself is handled by the base class.
    /// </summary>
    protected override void OnVisibleDirectionChanged()
    {
        if (this.bodyAnimator == null)
        {
            return;
        }

        this.PlayAnimation();
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

        // While knocked back, the physics push moves the chicken instead.
        if (this.knockbackTimer > 0f)
        {
            this.knockbackTimer -= Time.fixedDeltaTime;

            if (this.knockbackTimer <= 0f)
            {
                this.Rb.linearVelocity = Vector2.zero;
            }

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

        // Initialize the animation immediately.
        if (!this.animationInitialized)
        {
            this.PlayAnimation();
            return;
        }

        // Walking/idle transitions remain immediate.
        if (this.IsMoving != this.lastAnimationMoving)
        {
            this.PlayAnimation();
        }
    }

    private void PlayAnimation()
    {
        string stateName =
            (this.IsMoving ? "Walk" : "Idle") + this.VisibleDirection;

        this.bodyAnimator.Play(stateName);

        this.lastAnimationMoving = this.IsMoving;
        this.animationInitialized = true;
    }
}