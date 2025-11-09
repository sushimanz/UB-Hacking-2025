using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// Attack height type enumeration
public enum AttackHeight
{
    Normal,
    Low,
    High
}

// Attack properties structure
[System.Serializable]
public struct AttackProperties
{
    [Tooltip("Damage dealt by this attack")]
    public float damage;
    [Tooltip("Horizontal knockback force")]
    public float knockbackForce;
    [Tooltip("Upward knockback force")]
    public float knockbackUpward;
    [Tooltip("Stun duration (seconds)")]
    public float stunDuration;
    [Tooltip("Hitstop duration (seconds)")]
    public float hitstopDuration;
    [Tooltip("Attack height type (Normal, Low, or High)")]
    public AttackHeight attackHeight;

    public AttackProperties(float damage, float knockbackForce, float knockbackUpward, float stunDuration, float hitstopDuration, AttackHeight attackHeight = AttackHeight.Normal)
    {
        this.damage = damage;
        this.knockbackForce = knockbackForce;
        this.knockbackUpward = knockbackUpward;
        this.stunDuration = stunDuration;
        this.hitstopDuration = hitstopDuration;
        this.attackHeight = attackHeight;
    }
}

// Hit data structure
public struct HitInfo
{
    public float damage;
    public Vector2 knockbackDirection;
    public float knockbackForce;
    public float knockbackUpwardForce;
    public float stunDuration;
    public float hitstopDuration;
    public AttackHeight attackHeight;
    public GameObject attacker; // Reference to the attacker for callback

    public HitInfo(float damage, Vector2 knockbackDirection, float knockbackForce, float knockbackUpwardForce, float stunDuration, float hitstopDuration, AttackHeight attackHeight, GameObject attacker)
    {
        this.damage = damage;
        this.knockbackDirection = knockbackDirection;
        this.knockbackForce = knockbackForce;
        this.knockbackUpwardForce = knockbackUpwardForce;
        this.stunDuration = stunDuration;
        this.hitstopDuration = hitstopDuration;
        this.attackHeight = attackHeight;
        this.attacker = attacker;
    }
}

public class player : MonoBehaviour
{
    [Header("Player Setup")]
    [Tooltip("Player index: 0 for Player 1, 1 for Player 2, etc.")]
    public int playerIndex = 0;

    [Header("Input System")]
    [Tooltip("If using PlayerInputManager, leave these empty. Otherwise assign Input Action References.")]
    public InputActionReference moveAction;
    [Tooltip("Assign the Jump action (button) from your Input Actions asset.")]
    public InputActionReference jumpAction;
    [Tooltip("Assign the Attack action (button) from your Input Actions asset.")]
    public InputActionReference attackAction;
    [Tooltip("Assign the Heavy Attack action (button) from your Input Actions asset.")]
    public InputActionReference heavyAttackAction;
    [Tooltip("Assign the Special Attack action (button) from your Input Actions asset.")]
    public InputActionReference specialAttackAction;

    [Header("Movement")]
    [Tooltip("Horizontal movement speed in units per second")]
    public float speed = 4f;
    [Tooltip("Dash multiplier applied to speed when dashing")]
    public float dashMultiplier = 2.5f;
    [Tooltip("Duration of the dash in seconds")]
    public float dashDuration = 0.18f;
    [Tooltip("Max time between taps to register a double-tap (seconds)")]
    public float doubleTapTime = 0.3f;

    [Header("Jump")]
    [Tooltip("Jump impulse/velocity.")]
    public float jumpForce = 6f;
    [Tooltip("Multiplier applied to gravity when falling (makes falls faster)")]
    public float fallMultiplier = 2.5f;
    [Tooltip("Multiplier applied to gravity when jump is released early (makes low jumps)")]
    public float lowJumpMultiplier = 2f;
    [Tooltip("Optional tag to identify ground objects. If non-empty, grounding will check the hit collider's tag instead of using the layer mask.")]
    public string groundTag = "Ground";
    [Tooltip("Optional transform to use as the ground-check origin. If null, player's position is used.")]
    public Transform groundCheck;

    [Header("Animation")]
    [Tooltip("Animator component for controlling animations")]
    public Animator animator;
    [Tooltip("Transform to flip (usually the sprite or a parent of sprite and hitboxes). If null, flips this GameObject.")]
    public Transform flipTransform;
    
    [Header("Character Selection")]
    [Tooltip("Array of animator controllers for different characters (index 0 = character 1, index 1 = character 2, etc.)")]
    public RuntimeAnimatorController[] characterAnimators;

