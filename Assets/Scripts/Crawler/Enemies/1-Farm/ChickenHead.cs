using System.Collections;
using UnityEngine;
using Direction = EnemyBody.Direction;

/// <summary>
/// The chicken head automatically attacks at random intervals.
/// It handles egg spawning and all head animations.
/// </summary>
public class ChickenHead : EnemyHead
{
    [Header("Attack")]
    [SaveField(1, 10)]
    [SerializeField]
    private float attackSpeed = 1f;

    [SaveField(1, 20)]
    [SerializeField]
    private float eggSpeed = 6f;

    [SaveField(1, 50)]
    [SerializeField]
    private int eggDamage = 1;

    [SerializeField]
    private EggProjectile eggPrefab;

    [SerializeField]
    private Vector2 eggSpawnOffset = Vector2.zero;

    [SerializeField]
    [Tooltip("Seconds after the attack animation starts before the egg appears.")]
    private float eggSpawnDelay = 0.2f;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private float minAmbientInterval = 4f;

    [SerializeField]
    private float maxAmbientInterval = 8f;

    [Header("Attack Timing")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Chance to actually attack each time the attack interval expires.")]
    private float attackChance = 1f / 3f;

    [SerializeField]
    [Range(0.5f, 1.5f)]
    private float minAttackIntervalMultiplier = 0.75f;

    [SerializeField]
    [Range(0.5f, 1.5f)]
    private float maxAttackIntervalMultiplier = 1.25f;

    private State state;
    private float stateEndTime;
    private float nextAmbientTime;
    private float nextAttackTime;
    private string currentAnimation;

    // Direction the head is locked to while attacking.
    private Direction attackDirection;

    private enum State
    {
        Idle,
        Attacking,
        Ambient,
    }

    /// <summary>
    /// Gets the direction the head is actually looking: the locked attack
    /// direction while attacking, otherwise the body's facing direction.
    /// </summary>
    private Direction HeadDirection =>
        this.state == State.Attacking ? this.attackDirection : this.FacingDirection;

    /// <summary>
    /// Initializes the head and schedules its first attack.
    /// </summary>
    protected override void OnAttached()
    {
        if (this.animator == null)
        {
            this.animator = this.GetComponent<Animator>();
        }

        this.state = State.Idle;
        this.currentAnimation = null;

        this.ScheduleNextAttack();
        this.ScheduleAmbient();

        this.UpdateSortingOrder();
        this.RefreshAnimation();
    }

    /// <summary>
    /// Automatically attacks when the randomized timer expires.
    /// </summary>
    public override void Tick()
    {
        this.UpdateAnimationState();

        if (this.state != State.Attacking &&
            Time.time >= this.nextAttackTime)
        {
            this.TryAttack();
        }
    }

    /// <summary>
    /// Updates the head when its facing direction changes.
    /// </summary>
    protected override void OnFacingDirectionChanged()
    {
        this.UpdateSortingOrder();

        if (this.state == State.Ambient)
        {
            this.state = State.Idle;
        }

        if (this.state != State.Attacking)
        {
            this.RefreshAnimation();
        }
    }

    /// <summary>
    /// Snaps a vector to the nearest of the four cardinal directions.
    /// </summary>
    private static Direction GetCardinalDirection(Vector2 vector)
    {
        if (Mathf.Abs(vector.x) > Mathf.Abs(vector.y))
        {
            return vector.x >= 0f ? Direction.Right : Direction.Left;
        }

        return vector.y >= 0f ? Direction.Up : Direction.Down;
    }

    private static Vector2 DirectionToVector(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up: return Vector2.up;
            case Direction.Down: return Vector2.down;
            case Direction.Left: return Vector2.left;
            default: return Vector2.right;
        }
    }

    private void TryAttack()
    {
        Transform player = this.Body != null
            ? this.Body.PlayerTransform
            : null;

        if (player == null)
        {
            this.nextAttackTime = Time.time + 0.5f;
            return;
        }

        // The player has not been detected yet: do not attack, check again soon.
        // The detection range is defined on the body.
        if (!this.Body.IsPlayerDetected)
        {
            this.nextAttackTime = Time.time + 0.5f;
            return;
        }

        // Each time the interval expires, only attack with a certain chance.
        // Otherwise wait for the next interval and roll again.
        if (Random.value > this.attackChance)
        {
            this.ScheduleNextAttack();
            return;
        }

        // Turn the head to the best 90-degree direction towards the player.
        Vector2 toPlayer =
            (Vector2)player.position - (Vector2)this.transform.position;
        this.attackDirection = GetCardinalDirection(toPlayer);

        this.state = State.Attacking;
        this.UpdateSortingOrder();

        string attackAnimation = "Attack" + this.attackDirection;
        this.Play(attackAnimation, true);

        // The next attack is scheduled when this attack animation ends,
        // so the cooldown can never expire while the chicken is still attacking.
        this.StopAllCoroutines();
        this.StartCoroutine(this.WaitForAttackAnimation(attackAnimation));
        this.StartCoroutine(this.ShootEggAfterDelay());
    }

