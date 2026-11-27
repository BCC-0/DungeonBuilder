
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles player movement and using tools/weapons.
/// </summary>
[RequireComponent(typeof(CrawlerInputHandler))]
[RequireComponent(typeof(CrawlerPlayerData))]
public class CrawlerPlayerHandler : MonoBehaviour, IDamageable, IKnockbackable
{
    private CrawlerPlayerData playerData;

    private Rigidbody2D rigidbody2d;
    private Vector2 movementDirection;

    private int movementLockCount = 0;
    private Vector2 lastFacingDirection = Vector2.down;

    private Animator animator;
    private AnimatorOverrideController overrideController;

    private AnimatorUpdateMode originalAnimatorUpdateMode;

    private bool inventoryTransitioning;
    private Tween inventoryTween;

    private Vector3 originalCameraLocalPosition;
    private float originalCameraSize;

    private Vector2 previousFacingDirection;

    [Header("Inventory Transition")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private CanvasGroup inventoryCanvasGroup;

    [SerializeField]
    private float cameraZoomDuration = 0.8f;

    [SerializeField]
    private float inventoryFadeDuration = 0.4f;

    [SerializeField]
    private float inventoryZoomSize = 2f;

    [SerializeField]
    private Vector3 inventoryCameraLocalPosition;

    [Header("Inventory Background Breathing")]
    [SerializeField]
    private Image inventoryBackground;

    [SerializeField]
    private Sprite[] inventoryBreathingSprites;

    [SerializeField]
    private float minBreathingInterval = 5f;

    [SerializeField]
    private float maxBreathingInterval = 12f;

    [SerializeField]
    private float breathingFrameDuration = 0.15f;

    private Coroutine inventoryBreathingCoroutine;

    [Header("Death Animation")]
    [SerializeField]
    private SpriteRenderer playerSpriteRenderer;

    [SerializeField]
    private string deathAnimationTrigger = "Die";

    [SerializeField]
    private string deathAnimationState = "Death";

    [SerializeField]
    private float deathAnimationTimeout = 5f;

    [Header("Death UI")]
    [SerializeField]
    private CanvasGroup deathCanvasGroup;

    [SerializeField]
    private float deathFadeDuration = 1f;

    [SerializeField]
    private float deathSpriteFadeDuration = 1f;

    [Header("Status Effects")]
    [SerializeField]
    private GameObject statusEffectUIPrefab;

    [SerializeField]
    private Vector2 statusEffectUIOffset = new Vector2(0f, 1.5f);

    private bool isDead;

    private EntityWorldUI statusWorldUI;

    /// <summary>
    /// Gets the last direction this player was facing.
    /// </summary>
    public Vector2 LastFacingDirection => this.lastFacingDirection;

    /// <summary>
    /// Gets a value indicating whether the player can currently move.
    /// </summary>
    public bool CanMove =>
        this.movementLockCount <= 0 &&
        (this.playerData == null || !this.playerData.StatusEffects.PreventsMovement);

    /// <summary>
    /// Gets the team of the player.
    /// </summary>
    public Team Team => Team.Player;

    /// <summary>
    /// Gets the multiplier on the damage this player deals, including status
    /// effects such as strength.
    /// </summary>
    public float DamageMultiplier =>
        this.playerData != null && this.playerData.StatusEffects != null
            ? this.playerData.StatusEffects.DamageDealtMultiplier
            : 1f;

    /// <summary>
    /// Applies damage to the player if the attacking team is allowed to
    /// damage the player. Applies knockback when specified.
    /// </summary>
    /// <param name="info">The damage details.</param>
    public void TakeDamage(DamageInfo info)
    {
        if (this.isDead || !TeamUtility.CanDamage(info.Team, this.Team))
        {
            return;
        }

        DamageInfo scaled = info.WithAmount(
            info.Amount * this.playerData.StatusEffects.DamageTakenMultiplier);

        this.playerData.ProcessIncomingDamage(scaled);

        this.playerData.TakeDamage(Mathf.RoundToInt(scaled.Amount));

        if (info.Knockback > 0f)
        {
            this.ApplyKnockback(info.Direction, info.Knockback);
        }

        if (this.playerData.CurrentHealth <= 0)
        {
            this.Die();
        }
    }

    /// <summary>
    /// Applies a knockback impulse and temporarily pauses player movement.
    /// </summary>
    /// <param name="direction">The direction to push the player.</param>
    /// <param name="force">The strength of the push.</param>
    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (force <= 0f || this.isDead)
        {
            return;
        }

        KnockbackUtility.Apply(this.rigidbody2d, direction, force);
        this.PausePlayerMovement(0.2f);
    }

