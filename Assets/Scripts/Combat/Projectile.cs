using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class Projectile : MonoBehaviour
{
    [SerializeField] private SO_DamageEvent onDamageRequested;
    private ProjectileObjectPool owner;
    private SpriteRenderer sprite;
    private Rigidbody2D body;
    private Vector2 direction;
    private float speed;
    private float range;
    private float lifetime;
    private int damage;
    private LayerMask hitMask;
    private void Awake() { sprite = GetComponent<SpriteRenderer>(); body = GetComponent<Rigidbody2D>(); }
    /// <summary>Initializes a leased projectile; movement uses swept raycasts to prevent tunneling.</summary>
    public void Launch(ProjectileObjectPool pool, SO_WeaponData weapon, Vector2 origin, Vector2 aim, LayerMask mask)
    {
        owner = pool;
        transform.SetParent(null, true);
        direction = aim.normalized;
        speed = Mathf.Max(1f, weapon.ProjectileSpeed);
        body.position = origin;
        body.rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        body.linearVelocity = direction * speed;
        range = Mathf.Max(0f, weapon.Range);
        lifetime = 3f;
        damage = weapon.BaseDamage;
        hitMask = mask;
        sprite.sprite = weapon.ProjectileSprite;
    }
    private void FixedUpdate()
    {
        float distance = Mathf.Min(range, speed * Time.fixedDeltaTime);
        var hit = Physics2D.Raycast(body.position, direction, distance, hitMask);
        range -= distance;
        lifetime -= Time.fixedDeltaTime;
        if (hit.collider != null) onDamageRequested?.Hit(hit.collider, damage);
        if (hit.collider != null || range <= 0f || lifetime <= 0f) owner.Return(this);
    }
}
