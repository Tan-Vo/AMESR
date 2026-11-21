using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerStateMachine), typeof(PlayerStats))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float swimSpeed = 5f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 40f;
    [SerializeField] private float airControlMultiplier = 0.65f;
    [Header("Jumping")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float coyoteTimeDuration = 0.12f;
    [SerializeField] private float jumpBufferDuration = 0.1f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float maxFallSpeed = -20f;
    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] lowOxygenFrames;
    private Rigidbody2D rb;
    private PlayerStateMachine stateMachine;
    private PlayerStats stats;
    private JetpackSystem jetpack;
    private Vector2 moveInput;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool jumpHeld;
    private bool jumpReleased;
    private bool running;
    private bool grounded;
    private bool wasGrounded;
    private bool waterDirty;
    private bool rescuePending;
    private float rescueY;
    private float normalGravity;
    public bool IsUnderwater { get; private set; }
    public bool IsGrounded => grounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stateMachine = GetComponent<PlayerStateMachine>();
        stats = GetComponent<PlayerStats>();
        jetpack = GetComponent<JetpackSystem>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        normalGravity = rb.gravityScale;
    }
    private void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        running = Input.GetKey(KeyCode.LeftShift);
        jumpHeld = Input.GetButton("Jump");
        if (Input.GetButtonDown("Jump")) jumpBufferCounter = jumpBufferDuration;
        else jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.deltaTime);
        if (Input.GetButtonUp("Jump")) jumpReleased = true;
        UpdateState();
    }
    private void FixedUpdate()
    {
        if (rescuePending)
        {
            rescuePending = false;
            rb.position = new Vector2(rb.position.x, rescueY);
            rb.linearVelocity = Vector2.zero;
        }
        if (waterDirty)
        {
            waterDirty = false;
            rb.gravityScale = IsUnderwater ? 0f : normalGravity;
            rb.linearDamping = IsUnderwater ? 2f : 0f;
        }
        wasGrounded = grounded;
        grounded = !IsUnderwater && Physics2D.OverlapBox(groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.45f, groundCheckSize, 0f, groundLayer) != null;
        if (grounded) coyoteTimeCounter = coyoteTimeDuration;
        else coyoteTimeCounter = Mathf.Max(0f, coyoteTimeCounter - Time.fixedDeltaTime);
        if (stats.IsDead() || stateMachine.MovementLocked || stateMachine.CurrentState == PlayerState.GrappleSwinging)
        {
            if (stats.IsDead()) rb.linearVelocity = Vector2.zero;
            jumpBufferCounter = 0f;
            jumpReleased = false;
            return;
        }
        if (IsUnderwater)
        {
            Vector2 target = Vector2.ClampMagnitude(moveInput, 1f) * swimSpeed;
            if (jetpack != null && jetpack.IsJetpacking) target.y = Mathf.Max(target.y, rb.linearVelocity.y);
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            float target = moveInput.x * (running ? runSpeed : walkSpeed);
            float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
            if (!grounded) rate *= airControlMultiplier;
            rb.linearVelocity = rb.linearVelocity.WithX(Mathf.MoveTowards(rb.linearVelocity.x, target, rate * Time.fixedDeltaTime));
            if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
            {
                rb.linearVelocity = rb.linearVelocity.WithY(jumpForce);
                grounded = false;
                coyoteTimeCounter = jumpBufferCounter = 0f;
                stateMachine.ChangeState(PlayerState.Jumping);
            }
            if (jumpReleased && rb.linearVelocity.y > 0f) rb.linearVelocity = rb.linearVelocity.WithY(rb.linearVelocity.y * 0.5f);
            float multiplier = rb.linearVelocity.y < 0f ? fallMultiplier : !jumpHeld ? lowJumpMultiplier : 1f;
            if (!grounded && (jetpack == null || !jetpack.IsJetpacking))
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * rb.gravityScale * (multiplier - 1f) * Time.fixedDeltaTime;
            rb.linearVelocity = rb.linearVelocity.WithY(Mathf.Max(rb.linearVelocity.y, maxFallSpeed));
            if (grounded && !wasGrounded) stateMachine.ChangeState(PlayerState.Landing);
        }
        jumpReleased = false;
    }
    private void UpdateState()
    {
        if (!stateMachine.IsLocomotion || stateMachine.CurrentState == PlayerState.Landing) return;
        PlayerState next;
        if (jetpack != null && jetpack.IsJetpacking) next = PlayerState.Jetpacking;
        else if (IsUnderwater) next = moveInput.y < -0.1f ? PlayerState.Diving : PlayerState.Swimming;
        else if (!grounded) next = rb.linearVelocity.y > 0f ? PlayerState.Jumping : PlayerState.Falling;
        else next = Mathf.Abs(moveInput.x) > 0.01f ? running ? PlayerState.Running : PlayerState.Walking : PlayerState.Idle;
        stateMachine.ChangeState(next);
    }
    private void LateUpdate()
    {
        if (spriteRenderer != null && Mathf.Abs(moveInput.x) > 0.01f) spriteRenderer.flipX = moveInput.x < 0f;
        if (animator != null)
        {
            animator.SetFloat(AnimatorHashes.Speed, Mathf.Abs(rb.linearVelocity.x));
            animator.SetFloat(AnimatorHashes.VelocityY, rb.linearVelocity.y);
            animator.SetBool(AnimatorHashes.IsRunning, running && grounded && Mathf.Abs(moveInput.x) > 0.01f);
            animator.SetBool(AnimatorHashes.IsJumping, !grounded && !IsUnderwater);
            animator.SetBool(AnimatorHashes.IsSwimming, IsUnderwater);
            animator.SetBool(AnimatorHashes.IsDiving, IsUnderwater && moveInput.y < 0f);
            animator.SetBool(AnimatorHashes.IsHurt, stateMachine.CurrentState == PlayerState.Hurt);
            animator.SetBool(AnimatorHashes.IsDead, stats.IsDead());
            animator.SetBool(AnimatorHashes.IsMining, stateMachine.CurrentState == PlayerState.Mining);
            animator.SetBool(AnimatorHashes.IsShooting, stateMachine.CurrentState == PlayerState.Combat_Ranged);
            animator.SetBool(AnimatorHashes.IsSlashing, stateMachine.CurrentState == PlayerState.Combat_Melee);
            animator.SetBool(AnimatorHashes.IsHarpooning, stateMachine.CurrentState == PlayerState.HarpoonAiming || stateMachine.CurrentState == PlayerState.HarpoonReeling);
            animator.SetBool(AnimatorHashes.IsWaitering, stateMachine.CurrentState >= PlayerState.Waiter_Idle && stateMachine.CurrentState <= PlayerState.Waiter_Serving);
        }
        if (spriteRenderer != null && IsUnderwater && stats.IsLowOxygen() && !stats.IsDead() &&
            stateMachine.CurrentState != PlayerState.Hurt && lowOxygenFrames != null && lowOxygenFrames.Length > 0)
            spriteRenderer.sprite = lowOxygenFrames[(int)(Time.time * 8f) % lowOxygenFrames.Length];
    }
    /// <summary>Requests a water-mode change on the next physics step.</summary>
    public void SetUnderwater(bool underwater)
    {
        IsUnderwater = underwater;
        stateMachine.InWater = underwater;
        waterDirty = true;
        if (stateMachine.IsLocomotion) stateMachine.ChangeState(underwater ? PlayerState.Swimming : PlayerState.Falling);
    }
    /// <summary>Requests an emergency teleport above the water surface.</summary>
    public void RequestSurface(float y)
    {
        rescueY = y;
        rescuePending = true;
        SetUnderwater(false);
    }
}