    /// <summary>
    /// Sets the current movement direction of the player.
    /// </summary>
    /// <param name="move">The movement direction.</param>
    public void SetMovement(Vector2 move)
    {
        if (this.playerData != null &&
            this.playerData.StatusEffects != null &&
            this.playerData.StatusEffects.ConfusesMovement)
        {
            // Confusion turns the movement of the player ninety degrees.
            move = new Vector2(move.y, -move.x);
        }

        this.movementDirection = move.normalized;
    }

    /// <summary>
    /// Interact with the object at the interaction vector.
    /// </summary>
    /// <param name="interaction">Location of the object we want to interact with.</param>
    public void Interact(Vector2 interaction)
    {
        // TODO: Call interaction of object.
        // First find object using the map's find at location!
        Debug.Log("Interacted with " + interaction);
    }

    /// <summary>
    /// Starts an attack by calling our current weapon's attack function.
    /// </summary>
    public void Attack()
    {
        if (this.CanMove &&
            !this.isDead &&
            !this.playerData.StatusEffects.PreventsAttack)
        {
            this.playerData.UseWeapon(this);
        }
    }

    /// <summary>
    /// Starts using a tool by calling our current tool's use function.
    /// </summary>
    public void UseTool()
    {
        if (this.CanMove &&
            !this.isDead &&
            !this.playerData.StatusEffects.PreventsItemUse)
        {
            this.playerData.UseTool(this);
        }
    }

    /// <summary>
    /// Opens or closes the inventory with a camera and UI transition.
    /// </summary>
    /// <returns>Returns whether the inventory will be open after this call.</returns>
    public bool OpenInventory()
    {
        if (this.inventoryTransitioning || this.isDead)
        {
            return this.playerData.IsInventoryOpen;
        }

        bool open = !this.playerData.IsInventoryOpen;

        this.inventoryTransitioning = true;

        // Pause the entire game immediately.
        Time.timeScale = 0f;

        if (open)
        {
            EntityWorldUI.SetGlobalEnabled(false);
            this.SetCanMove(false);
            this.movementDirection = Vector2.zero;

            this.previousFacingDirection = this.lastFacingDirection;

            // Allow the player's animator to update while the game is paused.
            this.originalAnimatorUpdateMode = this.animator.updateMode;
            this.animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            // Make the player face down towards the camera.
            this.lastFacingDirection = Vector2.down;

            this.animator.SetFloat("MoveX", 0f);
            this.animator.SetFloat("MoveY", -1f);
            this.animator.SetFloat("Speed", 0f);

            this.OpenInventorySequence();
        }
        else
        {
            this.CloseInventorySequence();
        }

        return open;
    }

    /// <summary>
    /// Sets whether the player can do anything.
    /// Used for pausing, animating, stuns, etc.
    /// </summary>
    /// <param name="canMove">Whether the player can move after this.</param>
    public void SetCanMove(bool canMove)
    {
        if (canMove)
        {
            // Removes this lock.
            this.movementLockCount--;
            return;
        }

        // Adds a lock, since the player shouldn't move now.
        this.movementLockCount++;
    }

    /// <summary>
    /// Plays a weapon/tool animation dynamically via override controller.
    /// </summary>
    /// <param name="clip">The clip to set.</param>
    public void PlayUseAnimation(AnimationClip clip)
    {
        this.overrideController["Use"] = clip;

        this.animator.SetTrigger("Use");
    }

    /// <summary>
    /// Pauses the player's movement for the given time.
    /// </summary>
    /// <param name="time">The time to pause the player.</param>
    public void PausePlayerMovement(float time)
    {
        if (!this.isDead)
        {
            this.StartCoroutine(this.PlayerPauser(time));
        }
    }