    [Header("Attack Properties")]
    [Tooltip("Collider2D used as the attack hitbox (assign a child GameObject's collider)")]
    public Collider2D attackHitbox;
    [Tooltip("Properties for punch attack")]
    public AttackProperties punchProperties = new AttackProperties(10f, 1f, 2f, 0.5f, 0.1f, AttackHeight.Normal);
    [Tooltip("Properties for heavy punch attack")]
    public AttackProperties heavyPunchProperties = new AttackProperties(20f, 2f, 3f, 0.8f, 0.15f, AttackHeight.High);
    [Tooltip("Properties for low attack")]
    public AttackProperties lowProperties = new AttackProperties(8f, 1.2f, 0.5f, 0.4f, 0.08f, AttackHeight.Low);
    [Tooltip("Properties for low kick attack")]
    public AttackProperties lowKickProperties = new AttackProperties(12f, 1.5f, 1f, 0.6f, 0.1f, AttackHeight.Low);
    [Tooltip("Properties for air kick attack")]
    public AttackProperties airKickProperties = new AttackProperties(15f, 3f, 4f, 1f, 0.1f, AttackHeight.Normal);

    [Header("Hitstop")]
    [Tooltip("Time scale during hitstop (0 = frozen, 1 = normal)")]
    public float hitstopTimeScale = 0.1f;

    [Header("Health")]
    [Tooltip("Current health of the player")]
    public float health = 100f;
    [Tooltip("Maximum health of the player")]
    public float maxHealth = 100f;

    [Header("Hit Flash")]
    [Tooltip("SpriteRenderer to flash when hit")]
    public SpriteRenderer spriteRenderer;
    [Tooltip("Color to flash when hit")]
    public Color hitFlashColor = Color.red;
    [Tooltip("Duration of the hit flash effect")]
    public float hitFlashDuration = 0.1f;

    [Header("Combo System")]
    [Tooltip("Reference to the opponent player (used for combo tracking)")]
    public player opponent;

