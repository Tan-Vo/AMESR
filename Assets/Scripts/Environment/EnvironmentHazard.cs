using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public sealed class EnvironmentHazard : MonoBehaviour
{
    [SerializeField] private EnvironmentHazardType hazardType;
    [SerializeField] private SO_IntEvent onPlayerDamageRequested;
    [SerializeField] private SO_PlayerEffectEvent onPlayerEffectRequested;
    [SerializeField] private Sprite[] frames;
    private SpriteRenderer sprite;
    private Rigidbody2D body;
    private Vector2 origin;
    private float timer, cooldown;
    private void Awake() { sprite = GetComponent<SpriteRenderer>(); body = GetComponent<Rigidbody2D>(); origin = transform.position; }
    private void FixedUpdate()
    {
        timer += Time.fixedDeltaTime; cooldown -= Time.fixedDeltaTime;
        if (hazardType == EnvironmentHazardType.CrushingWall && body != null) body.MovePosition(origin + Vector2.right * Mathf.Sin(timer * 0.5f) * 2f);
    }
    private void OnTriggerStay2D(Collider2D other) { Affect(other); }
    private void OnCollisionStay2D(Collision2D collision) { Affect(collision.collider); }
    private void Affect(Collider2D player)
    {
        if (!player.CompareTag("Player") || cooldown > 0f || hazardType == EnvironmentHazardType.LavaVent && timer % 8f >= 5f) return;
        if (hazardType == EnvironmentHazardType.CrushingWall)
        {
            Vector2 position = player.bounds.center;
            var left = Physics2D.Raycast(position, Vector2.left, 0.6f, 1 << 9);
            var right = Physics2D.Raycast(position, Vector2.right, 0.6f, 1 << 9);
            if (left.collider == null || right.collider == null || left.collider == right.collider) return;
        }
        int damage = hazardType == EnvironmentHazardType.PoisonCloud ? 3 : hazardType == EnvironmentHazardType.LavaVent ? 20 : hazardType == EnvironmentHazardType.ElectricField ? 10 : 10000;
        onPlayerDamageRequested?.Raise(damage); cooldown = 1f;
        if (hazardType == EnvironmentHazardType.ElectricField) onPlayerEffectRequested?.Raise(PlayerEffectKind.Stun, Vector2.zero, 1f);
    }
    private void LateUpdate()
    {
        if (frames != null && frames.Length > 0) sprite.sprite = frames[(int)(Time.time * 15) % frames.Length];
        sprite.color = hazardType == EnvironmentHazardType.LavaVent && timer % 8f >= 5f ? new Color(1, 1, 1, 0.25f) : Color.white;
    }
}
