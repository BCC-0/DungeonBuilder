using UnityEngine;
using Direction = EnemyBody.Direction;

/// <summary>
/// The chicken head. It does not decide when to attack: the body asks for an
/// attack with <see cref="RequestAttack"/>, and the head shoots an egg at the
/// player when its cooldown allows it. The head looks at the player and
/// handles all of its own animation.
/// </summary>
public class ChickenHead : EnemyHead
{
    [Header("Attack")]
    [SaveField(1, 10)]
    private float attackCooldown = 2f;
    [SaveField(1, 20)]
    private float eggSpeed = 6f;
    [SaveField(1, 50)]
    private int eggDamage = 1;

    [SerializeField]
    private EggProjectile eggPrefab;
    [SerializeField]
    private Vector2 eggSpawnOffset = new Vector2(0f, 0.5f);

    [Header("Animation")]
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private float minAmbientInterval = 4f;
    [SerializeField]
    private float maxAmbientInterval = 8f;

    private State state;
    private float stateEndTime;
    private float nextAmbientTime;
    private float nextAttackTime;
    private bool attackRequested;
    private string currentAnimation;

    private enum State
    {
        Idle,
        Attacking,
        Ambient,
    }

    /// <summary>
    /// Called by the body to say it wants to attack. The request is only
    /// valid for one frame, so the body keeps calling this while it wants to
    /// attack. The head still waits for its own cooldown.
    /// </summary>
    public void RequestAttack()
    {
        this.attackRequested = true;
    }

    /// <summary>
    /// Attacks when the body asked for it and the cooldown is over, then
    /// updates the animation.
    /// </summary>
    public override void Tick()
    {
        this.TryAttack();

        // A request is only valid for the frame it was made in.
        this.attackRequested = false;
        this.UpdateAnimationState();
    }

    /// <summary>
    /// Finds the animator of this head and starts idling.
    /// </summary>
    protected override void OnAttached()
    {
        if (this.animator == null)
        {
            this.animator = this.GetComponent<Animator>();
        }

        this.state = State.Idle;
        this.currentAnimation = null;
        this.ScheduleAmbient();
        this.UpdateSortingOrder();
        this.RefreshAnimation();
    }

    /// <summary>
    /// Updates the head when the body's facing direction changes.
    /// </summary>
    protected override void OnFacingDirectionChanged()
    {
        this.UpdateSortingOrder();

        // Turning ends an ambient animation.
        if (this.state == State.Ambient)
        {
            this.state = State.Idle;
        }

        // Do not restart an attack that is playing, it ends on its own.
        if (this.state == State.Idle)
        {
            this.RefreshAnimation();
        }
    }

    private void TryAttack()
    {
        if (!this.attackRequested || Time.time < this.nextAttackTime)
        {
            return;
        }

        Transform player = this.Body.PlayerTransform;

        if (player == null)
        {
            return;
        }

        this.nextAttackTime = Time.time + this.attackCooldown;
        this.state = State.Attacking;
        this.Play("Attack" + this.FacingDirection, true);
        this.stateEndTime = Time.time + this.GetCurrentAnimationLength();

        this.ShootEgg(player);
    }

    private void UpdateSortingOrder()
    {
        if (this.SpriteRenderer == null || this.Body == null)
        {
            return;
        }

        SpriteRenderer bodyRenderer = this.Body.SpriteRenderer;

        if (bodyRenderer == null)
        {
            return;
        }

        this.SpriteRenderer.sortingLayerID = bodyRenderer.sortingLayerID;

        int offset = this.FacingDirection == Direction.Up ? -1 : 1;
        this.SpriteRenderer.sortingOrder = bodyRenderer.sortingOrder + offset;
    }

    private void ShootEgg(Transform target)
    {
        if (this.eggPrefab == null)
        {
            return;
        }

        Vector2 spawnPosition = this.Origin + this.eggSpawnOffset;
        Vector2 direction = ((Vector2)target.position - spawnPosition).normalized;

        EggProjectile egg = Instantiate(this.eggPrefab, spawnPosition, Quaternion.identity);
        egg.Initialize(this.Body, direction, this.eggSpeed, this.eggDamage);
    }

    private void UpdateAnimationState()
    {
        if (this.state == State.Idle)
        {
            if (Time.time >= this.nextAmbientTime)
            {
                this.state = State.Ambient;
                this.RefreshAnimation();
                this.stateEndTime = Time.time + this.GetCurrentAnimationLength();
            }

            return;
        }

        if (Time.time >= this.stateEndTime)
        {
            this.state = State.Idle;
            this.ScheduleAmbient();
            this.RefreshAnimation();
        }
    }

    private void ScheduleAmbient()
    {
        this.nextAmbientTime = Time.time + Random.Range(this.minAmbientInterval, this.maxAmbientInterval);
    }

    private void RefreshAnimation()
    {
        switch (this.state)
        {
            case State.Attacking:
                this.Play("Attack" + this.FacingDirection);
                break;
            case State.Ambient:
                this.Play("Ambient" + this.FacingDirection);
                break;
            default:
                this.Play("Idle" + this.FacingDirection);
                break;
        }
    }

    private void Play(string stateName, bool restart = false)
    {
        if (this.animator == null || (!restart && stateName == this.currentAnimation))
        {
            return;
        }

        this.animator.Play(stateName, 0, 0f);
        this.currentAnimation = stateName;
    }

    private float GetCurrentAnimationLength()
    {
        if (this.animator == null)
        {
            return 0f;
        }

        return this.animator.GetCurrentAnimatorStateInfo(0).length;
    }
}