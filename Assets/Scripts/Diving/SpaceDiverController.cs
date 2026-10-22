using UnityEngine;
using CosmicDiver.Core;

namespace CosmicDiver.Diving
{
    /// <summary>
    /// Điều khiển Thợ Lặn Kai trong môi trường Biển Tinh Vân Vũ Trụ (Fluid / Zero-G Space Diving)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class SpaceDiverController : MonoBehaviour
    {
        [Header("Thông Số Di Chuyển")]
        [SerializeField] private float swimSpeed = 4.5f;
        [SerializeField] private float boostSpeed = 8.5f;
        [SerializeField] private float waterDrag = 2.0f;

        [Header("Độ Sâu Môi Trường")]
        [Tooltip("Tọa độ Y của mặt nước / mặt biển tinh vân")]
        [SerializeField] private float surfaceY = 0f;
        [SerializeField] private float currentDepth = 0f;

        [Header("Tham Chiếu Hệ Thống")]
        [SerializeField] private OxygenSystem oxygenSystem;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private bool isBoosting;
        private bool isAiming;
        private DiverActionState currentState = DiverActionState.Idle;

        public DiverActionState CurrentState => currentState;
        public float CurrentDepth => currentDepth;
        public bool IsBoosting => isBoosting;
        public bool IsAiming => isAiming;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (oxygenSystem == null) oxygenSystem = GetComponent<OxygenSystem>();

            // Cấu hình vật lý môi trường chất lỏng không gian
            rb.gravityScale = 0f;
            rb.linearDamping = waterDrag;
            rb.freezeRotation = true;
        }

        private void Update()
        {
            // 1. Tính toán độ sâu hiện tại (mét)
            currentDepth = Mathf.Max(0f, surfaceY - transform.position.y);

            // 2. Nhận tín hiệu điều khiển
            HandleInput();

            // 3. Cập nhật dưỡng khí
            if (oxygenSystem != null)
            {
                oxygenSystem.ConsumeOxygen(currentDepth, isBoosting);
            }

            // 4. Cập nhật trạng thái Thợ Lặn
            UpdateActionState();

            // 5. Đổi hướng nhìn nhân vật (Flip Sprite)
            HandleFacingDirection();
        }

        private void FixedUpdate()
        {
            // Di chuyển vật lý 8 hướng mượt mà
            float targetSpeed = isBoosting ? boostSpeed : swimSpeed;
            if (isAiming) targetSpeed *= 0.5f; // Di chuyển chậm lại khi giương súng ngắm

            if (moveInput.sqrMagnitude > 0.01f)
            {
                rb.AddForce(moveInput * (targetSpeed * 10f));
            }
        }

        private void HandleInput()
        {
            float horizontal = 0f;
            float vertical = 0f;

            // Hỗ trợ cả Input Manager và New Input System
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;

                isBoosting = (keyboard.leftShiftKey.isPressed || keyboard.spaceKey.isPressed) && (moveInput.sqrMagnitude > 0.1f);
            }
#else
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
            isBoosting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Space)) && (moveInput.sqrMagnitude > 0.1f);
#endif
            moveInput = new Vector2(horizontal, vertical).normalized;

            // Nhấp chuột phải để ngắm súng móc
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            isAiming = mouse != null && mouse.rightButton.isPressed;
#else
            isAiming = Input.GetMouseButton(1);
#endif
        }

        private void UpdateActionState()
        {
            if (oxygenSystem != null && oxygenSystem.CurrentOxygen <= 0f)
            {
                currentState = DiverActionState.Dead;
                return;
            }

            if (oxygenSystem != null && oxygenSystem.IsPanicking)
            {
                currentState = DiverActionState.OxygenPanic;
                return;
            }

            if (isAiming)
            {
                currentState = DiverActionState.AimingHarpoon;
                return;
            }

            if (isBoosting)
            {
                currentState = DiverActionState.Boosting;
                return;
            }

            if (moveInput.sqrMagnitude > 0.05f)
            {
                currentState = DiverActionState.Swimming;
                return;
            }

            currentState = DiverActionState.Idle;
        }

        private void HandleFacingDirection()
        {
            if (spriteRenderer == null) return;

            if (isAiming)
            {
                // Hướng mặt theo vị trí con trỏ chuột
                Vector3 mouseWorldPos = Camera.main != null ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : transform.position;
                spriteRenderer.flipX = mouseWorldPos.x < transform.position.x;
            }
            else if (Mathf.Abs(moveInput.x) > 0.1f)
            {
                // Hướng mặt theo chiều bơi
                spriteRenderer.flipX = moveInput.x < 0;
            }
        }
    }
}
