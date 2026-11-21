using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class HarpoonProjectile : MonoBehaviour
{
    [SerializeField] private SO_PhysicsFieldEvent onPhysicsFieldRequested;
    private Vector2 direction;
    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private float remaining;
    private float lifetime;
    private float speed;
    private LayerMask mask;
    private Action<HarpoonProjectile, Collider2D> onHit;
    private void Awake() { body = GetComponent<Rigidbody2D>(); sprite = GetComponent<SpriteRenderer>(); }
    private void OnEnable() { onPhysicsFieldRequested?.RegisterListener(ApplyField); }
    private void OnDisable() { onPhysicsFieldRequested?.UnregisterListener(ApplyField); }
    private void ApplyField(Vector2 center, float radius, float strength)
    {
        if ((center - body.position).sqrMagnitude <= radius * radius)
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, (center - body.position).normalized * strength, strength * Time.fixedDeltaTime);
    }
    /// <summary>Launches a leased tip; the owning system returns it on hit or exhaustion.</summary>
    public void Launch(Vector2 origin, Vector2 aim, float velocity, float range, LayerMask hitMask, Sprite sprite, Action<HarpoonProjectile, Collider2D> callback)
    {
        transform.SetParent(null, true);
        direction = aim.normalized;
        speed = Mathf.Max(1f, velocity);
        body.position = origin;
        body.rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        body.linearVelocity = direction * speed;
        remaining = Mathf.Max(0f, range);
        lifetime = 3f;
        mask = hitMask;
        onHit = callback;
        this.sprite.sprite = sprite;
    }
    private void FixedUpdate()
    {
        direction = body.linearVelocity.normalized;
        float step = Mathf.Min(remaining, body.linearVelocity.magnitude * Time.fixedDeltaTime);
        var hit = Physics2D.Raycast(body.position, direction, step, mask);
        remaining -= step;
        lifetime -= Time.fixedDeltaTime;
        if (hit.collider != null || remaining <= 0f || lifetime <= 0f) onHit?.Invoke(this, hit.collider);
    }
    public Vector2 Position => body.position;
}