    [Header("Special Attack / Projectile")]
    [Tooltip("Array of projectile prefabs for each character (index 0 = character 1, index 1 = character 2, etc.)")]
    public GameObject[] projectilePrefabs;
    [Tooltip("Spawn point for projectile (if null, uses player position)")]
    public Transform projectileSpawnPoint;
    [Tooltip("Speed of the projectile")]
    public float projectileSpeed = 10f;
    [Tooltip("Properties for special attack/projectile")]
    public AttackProperties specialProperties = new AttackProperties(15f, 2f, 1.5f, 0.1f, 0.1f, AttackHeight.Normal);

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Enable();
        if (jumpAction != null && jumpAction.action != null)
            jumpAction.action.Enable();
        if (attackAction != null && attackAction.action != null)
            attackAction.action.Enable();
        if (heavyAttackAction != null && heavyAttackAction.action != null)
            heavyAttackAction.action.Enable();
        if (specialAttackAction != null && specialAttackAction.action != null)
            specialAttackAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Disable();
        if (jumpAction != null && jumpAction.action != null)
            jumpAction.action.Disable();
        if (attackAction != null && attackAction.action != null)
            attackAction.action.Disable();
        if (heavyAttackAction != null && heavyAttackAction.action != null)
            heavyAttackAction.action.Disable();
        if (specialAttackAction != null && specialAttackAction.action != null)
            specialAttackAction.action.Disable();
    }

    // cached physics components
    private Rigidbody2D rb2D;
    // jump held state
    private bool jumpHeld = false;
    // ground level (set when player first stops moving vertically)
    private float groundLevel;
    private bool groundLevelSet = false;
    // track if we're currently attacking
    private bool wasAttacking = false;
    // track which attack is currently active
    private string currentAttackType = "";
    // track objects hit during current attack to prevent multiple hits
    private HashSet<GameObject> hitObjectsThisAttack = new HashSet<GameObject>();
    // stun timer
    private float stunTimer = 0f;
    // hitstop timer
    private float hitstopTimer = 0f;
    private float originalTimeScale = 1f;
    // hit flash coroutine reference
    private Coroutine hitFlashCoroutine = null;
    private Color originalSpriteColor;
    // player facing direction (1 = right, -1 = left)
    private int facingDirection = 1;
    // blocking state
    private bool isBlocking = false;
    private bool isStandBlocking = false;
    private bool isCrouchBlocking = false;
    // combo tracking - global variable to track which attacks have been used in current combo
    private HashSet<string> usedAttacksInCombo = new HashSet<string>();
    // track if opponent was stunned last frame to detect when they exit stun
    private bool opponentWasStunnedLastFrame = false;
    // PlayerInput component for handling device assignment
    private PlayerInput playerInput;
    // Input actions from PlayerInput (used if InputActionReferences are not assigned)
    private InputAction playerMoveAction;
    private InputAction playerJumpAction;
    private InputAction playerAttackAction;
    private InputAction playerHeavyAttackAction;
    private InputAction playerSpecialAttackAction;

    // Update is used for kinematic movement (non-physics). If you use Rigidbody, consider moving logic to FixedUpdate and use velocity or MovePosition.
    void Start()
    {
        // Set player index based on GameObject name
        if (gameObject.name.Contains("Player 1") || gameObject.name.Contains("P1"))
        {
            playerIndex = 0;
            Debug.Log("Player index set to 0 (Player 1)");
        }
        else if (gameObject.name.Contains("Player 2") || gameObject.name.Contains("P2"))
        {
            playerIndex = 1;
            Debug.Log("Player index set to 1 (Player 2)");
        }
        
        // Try to get PlayerInput component (used with PlayerInputManager)
        playerInput = GetComponent<PlayerInput>();
        
        // If PlayerInput exists, get actions from it instead of InputActionReferences
        if (playerInput != null)
        {
            var actionMap = playerInput.currentActionMap;
            playerMoveAction = actionMap.FindAction("Move");
            playerJumpAction = actionMap.FindAction("Jump");
            playerAttackAction = actionMap.FindAction("Attack");
            playerHeavyAttackAction = actionMap.FindAction("HAttack");
            playerSpecialAttackAction = actionMap.FindAction("Special");
        }
        
        // Find opponent player
        FindOpponent();
        
        // Set the correct animator controller based on character selection from GameData
        SetupCharacterAnimator();
        
        // Get or cache Rigidbody2D and set initial Y velocity to non-zero
        if (rb2D == null)
        {
            rb2D = GetComponent<Rigidbody2D>();
        }
        
        if (rb2D != null)
        {
            // Set initial Y velocity to a small non-zero value (e.g., -0.1f for slight downward)
            Vector2 initialVelocity = rb2D.linearVelocity;
            initialVelocity.y = -0.1f;
            rb2D.linearVelocity = initialVelocity;
        }
        
        // Ground level will be set when player first stops moving vertically
        
        // Store the original sprite color
        if (spriteRenderer != null)
        {
            originalSpriteColor = spriteRenderer.color;
        }
    }

    void FindOpponent()
    {
        // Find all player objects in the scene
        player[] allPlayers = FindObjectsByType<player>(FindObjectsSortMode.None);
        
        // Find the other player (not this one)
        foreach (player p in allPlayers)
        {
            if (p != this)
            {
                opponent = p;
                Debug.Log(gameObject.name + " found opponent: " + opponent.gameObject.name);
                break;
            }
        }
        
        // If no opponent found yet, try again next frame
        if (opponent == null)
        {
            Invoke(nameof(FindOpponent), 0.1f);
        }
    }
    
    void SetupCharacterAnimator()
    {
        if (animator == null || characterAnimators == null || characterAnimators.Length == 0)
        {
            Debug.LogWarning("Animator or character animators not set up!");
            return;
        }
        
        // Get character ID from GameData singleton
        int characterID = GameData.Instance.GetPlayerCharacter(playerIndex);

        // Validate character ID
        if (characterID < 0 || characterID >= characterAnimators.Length)
        {
            Debug.LogWarning($"Invalid character ID {characterID} for Player {playerIndex + 1}. Using default character 0.");
            characterID = 0;
        }
        
        Debug.Log($"Player {playerIndex + 1} selected character ID: {characterID}");
        // Assign the animator controller
        if (characterAnimators[characterID] != null)
        {
            animator.runtimeAnimatorController = characterAnimators[characterID];
            Debug.Log($"Player {playerIndex + 1} using character {characterID} animator");
        }
        else
        {
            Debug.LogError($"Character animator at index {characterID} is null!");
        }
    }

    void Update()
    {
        // cache Rigidbody2D if not already
        if (rb2D == null)
        {
            rb2D = GetComponent<Rigidbody2D>();
        }

        // Update hitstop timer (uses unscaled time)
        if (hitstopTimer > 0f)
        {
            hitstopTimer -= Time.unscaledDeltaTime;
            if (hitstopTimer <= 0f)
            {
                hitstopTimer = 0f;
                Time.timeScale = originalTimeScale;
            }
        }

        // Update stun timer
        bool isStunned = stunTimer > 0f;
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer < 0f)
                stunTimer = 0f;
        }

        // Update stun animator parameter
        if (animator != null)
        {
            animator.SetBool("stun", isStunned);
        }

        // Track opponent's stun state to reset combo when they exit stun
        if (opponent != null)
        {
            bool opponentIsStunned = opponent.IsStunned();
            
            // If opponent was stunned last frame but is not stunned now, they exited stun
            if (opponentWasStunnedLastFrame && !opponentIsStunned)
            {
                // Reset our combo tracker since opponent is no longer in stun
                usedAttacksInCombo.Clear();
                Debug.Log(gameObject.name + "'s combo was reset (opponent exited stun)");
            
            }
            
            // Update the tracking variable for next frame
            opponentWasStunnedLastFrame = opponentIsStunned;
        }

        // Don't allow input while stunned
        if (isStunned)
        {
            // Still update previous horizontal for dash detection
            previousHorizontal = 0f;
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        // Read from PlayerInput actions if available, otherwise use InputActionReferences
        if (playerInput != null && playerMoveAction != null)
        {
            Vector2 v = playerMoveAction.ReadValue<Vector2>();
            horizontal = v.x;
            vertical = v.y;
        }
        else if (moveAction != null && moveAction.action != null)
        {
            Vector2 v = moveAction.action.ReadValue<Vector2>();
            horizontal = v.x;
            vertical = v.y;
        }

        // Update facing direction based on opponent position
        if (opponent != null)
        {
            if (opponent.transform.position.x > transform.position.x)
                facingDirection = 1; // Face right (opponent is to the right)
            else if (opponent.transform.position.x < transform.position.x)
                facingDirection = -1; // Face left (opponent is to the left)
        }
        else
        {
            // Fallback: Update facing direction based on movement if no opponent
            if (horizontal > 0.01f)
                facingDirection = 1;
            else if (horizontal < -0.01f)
                facingDirection = -1;
        }

        // Flip the sprite and hitboxes to match facing direction
        Transform targetTransform = flipTransform != null ? flipTransform : transform;
        targetTransform.localScale = new Vector3(facingDirection, 1f, 1f);

        // Check if player is holding back (opposite to facing direction)
        bool isHoldingBack = (facingDirection == 1 && horizontal < -0.5f) || (facingDirection == -1 && horizontal > 0.5f);

        // Check if crouching (holding down)
        bool isCrouching = vertical < -0.5f;

        // Determine blocking state and store in member variables
        isBlocking = isHoldingBack || (isCrouching && isHoldingBack);
        isStandBlocking = isHoldingBack && !isCrouching;
        isCrouchBlocking = isCrouching && isHoldingBack;

        // If crouching, disable horizontal movement
        if (isCrouching)
        {
            horizontal = 0f;
        }

        // Double-tap detection and dash handling
        HandleDoubleTapAndDash(horizontal, out int dashDirection);

        // Choose movement direction: if currently dashing, use dashDirection, otherwise use current horizontal input
        int moveDir = (isDashing ? dashDirection : (horizontal > 0.01f ? 1 : (horizontal < -0.01f ? -1 : 0)));

        float currentSpeed = speed;
        if (isDashing)
            currentSpeed *= dashMultiplier;

        // --- Jump input handling ---
        bool jumpPressed = false;
        if (playerInput != null && playerJumpAction != null)
        {
            jumpPressed = playerJumpAction.triggered;
            float val = 0f;
            try { val = playerJumpAction.ReadValue<float>(); } catch { val = 0f; }
            jumpHeld = val > 0.5f;
        }
        else if (jumpAction != null && jumpAction.action != null)
        {
            jumpPressed = jumpAction.action.triggered;
            float val = 0f;
            try { val = jumpAction.action.ReadValue<float>(); } catch { val = 0f; }
            jumpHeld = val > 0.5f;
        }

        // --- Attack input handling ---
        bool attackPressed = false;
        if (playerInput != null && playerAttackAction != null)
        {
            attackPressed = playerAttackAction.triggered;
        }
        else if (attackAction != null && attackAction.action != null)
        {
            attackPressed = attackAction.action.triggered;
        }

        // --- Heavy Attack input handling ---
        bool heavyAttackPressed = false;
        if (playerInput != null && playerHeavyAttackAction != null)
        {
            heavyAttackPressed = playerHeavyAttackAction.triggered;
        }
        else if (heavyAttackAction != null && heavyAttackAction.action != null)
        {
            heavyAttackPressed = heavyAttackAction.action.triggered;
        }

        // --- Special Attack input handling ---
        bool specialAttackPressed = false;
        if (playerInput != null && playerSpecialAttackAction != null)
        {
            specialAttackPressed = playerSpecialAttackAction.triggered;
        }
        else if (specialAttackAction != null && specialAttackAction.action != null)
        {
            specialAttackPressed = specialAttackAction.action.triggered;
        }

        // --- Ground check ---
        bool grounded = IsGrounded();

        // Check if currently attacking (punch or air kick)
        bool isPunching = false;
        bool isHeavyPunching = false;
        bool isLowAttacking = false;
        bool isLowKicking = false;
        bool isAirKicking = false;
        bool isSpecialAttacking = false;
        if (animator != null)
        {
            isPunching = animator.GetCurrentAnimatorStateInfo(0).IsName("punch");
            isHeavyPunching = animator.GetCurrentAnimatorStateInfo(0).IsName("Hpunch");
            isLowAttacking = animator.GetCurrentAnimatorStateInfo(0).IsName("low");
            isLowKicking = animator.GetCurrentAnimatorStateInfo(0).IsName("lowKick");
            isAirKicking = animator.GetCurrentAnimatorStateInfo(0).IsName("airKick");
            isSpecialAttacking = animator.GetCurrentAnimatorStateInfo(0).IsName("special");
        }

        if (attackPressed && animator != null)
        {
            if (grounded)
            {
                // Check if crouching - trigger low attack instead of punch
                if (isCrouching)
                {
                    // Only allow low attack if it hasn't been used in this combo
                    if (!usedAttacksInCombo.Contains("low"))
                    {
                        // Trigger low attack from crouch
                        animator.SetBool("walk", false);
                        animator.SetTrigger("low");
                        currentAttackType = "low";
                    }
                }
                else
                {
                    // Only allow punch if it hasn't been used in this combo
                    if (!usedAttacksInCombo.Contains("punch"))
                    {
                        // Trigger punch when grounded
                        animator.SetBool("walk", false);
                        animator.SetTrigger("punch");
                        currentAttackType = "punch";
                    }
                }
            }
            else
            {
                // Only allow air kick if it hasn't been used in this combo
                if (!usedAttacksInCombo.Contains("airKick"))
                {
                    // Trigger air kick when in the air
                    animator.SetTrigger("airKick");
                    currentAttackType = "airKick";
                }
            }
        }

        if (heavyAttackPressed && animator != null && grounded)
        {
            // Check if crouching - trigger lowKick instead of heavy punch
            if (isCrouching)
            {
                // Only allow lowKick if it hasn't been used in this combo
                if (!usedAttacksInCombo.Contains("lowKick"))
                {
                    // Trigger lowKick from crouch
                    animator.SetBool("walk", false);
                    animator.SetTrigger("lowKick");
                    currentAttackType = "lowKick";
                }
            }
            else
            {
                // Only allow heavy punch if it hasn't been used in this combo
                if (!usedAttacksInCombo.Contains("heavyPunch"))
                {
                    // Trigger heavy punch when grounded
                    animator.SetBool("walk", false);
                    animator.SetTrigger("Hpunch");
                    currentAttackType = "heavyPunch";
                }
            }
        }

        if (specialAttackPressed && animator != null && grounded)
        {
            // Trigger special attack when grounded (not limited by combo tracker)
            animator.SetBool("walk", false);
            animator.SetTrigger("special");
            currentAttackType = "special";
        }

        // Update current attack type based on animation state
        if (isPunching && currentAttackType != "punch")
            currentAttackType = "punch";
        else if (isHeavyPunching && currentAttackType != "heavyPunch")
            currentAttackType = "heavyPunch";
        else if (isLowAttacking && currentAttackType != "low")
            currentAttackType = "low";
        else if (isLowKicking && currentAttackType != "lowKick")
            currentAttackType = "lowKick";
        else if (isAirKicking && currentAttackType != "airKick")
            currentAttackType = "airKick";
        else if (isSpecialAttacking && currentAttackType != "special")
            currentAttackType = "special";
        else if (!isPunching && !isHeavyPunching && !isLowAttacking && !isLowKicking && !isAirKicking && !isSpecialAttacking)
            currentAttackType = "";

        // Continuously check for attack hits during attack animations
        if (isPunching || isHeavyPunching || isLowAttacking || isLowKicking || isAirKicking)
        {
            CheckAttackHit();
        }

        // Reset air kick to idle when landing
        if (isAirKicking && grounded)
        {
            if (animator != null)
            {
                animator.ResetTrigger("airKick");
                animator.Play("idle", 0, 0f);
            }
        }

        // Track state changes
        bool isAttacking = isPunching || isHeavyPunching || isLowAttacking || isLowKicking || isAirKicking || isSpecialAttacking;
        if (isAttacking && !wasAttacking)
        {
            // Clear the hit list at the start of a new attack
            hitObjectsThisAttack.Clear();
        }
        wasAttacking = isAttacking;
        
        // Set walk animation parameter based on horizontal movement
        if (animator != null)
        {
            // Don't set walk to true if attacking or blocking
            if (!isAttacking)
            {
                bool isWalking = Mathf.Abs(horizontal) > 0.01f;
                animator.SetBool("walk", isWalking);
            }

            // Set jump parameter based on grounded state
            animator.SetBool("jump", !grounded);
            
            // Set crouch parameter based on crouch input
            animator.SetBool("crouch", isCrouching);
        }

        // If jump requested and grounded, perform jump depending on available physics
        // Don't allow jumping while punching or heavy punching
        if (jumpPressed && grounded && !isPunching && !isHeavyPunching)
        {
            DoJump();
            // after jump, consider not grounded until physics or kinematic update
            grounded = false;
        }

        // Apply horizontal + vertical movement
        if (rb2D != null)
        {
            Vector2 vel2 = rb2D.linearVelocity;
            // Stop horizontal movement when punching, heavy punching, low attacking, low kicking, or special attacking, but keep velocity during air kick
            if (isPunching || isHeavyPunching || isLowAttacking || isLowKicking || isSpecialAttacking)
            {
                vel2.x = 0f;
            }
            else if (!isAirKicking)
            {
                // Only apply input movement if not air kicking
                float finalSpeed = currentSpeed;
                
                // Check if moving backward (opposite to facing direction)
                bool movingBackward = (facingDirection == 1 && moveDir < 0) || (facingDirection == -1 && moveDir > 0);
                if (movingBackward)
                {
                    finalSpeed *= 0.4f; // Half speed when walking backward
                }
                
                vel2.x = moveDir * finalSpeed;
            }
            // If air kicking, keep existing horizontal velocity (don't modify vel2.x)
            
            rb2D.linearVelocity = vel2;

            // Apply gravity scaling only when NOT dashing
            if (!isDashing)
            {
                if (rb2D.linearVelocity.y < 0f)
                {
                    rb2D.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;
                }
                else if (rb2D.linearVelocity.y > 0f && !jumpHeld)
                {
                    rb2D.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.deltaTime;
                }
            }
            else
            {
                // Pause gravity while dashing - zero out vertical velocity
                vel2.y = 0f;
                rb2D.linearVelocity = vel2;
            }

            // Detect when player first stops moving vertically and set ground level
            if (!groundLevelSet && Mathf.Abs(rb2D.linearVelocity.y) < 0.01f)
            {
                groundLevel = transform.position.y;
                groundLevelSet = true;
            }
        }

        // Update dash timer
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                isDashing = false;
        }

        // store previous horizontal for edge detection next frame
        previousHorizontal = horizontal;
    }

    // --- Double-tap / dash state ---
    private bool isDashing = false;
    private float dashTimer = 0f;
    private int dashDir = 0; // direction of active dash
    private float lastTapTime = -1f;
    private int lastTapDir = 0;
    private float previousHorizontal = 0f;

    private void HandleDoubleTapAndDash(float horizontalInput, out int activeDashDirection)
    {
        activeDashDirection = dashDir;

        // Determine current direction as discrete -1/0/1 using a threshold
        int curDir = 0;
        if (horizontalInput > 0.5f) curDir = 1;
        else if (horizontalInput < -0.5f) curDir = -1;

        // Detect press start: previous was near zero and now non-zero
        bool pressStarted = Mathf.Abs(previousHorizontal) < 0.5f && curDir != 0;

        if (pressStarted)
        {
            // If same direction as last tap and within doubleTapTime -> double-tap detected
            if (lastTapDir == curDir && (Time.time - lastTapTime) <= doubleTapTime)
            {
                // Start dash in that direction
                StartDash(curDir);

                // consume the tap sequence
                lastTapDir = 0;
                lastTapTime = -1f;
            }
            else
            {
                // record this tap as the first tap
                lastTapDir = curDir;
                lastTapTime = Time.time;
            }
        }

        // If enough time passes without a second tap, forget the first tap
        if (lastTapDir != 0 && (Time.time - lastTapTime) > doubleTapTime)
        {
            lastTapDir = 0;
            lastTapTime = -1f;
        }

        // expose active dash direction
        activeDashDirection = dashDir;
    }

    private void StartDash(int dir)
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashDir = dir;
    }

    private bool IsGrounded()
    {
        // If ground level hasn't been set yet, not grounded
        if (!groundLevelSet) return false;
        
        // Simple ground check: if player is at or below the ground level, they're grounded
        return transform.position.y <= groundLevel + 0.1f;
    }

    private void DoJump()
    {
        if (rb2D != null)
        {
            Vector2 v2 = rb2D.linearVelocity;
            v2.y = jumpForce;
            rb2D.linearVelocity = v2;
        }
    }

    // Called from animation event to check for attack hits
    public void CheckAttackHit()
    {
        if (attackHitbox == null)
        {
            return;
        }

        // Get the bounds of the attack hitbox
        Bounds hitboxBounds = attackHitbox.bounds;

        // Use OverlapBox to find all types of colliders in the same area as the hitbox
        // This includes BoxCollider2D, CircleCollider2D, CapsuleCollider2D, PolygonCollider2D, etc.
        Collider2D[] results = Physics2D.OverlapBoxAll(hitboxBounds.center, hitboxBounds.size, 0f);
        
        foreach (Collider2D col in results)
        {
            // Don't hit yourself or the hitbox itself
            if (col.gameObject == gameObject || col == attackHitbox)
            {
                continue;
            }

            // Only hit colliders with the "hurtbox" tag
            if (!col.CompareTag("hurtbox"))
            {
                continue;
            }

            // Don't hit your own hurtbox (check if the hurtbox's parent is this GameObject)
            if (col.transform.parent != null && col.transform.parent.gameObject == gameObject)
            {
                continue;
            }

            // Only hit each object once per attack animation
            if (hitObjectsThisAttack.Contains(col.gameObject))
            {
                continue;
            }
                
            Debug.Log("Attack hit: " + col.gameObject.name);
            
            // Add to hit list
            hitObjectsThisAttack.Add(col.gameObject);
            
            // Calculate knockback direction (away from attacker)
            Vector2 knockbackDir = (col.transform.position - transform.position).normalized;
            
            // Get attack-specific values based on current attack type
            AttackProperties properties;
            if (currentAttackType == "airKick")
            {
                properties = airKickProperties;
            }
            else if (currentAttackType == "heavyPunch")
            {
                properties = heavyPunchProperties;
            }
            else if (currentAttackType == "low")
            {
                properties = lowProperties;
            }
            else if (currentAttackType == "lowKick")
            {
                properties = lowKickProperties;
            }
            else // Default to punch values
            {
                properties = punchProperties;
            }
            
            // Create hit info and send
            HitInfo hitInfo = new HitInfo(
                properties.damage, 
                knockbackDir, 
                properties.knockbackForce, 
                properties.knockbackUpward, 
                properties.stunDuration, 
                properties.hitstopDuration,
                properties.attackHeight,
                gameObject // Pass reference to attacker
            );
            // Send message to the parent of the hurtbox collider
            if (col.transform.parent != null)
            {
                col.transform.parent.gameObject.SendMessage("OnHit", hitInfo, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                // If no parent, send to the collider's own GameObject
                col.gameObject.SendMessage("OnHit", hitInfo, SendMessageOptions.DontRequireReceiver);
            }
            
            // Apply hitstop effect with attack-specific duration
            ApplyHitstop(properties.hitstopDuration);
        }
    }

    // Apply hitstop (slow time) effect
    private void ApplyHitstop(float duration)
    {
        if (hitstopTimer <= 0f)
        {
            originalTimeScale = Time.timeScale;
        }
        hitstopTimer = duration;
        Time.timeScale = hitstopTimeScale;
    }

    // Called when this player gets hit by another player's attack
    public void OnHit(HitInfo hitInfo)
    {
        // Check if player is currently blocking using stored blocking state
        bool isCrouching = isCrouchBlocking; // If crouch blocking, they're crouching
        
        // Determine if the block is successful based on attack height
        bool blockSuccessful = false;
        if (isBlocking)
        {
            if (isStandBlocking && hitInfo.attackHeight != AttackHeight.Low)
            {
                // Standing block works against Normal and High attacks
                blockSuccessful = true;
            }
            else if (isCrouchBlocking && hitInfo.attackHeight != AttackHeight.High)
            {
                // Crouch block works against Normal and Low attacks
                blockSuccessful = true;
            }
        }
        
        if (blockSuccessful)
        {
            // Successful block - reduce damage and prevent most effects
            float blockedDamage = hitInfo.damage * 0.2f; // Take only 20% damage when blocking
            health -= blockedDamage;
            Debug.Log(gameObject.name + " blocked! Took " + blockedDamage + " damage. Health: " + health);
            
            // Trigger block animation
            if (animator != null)
            {
                animator.SetTrigger("block");
            }
            
            // Apply minimal knockback when blocking
            if (rb2D != null)
            {
                Vector2 knockback = hitInfo.knockbackDirection * (hitInfo.knockbackForce * 0.3f);
                rb2D.linearVelocity = knockback;
            }
            
            // DON'T add to attacker's combo tracker since attack was blocked
            return; // Don't apply full hit effects
        }
        
        // Normal hit (not blocked) - add to attacker's combo tracker
        if (hitInfo.attacker != null)
        {
            player attackerScript = hitInfo.attacker.GetComponent<player>();
            if (attackerScript != null && !string.IsNullOrEmpty(attackerScript.currentAttackType))
            {
                attackerScript.usedAttacksInCombo.Add(attackerScript.currentAttackType);
            }
        }
        
        health -= hitInfo.damage;
        Debug.Log(gameObject.name + " was hit! Took " + hitInfo.damage + " damage. Health: " + health);
        
        // Trigger hit flash effect
        if (spriteRenderer != null)
        {
            // Stop any existing flash coroutine to prevent overlap
            if (hitFlashCoroutine != null)
            {
                StopCoroutine(hitFlashCoroutine);
                spriteRenderer.color = originalSpriteColor; // Reset color first
            }
            hitFlashCoroutine = StartCoroutine(HitFlash());
        }
        
        // Check if already stunned before resetting animator
        bool wasAlreadyStunned = stunTimer > 0f;
        
        // Add stun time from hit info
        stunTimer += hitInfo.stunDuration;
        
        // Apply knockback using values from hit info
        if (rb2D != null)
        {
            Vector2 knockback = hitInfo.knockbackDirection * hitInfo.knockbackForce;
            knockback.y += hitInfo.knockbackUpwardForce; // Add upward force
            rb2D.linearVelocity = knockback;
        }
        
        // Check if player is dead
        if (health <= 0f)
        {
            health = 0f;
            Debug.Log(gameObject.name + " is dead!");
            
            // Restart the game after a short delay
            Invoke(nameof(RestartGame), 2f);
        }
        
        // Only interrupt current animation and return to idle if not already stunned
        if (!wasAlreadyStunned && animator != null)
        {
            animator.SetBool("walk", false);
            animator.SetBool("jump", false);
            animator.ResetTrigger("punch");
            animator.Play("idle", 0, 0f);
        }
    }

    // Coroutine to flash the sprite when hit
    private System.Collections.IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;
        
        // Change to flash color
        spriteRenderer.color = hitFlashColor;
        
        // Wait for flash duration
        yield return new WaitForSeconds(hitFlashDuration);
        
        // Return to original color
        spriteRenderer.color = originalSpriteColor;
        
        // Clear the coroutine reference
        hitFlashCoroutine = null;
    }

    // Public method to reset the combo tracker - called by opponent when they exit stun
    public void ResetOpponentCombo()
    {
        usedAttacksInCombo.Clear();
        Debug.Log(gameObject.name + "'s combo was reset");
    }

    // Public method to check if this player is stunned (used by opponent to track stun state)
    public bool IsStunned()
    {
        return stunTimer > 0f;
    }

    // Spawns a projectile for the special attack (called from animation event)
    public void SpawnProjectile()
    {
        if (projectilePrefabs == null || projectilePrefabs.Length == 0)
        {
            Debug.LogWarning("Projectile prefabs array is not assigned or empty!");
            return;
        }

        // Get character ID from GameData singleton
        int characterID = GameData.Instance.GetPlayerCharacter(playerIndex);

        // Validate character ID and use default if invalid
        if (characterID < 0 || characterID >= projectilePrefabs.Length)
        {
            Debug.LogWarning($"Invalid character ID {characterID} for projectile. Using character 0.");
            characterID = 0;
        }

        // Get the projectile prefab for this character
        GameObject projectilePrefab = projectilePrefabs[characterID];
        
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"Projectile prefab for character {characterID} is not assigned!");
            return;
        }

        // Determine spawn position
        Vector3 spawnPosition = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;

        // Instantiate the projectile
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        // Initialize projectile with attack data
        proj projectileScript = projectile.GetComponent<proj>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(specialProperties, gameObject, facingDirection, projectileSpeed);
        }

        Debug.Log(gameObject.name + " spawned a " + projectilePrefab.name + " projectile (character " + characterID + ")");
    }

    // Restart the entire game (called when a player dies)
    private void RestartGame()
    {
        // Reset time scale in case it was modified by hitstop
        Time.timeScale = 1f;
        
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}