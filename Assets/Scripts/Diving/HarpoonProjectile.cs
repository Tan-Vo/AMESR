using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class HarpoonProjectile : MonoBehaviour
{
    private Vector2 direction;
    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private float remaining;
    private float speed;
    private LayerMask mask;
    private Action<HarpoonProjectile, Collider2D> onHit;
    private void Awake() { body = GetComponent<Rigidbody2D>(); sprite = GetComponent<SpriteRenderer>(); }
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
        mask = hitMask;
        onHit = callback;
        this.sprite.sprite = sprite;
    }
    private void FixedUpdate()
    {
        float step = Mathf.Min(remaining, speed * Time.fixedDeltaTime);
        var hit = Physics2D.Raycast(body.position, direction, step, mask);
        remaining -= step;
        if (hit.collider != null || remaining <= 0f) onHit?.Invoke(this, hit.collider);
    }
    public Vector2 Position => body.position;
}