    /// <summary>
    /// Handles the player's death and starts the death sequence.
    /// </summary>
    public void Die()
    {
        if (this.isDead)
        {
            return;
        }

        this.isDead = true;

        this.playerData.StatusEffects?.ClearAll();

        if (this.statusWorldUI != null)
        {
            this.statusWorldUI.Destroy();
            this.statusWorldUI = null;
        }

        this.SetCanMove(false);
        this.movementDirection = Vector2.zero;
        this.rigidbody2d.linearVelocity = Vector2.zero;

        // Ensure gameplay isn't paused from an open inventory.
        Time.timeScale = 1f;

        if (this.inventoryBreathingCoroutine != null)
        {
            this.StopCoroutine(this.inventoryBreathingCoroutine);
            this.inventoryBreathingCoroutine = null;
        }

        this.inventoryTween?.Kill();
        this.inventoryTween = null;

        this.playerData.HideInventory();
        this.inventoryTransitioning = false;

        this.inventoryCanvasGroup.interactable = false;
        this.inventoryCanvasGroup.blocksRaycasts = false;

        this.StartCoroutine(this.DeathSequence());
    }

    /// <summary>
    /// Plays the death animation, hides the player,
    /// and fades in the game over screen.
    /// </summary>

    private IEnumerator DeathSequence()
    {
        this.animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        bool canPlayDeathAnimation = true;

        // Check that the death animation state exists.
        string fullStatePath = "Base Layer." + this.deathAnimationState;
        int fullStateHash = Animator.StringToHash(fullStatePath);

        if (!this.animator.HasState(0, fullStateHash))
        {
            Debug.LogError(
                $"Death animation state '{fullStatePath}' does not exist. " +
                "Skipping the animation and continuing to Game Over."
            );

            canPlayDeathAnimation = false;
        }

        // Check that the death trigger exists.
        bool hasDeathTrigger = false;

        foreach (AnimatorControllerParameter parameter in this.animator.parameters)
        {
            if (parameter.name == this.deathAnimationTrigger &&
                parameter.type == AnimatorControllerParameterType.Trigger)
            {
                hasDeathTrigger = true;
                break;
            }
        }

        if (!hasDeathTrigger)
        {
            Debug.LogError(
                $"Death animation trigger '{this.deathAnimationTrigger}' " +
                "does not exist. Skipping the animation and continuing to Game Over."
            );

            canPlayDeathAnimation = false;
        }

        // Play and wait for the death animation if configured correctly.
        if (canPlayDeathAnimation)
        {
            this.animator.SetTrigger(this.deathAnimationTrigger);

            int deathStateHash = Animator.StringToHash(this.deathAnimationState);
            float elapsed = 0f;
            bool animationStarted = false;

            while (elapsed < 1f)
            {
                if (this.animator.GetCurrentAnimatorStateInfo(0).shortNameHash
                    == deathStateHash)
                {
                    animationStarted = true;
                    break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!animationStarted)
            {
                Debug.LogWarning(
                    $"Death animation '{this.deathAnimationState}' was not entered. " +
                    "Continuing to Game Over."
                );
            }
            else
            {
                elapsed = 0f;
                bool animationFinished = false;

                while (elapsed < this.deathAnimationTimeout)
                {
                    AnimatorStateInfo state =
                        this.animator.GetCurrentAnimatorStateInfo(0);

                    if (state.shortNameHash == deathStateHash &&
                        state.normalizedTime >= 1f)
                    {
                        animationFinished = true;
                        break;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (!animationFinished)
                {
                    Debug.LogWarning(
                        $"Death animation '{this.deathAnimationState}' timed out. " +
                        "Continuing to Game Over."
                    );
                }
            }
        }

        // Pause gameplay before fading out the player.
        Time.timeScale = 0f;

        // Fade out the player using unscaled time.
        if (this.playerSpriteRenderer != null)
        {
            yield return this.playerSpriteRenderer
                .DOFade(0f, this.deathSpriteFadeDuration)
                .SetEase(Ease.OutSine)
                .SetUpdate(true)
                .WaitForCompletion();

            this.playerSpriteRenderer.enabled = false;
        }

        this.animator.enabled = false;

        // Show the Game Over screen.
        this.deathCanvasGroup.alpha = 0f;
        this.deathCanvasGroup.interactable = false;
        this.deathCanvasGroup.blocksRaycasts = false;
        this.deathCanvasGroup.gameObject.SetActive(true);

        this.deathCanvasGroup
            .DOFade(1f, this.deathFadeDuration)
            .SetEase(Ease.OutSine)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                this.deathCanvasGroup.interactable = true;
                this.deathCanvasGroup.blocksRaycasts = true;
            });
    }


    private void OpenInventorySequence()
    {
        this.inventoryCanvasGroup.alpha = 0f;
        this.inventoryCanvasGroup.interactable = false;
        this.inventoryCanvasGroup.blocksRaycasts = false;

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true);

        // Give the player time to face the camera.
        sequence.AppendInterval(0.15f);

        // Zoom the camera towards the player's body.
        sequence.Append(
            this.playerCamera.transform
                .DOLocalMove(this.inventoryCameraLocalPosition, this.cameraZoomDuration)
                .SetEase(Ease.InOutSine)
        );

        sequence.Join(
            this.playerCamera
                .DOOrthoSize(this.inventoryZoomSize, this.cameraZoomDuration)
                .SetEase(Ease.InOutSine)
        );

        // Show and populate the inventory after the camera zoom.
        sequence.AppendCallback(() =>
        {
            this.playerData.ShowInventory();
        });

        // Fade in the inventory.
        sequence.Append(
            this.inventoryCanvasGroup
                .DOFade(1f, this.inventoryFadeDuration)
                .SetEase(Ease.OutSine)
        );

        sequence.OnComplete(() =>
        {
            this.inventoryCanvasGroup.interactable = true;
            this.inventoryCanvasGroup.blocksRaycasts = true;

            this.inventoryTransitioning = false;
            this.inventoryTween = null;

            this.inventoryBreathingCoroutine = this.StartCoroutine(
                this.InventoryBreathingAnimation()
            );
        });

        this.inventoryTween = sequence;
    }

