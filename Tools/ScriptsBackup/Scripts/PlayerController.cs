using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;
    [Header("Ground Check")]
    [Tooltip("Khoảng cách kiểm tra mặt đất từ tâm nhân vật xuống dưới")]
    public float groundCheckDistance = 0.1f;
    [Tooltip("Layer nào được tính là mặt đất (nên chọn Ground hoặc Default)")]
    public LayerMask groundLayer;
    private Rigidbody2D rb;
    private Collider2D col;
    private float moveInput;
    private bool jumpRequested;
    private bool isGrounded;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
    }
    void Update()
    {
        moveInput = GetHorizontalInput();
        isGrounded = CheckGrounded();
        if (GetJumpInputDown() && isGrounded)
        {
            jumpRequested = true;
        }
    }
    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpRequested = false;
        }
    }
    private bool CheckGrounded()
    {
        if (col == null) return false;
        Bounds bounds = col.bounds;
        Vector2 checkPosition = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 boxSize = new Vector2(bounds.size.x * 0.9f, 0.05f);
        Collider2D hit = Physics2D.OverlapBox(checkPosition, boxSize, 0f);
        if (hit != null && hit.gameObject != gameObject)
        {
            if (groundLayer.value == 0 || ((1 << hit.gameObject.layer) & groundLayer.value) != 0 || hit.CompareTag("Ground"))
            {
                return true;
            }
        }
        return false;
    }
    private float GetHorizontalInput()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            float input = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input += 1f;
            if (input != 0f) return input;
        }
#endif
        try
        {
            return Input.GetAxisRaw("Horizontal");
        }
        catch
        {
            return 0f;
        }
    }
    private bool GetJumpInputDown()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame))
        {
            return true;
        }
#endif
        try
        {
            return Input.GetButtonDown("Jump");
        }
        catch
        {
            return false;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null)
        {
            Gizmos.color = Color.green;
            Bounds bounds = c.bounds;
            Vector2 checkPosition = new Vector2(bounds.center.x, bounds.min.y);
            Vector2 boxSize = new Vector2(bounds.size.x * 0.9f, 0.05f);
            Gizmos.DrawWireCube(checkPosition, boxSize);
        }
    }
}