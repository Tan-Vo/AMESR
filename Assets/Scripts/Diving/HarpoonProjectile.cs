using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class HarpoonProjectile : MonoBehaviour
{
    private Vector2 direction;
    private Vector2 position;
    private float remaining;
    private float speed;
    private LayerMask mask;
    private Action<HarpoonProjectile, Collider2D> onHit;
    /// <summary>Launches a leased tip; the owning system returns it on hit or exhaustion.</summary>
    public void Launch(Vector2 origin, Vector2 aim, float velocity, float range, LayerMask hitMask, Sprite sprite, Action<HarpoonProjectile, Collider2D> callback)
    {
        transform.SetParent(null, true);
        position = origin;
        direction = aim.normalized;
        speed = Mathf.Max(1f, velocity);
        remaining = Mathf.Max(0f, range);
        mask = hitMask;
        onHit = callback;
        GetComponent<SpriteRenderer>().sprite = sprite;
    }
    private void FixedUpdate()
    {
        float step = Mathf.Min(remaining, speed * Time.fixedDeltaTime);
        var hit = Physics2D.Raycast(position, direction, step, mask);
        position = hit.collider != null ? hit.point : position + direction * step;
        remaining -= step;
        if (hit.collider != null || remaining <= 0f) onHit?.Invoke(this, hit.collider);
    }
    private void LateUpdate() { transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg)); }
    public Vector2 Position => position;
}