    private void CloseInventorySequence()
    {
        if (this.inventoryBreathingCoroutine != null)
        {
            this.StopCoroutine(this.inventoryBreathingCoroutine);
            this.inventoryBreathingCoroutine = null;
        }

        if (this.inventoryBreathingSprites.Length > 0)
        {
            this.inventoryBackground.sprite = this.inventoryBreathingSprites[0];
        }

        this.inventoryCanvasGroup.interactable = false;
        this.inventoryCanvasGroup.blocksRaycasts = false;

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true);

        // Fade out the inventory.
        sequence.Append(
            this.inventoryCanvasGroup
                .DOFade(0f, this.inventoryFadeDuration)
                .SetEase(Ease.InSine)
        );

        // Hide the inventory after fading out.
        sequence.AppendCallback(() =>
        {
            this.playerData.HideInventory();
        });

        // Restore the original camera position.
        sequence.Append(
            this.playerCamera.transform
                .DOLocalMove(this.originalCameraLocalPosition, this.cameraZoomDuration)
                .SetEase(Ease.InOutSine)
        );

        sequence.Join(
            this.playerCamera
                .DOOrthoSize(this.originalCameraSize, this.cameraZoomDuration)
                .SetEase(Ease.InOutSine)
        );

        sequence.OnComplete(() =>
        {
            // Restore the player's previous facing direction.
            this.lastFacingDirection = this.previousFacingDirection;

            this.animator.SetFloat("MoveX", this.lastFacingDirection.x);
            this.animator.SetFloat("MoveY", this.lastFacingDirection.y);
            this.animator.SetFloat("Speed", 0f);

            // Restore the original animator update mode.
            this.animator.updateMode = this.originalAnimatorUpdateMode;

            // Allow the player to move again.
            this.SetCanMove(true);

            this.inventoryTransitioning = false;
            this.inventoryTween = null;

            // Resume gameplay.
            Time.timeScale = 1f;
            EntityWorldUI.SetGlobalEnabled(true);
        });

