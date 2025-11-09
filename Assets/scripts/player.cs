using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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

    public AttackProperties(float damage, float knockbackForce, float knockbackUpward, float stunDuration, float hitstopDuration)
    {
        this.damage = damage;
        this.knockbackForce = knockbackForce;
        this.knockbackUpward = knockbackUpward;
        this.stunDuration = stunDuration;
        this.hitstopDuration = hitstopDuration;
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

    public HitInfo(float damage, Vector2 knockbackDirection, float knockbackForce, float knockbackUpwardForce, float stunDuration, float hitstopDuration)
    {
        this.damage = damage;
        this.knockbackDirection = knockbackDirection;
        this.knockbackForce = knockbackForce;
        this.knockbackUpwardForce = knockbackUpwardForce;
        this.stunDuration = stunDuration;
        this.hitstopDuration = hitstopDuration;
    }
}

public class player : MonoBehaviour
{
    [Header("Input System")]
    [Tooltip("Assign the Move action (Vector2) from your Input Actions asset. Uses x component for left/right, y component for crouch.")]
    public InputActionReference moveAction;
    [Tooltip("Assign the Jump action (button) from your Input Actions asset.")]
    public InputActionReference jumpAction;
    [Tooltip("Assign the Attack action (button) from your Input Actions asset.")]
    public InputActionReference attackAction;
    [Tooltip("Assign the Heavy Attack action (button) from your Input Actions asset.")]
    public InputActionReference heavyAttackAction;

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

    [Header("Attack Properties")]
    [Tooltip("Collider2D used as the attack hitbox (assign a child GameObject's collider)")]
    public Collider2D attackHitbox;
    [Tooltip("Properties for punch attack")]
    public AttackProperties punchProperties = new AttackProperties(10f, 1f, 2f, 0.5f, 0.1f);
    [Tooltip("Properties for heavy punch attack")]
    public AttackProperties heavyPunchProperties = new AttackProperties(20f, 2f, 3f, 0.8f, 0.15f);
    [Tooltip("Properties for air kick attack")]
    public AttackProperties airKickProperties = new AttackProperties(15f, 3f, 4f, 1f, 0.1f);

    [Header("Hitstop")]
    [Tooltip("Time scale during hitstop (0 = frozen, 1 = normal)")]
    public float hitstopTimeScale = 0.1f;

    [Header("Health")]
    [Tooltip("Current health of the player")]
    public float health = 100f;
    [Tooltip("Maximum health of the player")]
    public float maxHealth = 100f;

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

    // Update is used for kinematic movement (non-physics). If you use Rigidbody, consider moving logic to FixedUpdate and use velocity or MovePosition.
    void Start()
    {
        // Ground level will be set when player first stops moving vertically
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

        // Don't allow input while stunned
        if (isStunned)
        {
            // Still update previous horizontal for dash detection
            previousHorizontal = 0f;
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        // Read from assigned Input System action (expects Vector2)
        if (moveAction != null && moveAction.action != null)
        {
            Vector2 v = moveAction.action.ReadValue<Vector2>();
            horizontal = v.x;
            vertical = v.y;
        }

        // Check if crouching (holding down)
        bool isCrouching = vertical < -0.5f;

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
        if (jumpAction != null && jumpAction.action != null)
        {
            jumpPressed = jumpAction.action.triggered;
            float val = 0f;
            try { val = jumpAction.action.ReadValue<float>(); } catch { val = 0f; }
            jumpHeld = val > 0.5f;
        }

        // --- Attack input handling ---
        bool attackPressed = false;
        if (attackAction != null && attackAction.action != null)
        {
            attackPressed = attackAction.action.triggered;
        }

        // --- Heavy Attack input handling ---
        bool heavyAttackPressed = false;
        if (heavyAttackAction != null && heavyAttackAction.action != null)
        {
            heavyAttackPressed = heavyAttackAction.action.triggered;
        }

        // --- Ground check ---
        bool grounded = IsGrounded();

        // Check if currently attacking (punch or air kick)
        bool isPunching = false;
        bool isHeavyPunching = false;
        bool isAirKicking = false;
        if (animator != null)
        {
            isPunching = animator.GetCurrentAnimatorStateInfo(0).IsName("punch");
            isHeavyPunching = animator.GetCurrentAnimatorStateInfo(0).IsName("Hpunch");
            isAirKicking = animator.GetCurrentAnimatorStateInfo(0).IsName("airKick");
        }

        if (attackPressed && animator != null)
        {
            if (grounded)
            {
                // Trigger punch from idle or walk state when grounded
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("idle") ||
                    animator.GetCurrentAnimatorStateInfo(0).IsName("walk"))
                {
                    // Force walk to false to interrupt walk and return to idle before punch
                    animator.SetBool("walk", false);
                    animator.SetTrigger("punch");
                    currentAttackType = "punch";
                }
            }
            else
            {
                // Trigger air kick when in the air
                animator.SetTrigger("airKick");
                currentAttackType = "airKick";
            }
        }

        if (heavyAttackPressed && animator != null)
        {
            if (grounded)
            {
                // Trigger heavy punch from idle or walk state when grounded
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("idle") ||
                    animator.GetCurrentAnimatorStateInfo(0).IsName("walk"))
                {
                    // Force walk to false to interrupt walk and return to idle before heavy punch
                    animator.SetBool("walk", false);
                    animator.SetTrigger("Hpunch");
                    currentAttackType = "heavyPunch";
                }
            }
        }

        // Update current attack type based on animation state
        if (isPunching && currentAttackType != "punch")
            currentAttackType = "punch";
        else if (isHeavyPunching && currentAttackType != "heavyPunch")
            currentAttackType = "heavyPunch";
        else if (isAirKicking && currentAttackType != "airKick")
            currentAttackType = "airKick";
        else if (!isPunching && !isHeavyPunching && !isAirKicking)
            currentAttackType = "";

        // Continuously check for attack hits during attack animations
        if (isPunching || isHeavyPunching || isAirKicking)
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
        bool isAttacking = isPunching || isHeavyPunching || isAirKicking;
        if (isAttacking && !wasAttacking)
        {
            // Clear the hit list at the start of a new attack
            hitObjectsThisAttack.Clear();
        }
        wasAttacking = isAttacking;
        
        // Set walk animation parameter based on horizontal movement
        if (animator != null)
        {
            // Don't set walk to true if attacking
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
            // Stop horizontal movement when punching or heavy punching, but keep velocity during air kick
            if (isPunching || isHeavyPunching)
            {
                vel2.x = 0f;
            }
            else if (!isAirKicking)
            {
                // Only apply input movement if not air kicking
                vel2.x = moveDir * currentSpeed;
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
                properties.hitstopDuration
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
        health -= hitInfo.damage;
        Debug.Log(gameObject.name + " was hit! Took " + hitInfo.damage + " damage. Health: " + health);
        
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
            // Add death logic here
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

}