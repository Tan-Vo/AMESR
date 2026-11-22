using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
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
    [SerializeField] private SO_PlayerEffectEvent onPlayerEffectRequested;
    [SerializeField] private SO_GameEvent onJumped, onLanded;
    private Rigidbody2D rb;
    private PlayerStateMachine stateMachine;
    private PlayerStats stats;
    private JetpackSystem jetpack;
    private RuntimeAnimatorController cachedAnimatorController;
    private readonly Dictionary<int, AnimatorControllerParameterType> animatorParameterTypes = new();
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
    private Vector2 externalDrift;
    private Vector2 previousDrift;
    private Vector2 teleportPosition;
    private bool teleportPending;
    private float slowRemaining;
    private float slowScale = 1f;
    public bool IsUnderwater { get; private set; }
    public bool IsGrounded => grounded;
    private void OnEnable() { onPlayerEffectRequested?.RegisterListener(ApplyEffect); }
    private void OnDisable() { onPlayerEffectRequested?.UnregisterListener(ApplyEffect); }
    private void ApplyEffect(PlayerEffectKind kind, Vector2 value, float amount)
    {
        if (kind == PlayerEffectKind.Teleport) { teleportPosition = value; teleportPending = true; }
        else if (kind == PlayerEffectKind.Stun) { stateMachine.Stun(amount); }
        else if (kind == PlayerEffectKind.Slow) { slowRemaining = Mathf.Max(slowRemaining, value.x); slowScale = Mathf.Clamp(amount, 0.1f, 1f); }
        else if (rb != null) externalDrift += (value - rb.position).normalized * amount;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stateMachine = GetComponent<PlayerStateMachine>();
        if (stateMachine == null) stateMachine = gameObject.AddComponent<PlayerStateMachine>();
        stats = GetComponent<PlayerStats>();
        if (stats == null) stats = gameObject.AddComponent<PlayerStats>();
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
        rb.linearVelocity -= previousDrift;
        previousDrift = Vector2.zero;
        slowRemaining = Mathf.Max(0f, slowRemaining - Time.fixedDeltaTime);
        float motionScale = slowRemaining > 0f ? slowScale : 1f;
        if (teleportPending)
        {
            teleportPending = false; rb.position = teleportPosition; rb.linearVelocity = Vector2.zero;
            externalDrift = Vector2.zero;
        }
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
            ApplyDrift();
            return;
        }
        if (IsUnderwater)
        {
            Vector2 target = Vector2.ClampMagnitude(moveInput, 1f) * swimSpeed * motionScale;
            if (jetpack != null && jetpack.IsJetpacking) target.y = Mathf.Max(target.y, rb.linearVelocity.y);
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            float target = moveInput.x * (running ? runSpeed : walkSpeed) * motionScale;
            float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
            if (!grounded) rate *= airControlMultiplier;
            rb.linearVelocity = rb.linearVelocity.WithX(Mathf.MoveTowards(rb.linearVelocity.x, target, rate * Time.fixedDeltaTime));
            if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
            {
                rb.linearVelocity = rb.linearVelocity.WithY(jumpForce);
                grounded = false;
                coyoteTimeCounter = jumpBufferCounter = 0f;
                stateMachine.ChangeState(PlayerState.Jumping);
                onJumped?.Raise();
            }
            if (jumpReleased && rb.linearVelocity.y > 0f) rb.linearVelocity = rb.linearVelocity.WithY(rb.linearVelocity.y * 0.5f);
            float multiplier = rb.linearVelocity.y < 0f ? fallMultiplier : !jumpHeld ? lowJumpMultiplier : 1f;
            if (!grounded && (jetpack == null || !jetpack.IsJetpacking))
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * rb.gravityScale * (multiplier - 1f) * Time.fixedDeltaTime;
            rb.linearVelocity = rb.linearVelocity.WithY(Mathf.Max(rb.linearVelocity.y, maxFallSpeed));
            if (grounded && !wasGrounded) { stateMachine.ChangeState(PlayerState.Landing); onLanded?.Raise(); }
        }
        jumpReleased = false;
        ApplyDrift();
    }
    private void ApplyDrift()
    {
        previousDrift = stats.IsDead() ? Vector2.zero : externalDrift;
        rb.linearVelocity += previousDrift;
        externalDrift = Vector2.zero;
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
        UpdateAnimator();
        if (spriteRenderer != null && IsUnderwater && stats.IsLowOxygen() && !stats.IsDead() &&
            stateMachine.CurrentState != PlayerState.Hurt && lowOxygenFrames != null && lowOxygenFrames.Length > 0)
            spriteRenderer.sprite = lowOxygenFrames[(int)(Time.time * 8f) % lowOxygenFrames.Length];
    }
    private void UpdateAnimator()
    {
        if (animator == null || !animator.isActiveAndEnabled || !animator.isInitialized) return;
        var controller = animator.runtimeAnimatorController;
        if (controller == null) return;
        if (controller != cachedAnimatorController)
        {
            cachedAnimatorController = controller;
            animatorParameterTypes.Clear();
            foreach (var parameter in animator.parameters)
                animatorParameterTypes[parameter.nameHash] = parameter.type;
        }

        SetAnimatorFloat(AnimatorHashes.Speed, Mathf.Abs(rb.linearVelocity.x));
        SetAnimatorFloat(AnimatorHashes.VelocityY, rb.linearVelocity.y);
        SetAnimatorBool(AnimatorHashes.IsRunning, running && grounded && Mathf.Abs(moveInput.x) > 0.01f);
        SetAnimatorBool(AnimatorHashes.IsJumping, !grounded && !IsUnderwater);
        SetAnimatorBool(AnimatorHashes.IsSwimming, IsUnderwater);
        SetAnimatorBool(AnimatorHashes.IsDiving, IsUnderwater && moveInput.y < 0f);
        SetAnimatorBool(AnimatorHashes.IsHurt, stateMachine.CurrentState == PlayerState.Hurt);
        SetAnimatorBool(AnimatorHashes.IsDead, stats.IsDead());
        SetAnimatorBool(AnimatorHashes.IsMining, stateMachine.CurrentState == PlayerState.Mining);
        SetAnimatorBool(AnimatorHashes.IsShooting, stateMachine.CurrentState == PlayerState.Combat_Ranged);
        SetAnimatorBool(AnimatorHashes.IsSlashing, stateMachine.CurrentState == PlayerState.Combat_Melee);
        SetAnimatorBool(AnimatorHashes.IsHarpooning, stateMachine.CurrentState == PlayerState.HarpoonAiming || stateMachine.CurrentState == PlayerState.HarpoonReeling);
        SetAnimatorBool(AnimatorHashes.IsWaitering, stateMachine.CurrentState >= PlayerState.Waiter_Idle && stateMachine.CurrentState <= PlayerState.Waiter_Serving);
    }
    private void SetAnimatorBool(int hash, bool value)
    {
        if (animatorParameterTypes.TryGetValue(hash, out var type) && type == AnimatorControllerParameterType.Bool)
            animator.SetBool(hash, value);
    }
    private void SetAnimatorFloat(int hash, float value)
    {
        if (animatorParameterTypes.TryGetValue(hash, out var type) && type == AnimatorControllerParameterType.Float)
            animator.SetFloat(hash, value);
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