    private IEnumerator ShootEggAfterDelay()
    {
        if (this.eggSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(this.eggSpawnDelay);
        }

        // Fire only in the direction the head is looking.
        this.ShootEgg(DirectionToVector(this.attackDirection));
    }

    private IEnumerator WaitForAttackAnimation(string animationName)
    {
        if (this.animator == null)
        {
            this.state = State.Idle;
            this.UpdateSortingOrder();
            this.ScheduleNextAttack();
            this.ScheduleAmbient();
            this.RefreshAnimation();
            yield break;
        }

        // Wait until Unity has entered the attack animation.
        yield return null;

        while (this.animator != null &&
               this.animator.GetCurrentAnimatorStateInfo(0).IsName(animationName) &&
               this.animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        // Wait if the Animator has not entered the state yet.
        while (this.animator != null &&
               !this.animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
        {
            yield return null;
        }

        // Let the attack animation finish before returning to idle.
        while (this.animator != null &&
               this.animator.GetCurrentAnimatorStateInfo(0).IsName(animationName) &&
               this.animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        if (this.state == State.Attacking)
        {
            this.state = State.Idle;
            this.UpdateSortingOrder();
            this.ScheduleNextAttack();
            this.ScheduleAmbient();
            this.RefreshAnimation();
        }
    }

    private void ScheduleNextAttack()
    {
        // attackSpeed represents the approximate number of attacks
        // per second. Randomize the delay around its base interval.
        float baseInterval = 1f / Mathf.Max(0.1f, this.attackSpeed);

        float minMultiplier = Mathf.Min(
            this.minAttackIntervalMultiplier,
            this.maxAttackIntervalMultiplier);

        float maxMultiplier = Mathf.Max(
            this.minAttackIntervalMultiplier,
            this.maxAttackIntervalMultiplier);

        float randomInterval = baseInterval * Random.Range(
            minMultiplier,
            maxMultiplier);

        this.nextAttackTime = Time.time + randomInterval;
    }

    private void ShootEgg(Vector2 direction)
    {
        if (this.eggPrefab == null)
        {
            return;
        }

        Vector2 spawnPosition = (Vector2)this.transform.position + this.eggSpawnOffset;

        EggProjectile egg = Instantiate(
            this.eggPrefab,
            spawnPosition,
            Quaternion.identity);

        egg.Initialize(
            this.Body,
            direction,
            this.eggSpeed,
            this.eggDamage);
    }

    private void UpdateAnimationState()
    {
        // The attack coroutine handles the attack animation ending.
        if (this.state == State.Attacking)
        {
            return;
        }

        // Handle ambient animation completion.
        if (this.state == State.Ambient)
        {
            if (Time.time >= this.stateEndTime)
            {
                this.state = State.Idle;
                this.ScheduleAmbient();
                this.RefreshAnimation();
            }

            return;
        }

        // Start an ambient animation when its timer expires.
        if (Time.time >= this.nextAmbientTime)
        {
            this.state = State.Ambient;
            this.RefreshAnimation();

            this.stateEndTime =
                Time.time + this.GetCurrentAnimationLength();
        }
    }

    private void ScheduleAmbient()
    {
        this.nextAmbientTime = Time.time + Random.Range(
            this.minAmbientInterval,
            this.maxAmbientInterval);
    }

    private void RefreshAnimation()
    {
        switch (this.state)
        {
            case State.Attacking:
                this.Play("Attack" + this.HeadDirection);
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
        if (this.animator == null)
        {
            return;
        }

        if (!restart && stateName == this.currentAnimation)
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

        AnimatorStateInfo info =
            this.animator.GetCurrentAnimatorStateInfo(0);

        return info.length;
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

        this.SpriteRenderer.sortingLayerID =
            bodyRenderer.sortingLayerID;

        int offset = this.HeadDirection == Direction.Up ? -1 : 1;

        this.SpriteRenderer.sortingOrder =
            bodyRenderer.sortingOrder + offset;
    }
}