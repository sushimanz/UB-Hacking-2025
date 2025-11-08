using UnityEngine;
using UnityEngine.InputSystem;

public class player : MonoBehaviour
{
    [Header("Input System")]
    [Tooltip("Assign the Move action (Vector2) from your Input Actions asset. Uses x component for left/right.")]
    public InputActionReference moveAction;
    [Tooltip("Assign the Jump action (button) from your Input Actions asset.")]
    public InputActionReference jumpAction;

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
    [Tooltip("Jump impulse/velocity. If Rigidbody present, applied as an impulse; otherwise used as kinematic vertical velocity.")]
    public float jumpForce = 6f;
    [Tooltip("Gravity applied when using kinematic fallback (negative value)")]
    public float gravity = -30f;
    [Tooltip("Multiplier applied to gravity when falling (makes falls faster)")]
    public float fallMultiplier = 2.5f;
    [Tooltip("Multiplier applied to gravity when jump is released early (makes low jumps)")]
    public float lowJumpMultiplier = 2f;
    [Tooltip("Layer(s) considered ground for grounding checks")]
    public LayerMask groundLayer = ~0;
    [Tooltip("Optional tag to identify ground objects. If non-empty, grounding will check the hit collider's tag instead of using the layer mask.")]
    public string groundTag = "Ground";
    [Tooltip("Distance from the origin (or groundCheck) to check for ground contact")]
    public float groundCheckDistance = 0.15f;
    [Tooltip("Optional transform to use as the ground-check origin. If null, player's position is used.")]
    public Transform groundCheck;

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Enable();
        if (jumpAction != null && jumpAction.action != null)
            jumpAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Disable();
        if (jumpAction != null && jumpAction.action != null)
            jumpAction.action.Disable();
    }

    // cached physics components
    private Rigidbody rb;
    private Rigidbody2D rb2D;
    // kinematic fallback vertical velocity
    private float verticalVelocity = 0f;
    private bool jumpHeld = false;

    // Update is used for kinematic movement (non-physics). If you use Rigidbody, consider moving logic to FixedUpdate and use velocity or MovePosition.
    void Update()
    {
        // cache physics components if not already
        if (rb == null && rb2D == null)
        {
            rb = GetComponent<Rigidbody>();
            rb2D = GetComponent<Rigidbody2D>();
        }

        float horizontal = 0f;

        // Prefer reading from assigned Input System action (expects Vector2). If not assigned, fallback to Keyboard for A/D.
        if (moveAction != null && moveAction.action != null)
        {
            // Many InputAction maps use a Vector2 for movement (x = left/right). Read that and use x.
            Vector2 v = moveAction.action.ReadValue<Vector2>();
            horizontal = v.x;
        }
        else if (Keyboard.current != null)
        {
            // Simple keyboard fallback so the script still works without wiring the Input Actions asset.
            float left = Keyboard.current.aKey.isPressed ? -1f : 0f;
            float right = Keyboard.current.dKey.isPressed ? 1f : 0f;
            horizontal = left + right;
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
            // triggered is true on performed for button-like actions
            jumpPressed = jumpAction.action.triggered;
            // read held state (some button actions return float 0/1)
            float val = 0f;
            try { val = jumpAction.action.ReadValue<float>(); } catch { val = 0f; }
            jumpHeld = val > 0.5f;
        }
        else if (Keyboard.current != null)
        {
            jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
            jumpHeld = Keyboard.current.spaceKey.isPressed;
        }

        // Ground check
        bool grounded = IsGrounded();

        // If jump requested and grounded, perform jump depending on available physics
        if (jumpPressed && grounded)
        {
            DoJump();
            // after jump, consider not grounded until physics or kinematic update
            grounded = false;
        }

        // Apply horizontal + vertical movement depending on physics availability
        if (rb != null)
        {
            // 3D rigidbody: set horizontal velocity while preserving existing Y and Z velocity
            Vector3 vel = rb.linearVelocity;
            vel.x = moveDir * currentSpeed;
            rb.linearVelocity = vel;

            // apply stronger gravity while falling and increased gravity when jump is released early
            if (rb.linearVelocity.y < 0f)
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;
            }
            else if (rb.linearVelocity.y > 0f && !jumpHeld)
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1f) * Time.deltaTime;
            }
        }
        else if (rb2D != null)
        {
            Vector2 vel2 = rb2D.linearVelocity;
            vel2.x = moveDir * currentSpeed;
            rb2D.linearVelocity = vel2;

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
            // Kinematic fallback: apply gravity and vertical velocity, then translate
            if (!grounded)
            {
                // stronger gravity when falling
                if (verticalVelocity < 0f)
                    verticalVelocity += gravity * fallMultiplier * Time.deltaTime;
                else if (verticalVelocity > 0f && !jumpHeld)
                    verticalVelocity += gravity * lowJumpMultiplier * Time.deltaTime;
                else
                    verticalVelocity += gravity * Time.deltaTime;
            }
            else if (verticalVelocity < 0f)
            {
                // small negative value to keep grounded
                verticalVelocity = -2f;
            }

            Vector3 movementK = new Vector3(moveDir * currentSpeed * Time.deltaTime, verticalVelocity * Time.deltaTime, 0f);
            transform.Translate(movementK, Space.World);
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
        // If we have a Rigidbody2D, check its current contact colliders and compare tags.
        if (rb2D != null)
        {
            Collider2D[] contacts = new Collider2D[16];
            int count = rb2D.GetContacts(contacts);
            for (int i = 0; i < count; i++)
            {
                var col = contacts[i];
                if (col == null) continue;
                if ((!string.IsNullOrEmpty(groundTag) && col.CompareTag(groundTag)) || col.CompareTag("ground"))
                    return true;
            }
            return false;
        }

        // Fallback: use a small overlap at the groundCheck position to detect ground by tag
        Vector3 origin = (groundCheck != null) ? groundCheck.position : transform.position;
        float radius = groundCheckDistance + 0.01f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(new Vector2(origin.x, origin.y), radius);
        foreach (var c in hits)
        {
            if (c == null) continue;
            if ((!string.IsNullOrEmpty(groundTag) && c.CompareTag(groundTag)) || c.CompareTag("ground"))
                return true;
        }
        return false;
    }

    private void DoJump()
    {
        if (rb != null)
        {
            // reset vertical velocity then set jump velocity
            Vector3 v = rb.linearVelocity;
            v.y = jumpForce;
            rb.linearVelocity = v;
        }
        else if (rb2D != null)
        {
            Vector2 v2 = rb2D.linearVelocity;
            v2.y = jumpForce;
            rb2D.linearVelocity = v2;
        }
        else
        {
            verticalVelocity = jumpForce;
        }
    }

}