        this.inventoryTween = sequence;
    }

    private IEnumerator InventoryBreathingAnimation()
    {
        while (this.playerData.IsInventoryOpen)
        {
            // Wait a random amount of real-world time.
            float waitTime = Random.Range(
                this.minBreathingInterval,
                this.maxBreathingInterval
            );

            yield return new WaitForSecondsRealtime(waitTime);

            if (!this.playerData.IsInventoryOpen)
            {
                break;
            }

            // Play through the breathing sprites.
            for (int i = 0; i < this.inventoryBreathingSprites.Length; i++)
            {
                this.inventoryBackground.sprite = this.inventoryBreathingSprites[i];

                yield return new WaitForSecondsRealtime(this.breathingFrameDuration);
            }

            // Return to the default background sprite.
            if (this.inventoryBreathingSprites.Length > 0)
            {
                this.inventoryBackground.sprite = this.inventoryBreathingSprites[0];
            }
        }

        this.inventoryBreathingCoroutine = null;
    }

    private IEnumerator PlayerPauser(float time)
    {
        this.SetCanMove(false);
        yield return new WaitForSeconds(time);
        this.SetCanMove(true);
        this.rigidbody2d.linearVelocity = Vector2.zero;
    }

    private void CreateStatusEffectUI()
    {
        if (this.statusEffectUIPrefab == null)
        {
            return;
        }

        Canvas canvas = EntityWorldUI.FindWorldUICanvas(this.gameObject);
        Camera worldCamera = this.playerCamera != null
            ? this.playerCamera
            : FindAnyObjectByType<Camera>();

        if (canvas == null || worldCamera == null)
        {
            Debug.LogWarning(
                "Could not initialize the player status effect UI: Canvas or Camera not found.",
                this);

            return;
        }

        this.statusWorldUI = new EntityWorldUI(
            this.statusEffectUIPrefab,
            canvas,
            worldCamera,
            this.statusEffectUIOffset,
            includeVitals: false);

        this.playerData.WorldUI = this.statusWorldUI;
    }

    private void Start()
    {
        this.rigidbody2d = this.GetComponent<Rigidbody2D>();
        this.playerData = this.GetComponent<CrawlerPlayerData>();
        this.animator = this.GetComponent<Animator>();

        this.overrideController = new AnimatorOverrideController(this.animator.runtimeAnimatorController);
        this.animator.runtimeAnimatorController = this.overrideController;

        this.originalCameraLocalPosition = this.playerCamera.transform.localPosition;
        this.originalCameraSize = this.playerCamera.orthographicSize;

        this.inventoryCanvasGroup.alpha = 0f;
        this.inventoryCanvasGroup.interactable = false;
        this.inventoryCanvasGroup.blocksRaycasts = false;

        this.deathCanvasGroup.alpha = 0f;
        this.deathCanvasGroup.interactable = false;
        this.deathCanvasGroup.blocksRaycasts = false;
        this.deathCanvasGroup.gameObject.SetActive(false);

        this.CreateStatusEffectUI();
    }

    private void Update()
    {
        this.UpdateAnimator();
    }

    private void LateUpdate()
    {
        this.statusWorldUI?.UpdateUIPosition(this.transform.position);
    }

    private void OnDestroy()
    {
        this.statusWorldUI?.Destroy();
    }

    private void FixedUpdate()
    {
        if (this.CanMove && !this.isDead)
        {
            Vector2 targetPosition = this.rigidbody2d.position +
                (this.movementDirection * this.playerData.MoveSpeed * Time.fixedDeltaTime);

            this.rigidbody2d.MovePosition(targetPosition);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (this.isDead)
        {
            return;
        }

        GameObject gameObject = collision.gameObject;

        if (gameObject.CompareTag("Item"))
        {
            ItemObject itemObject = gameObject.GetComponent<ItemObject>();

            if (itemObject == null)
            {
                Debug.LogError("Item object doesn't include ItemObject script!");
                return;
            }

            // TODO: Add get item animation!
            Item item = itemObject.TakeItem();

            if (item is ConsumableItem consumable)
            {
                consumable.Use(this, this.playerData);
                Destroy(item);
            }
            else
            {
                this.playerData.AddToInventory(item);
            }

            Destroy(gameObject);
        }
    }

    private void UpdateAnimator()
    {
        if (!this.CanMove || this.isDead)
        {
            return;
        }

        Vector2 dir = this.movementDirection;

        if (dir != Vector2.zero)
        {
            this.lastFacingDirection = dir;
        }

        this.animator.SetFloat("MoveX", this.lastFacingDirection.x);
        this.animator.SetFloat("MoveY", this.lastFacingDirection.y);

        this.animator.SetFloat("Speed", dir.sqrMagnitude > 0 ? 1f : 0f);
    }
}
