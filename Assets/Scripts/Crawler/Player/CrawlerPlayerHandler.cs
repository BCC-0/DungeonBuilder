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

    /// <summary>
    /// Gets the last direction this player was facing.
    /// </summary>
    public Vector2 LastFacingDirection => this.lastFacingDirection;

    /// <summary>
    /// Gets a value indicating whether the player can currently move.
    /// </summary>
    public bool CanMove => this.movementLockCount <= 0;

    /// <summary>
    /// Gets the team of the player.
    /// </summary>
    public Team Team => Team.Player;

    /// <summary>
    /// Applies damage to the player if the attacking team is allowed to
    /// damage the player. Applies knockback when specified.
    /// </summary>
    /// <param name="info">The damage details.</param>
    public void TakeDamage(DamageInfo info)
    {
        if (!TeamUtility.CanDamage(info.Team, this.Team))
        {
            return;
        }

        this.playerData.TakeDamage(Mathf.RoundToInt(info.Amount));

        if (info.Knockback > 0f)
        {
            this.ApplyKnockback(info.Direction, info.Knockback);
        }
    }

    /// <summary>
    /// Applies a knockback impulse and temporarily pauses player movement.
    /// </summary>
    /// <param name="direction">The direction to push the player.</param>
    /// <param name="force">The strength of the push.</param>
    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (force <= 0f)
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
        if (this.CanMove)
        {
            this.playerData.UseWeapon(this);
        }
    }

    /// <summary>
    /// Starts using a tool by calling our current tool's use function.
    /// </summary>
    public void UseTool()
    {
        if (this.CanMove)
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
        if (this.inventoryTransitioning)
        {
            return this.playerData.IsInventoryOpen;
        }

        bool open = !this.playerData.IsInventoryOpen;

        this.inventoryTransitioning = true;

        // Pause the entire game immediately.
        Time.timeScale = 0f;

        if (open)
        {
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

        // this.animator.Update(0f);
        this.animator.SetTrigger("Use");
    }

    /// <summary>
    /// Pauses the player's movement for the given time.
    /// </summary>
    /// <param name="time">The time to pause the player.</param>
    public void PausePlayerMovement(float time)
    {
        this.StartCoroutine(this.PlayerPauser(time));
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
    }

    private void Update()
    {
        this.UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (this.CanMove)
        {
            Vector2 targetPosition = this.rigidbody2d.position + (this.movementDirection * this.playerData.MoveSpeed * Time.fixedDeltaTime);

            this.rigidbody2d.MovePosition(targetPosition);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
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
            this.playerData.AddToInventory(itemObject.TakeItem());
            Destroy(gameObject);
        }
    }

    private void UpdateAnimator()
    {
        if (!this.CanMove)
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
