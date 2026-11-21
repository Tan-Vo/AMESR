using UnityEngine;

public enum BossAttackKind { Sweep, InkShot, Puddle, SlowField, Ring, Lightning, SonicWave, Shockwave, GravityWell, Asteroid, Telegraph, Vortex, AddBolt, Explosion }

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public sealed class BossHazard : MonoBehaviour
{
    [SerializeField] private SO_IntEvent onPlayerDamageRequested;
    [SerializeField] private SO_PlayerEffectEvent onPlayerEffectRequested;
    [SerializeField] private SO_PhysicsFieldEvent onPhysicsFieldRequested;
    [SerializeField] private Sprite[] telegraphFrames;
    [SerializeField] private Sprite[] vortexFrames;
    [SerializeField] private Sprite[] poisonFrames;
    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private BossArenaDirector arena;
    private BossAttackKind kind;
    private Vector2 direction;
    private Sprite appearance;
    private float speed, radius, remaining, delay, age, hitCooldown;
    private int damage;
    private bool started;
    private bool Moving => kind == BossAttackKind.InkShot || kind == BossAttackKind.Asteroid || kind == BossAttackKind.AddBolt || kind == BossAttackKind.Sweep || kind == BossAttackKind.SonicWave;
    private void Awake() { body = GetComponent<Rigidbody2D>(); sprite = GetComponent<SpriteRenderer>(); }
    private void OnEnable() { onPhysicsFieldRequested?.RegisterListener(ApplyField); }
    private void OnDisable() { onPhysicsFieldRequested?.UnregisterListener(ApplyField); }
    /// <summary>Initializes one pooled, telegraphed attack without runtime entity allocation.</summary>
    public void Initialize(BossArenaDirector director, BossAttackKind attack, Vector2 position, Vector2 aim, int amount, float velocity, float size, float duration, float windup, Sprite effect)
    {
        arena = director; kind = attack; direction = aim.normalized; damage = amount; speed = velocity; radius = size;
        remaining = duration; delay = windup; appearance = effect; age = hitCooldown = 0f; started = false;
        body.position = position; body.linearVelocity = Vector2.zero; body.rotation = 0f;
    }
    private void ApplyField(Vector2 center, float range, float strength)
    {
        if (!started || !Moving || (center - body.position).sqrMagnitude > range * range) return;
        body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, (center - body.position).normalized * strength, strength * Time.fixedDeltaTime);
    }
    private void FixedUpdate()
    {
        if (arena == null || arena.Player == null) return;
        float dt = Time.fixedDeltaTime;
        if (delay > 0f) { delay -= dt; return; }
        if (!started) { started = true; if (Moving) body.linearVelocity = direction * speed; }
        age += dt; remaining -= dt; hitCooldown -= dt;
        if (remaining <= 0f) { Expire(kind == BossAttackKind.InkShot); return; }
        Vector2 delta = (Vector2)arena.Player.position - body.position;
        if (kind == BossAttackKind.InkShot || kind == BossAttackKind.Asteroid || kind == BossAttackKind.AddBolt)
        {
            Vector2 velocity = body.linearVelocity;
            var hit = Physics2D.CircleCast(body.position, radius, velocity.normalized, velocity.magnitude * dt, (1 << 6) | (1 << 9) | (1 << 10));
            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Player")) onPlayerDamageRequested?.Raise(damage);
                Expire(kind == BossAttackKind.InkShot);
            }
            return;
        }
        if (kind == BossAttackKind.SlowField)
        {
            if (delta.sqrMagnitude < radius * radius) onPlayerEffectRequested?.Raise(PlayerEffectKind.Slow, new Vector2(0.1f, 0), 0.5f);
            return;
        }
        if (kind == BossAttackKind.Vortex)
        {
            if (arena.CurrentBoss != null) body.position = arena.CurrentBoss.transform.position;
            onPlayerEffectRequested?.Raise(PlayerEffectKind.Pull, body.position, speed);
            return;
        }
        if (kind == BossAttackKind.GravityWell)
        {
            onPhysicsFieldRequested?.Raise(body.position, 40f, 12f);
            onPlayerEffectRequested?.Raise(PlayerEffectKind.Pull, body.position, 6f);
        }
        bool contact = kind == BossAttackKind.Lightning ? Mathf.Abs(delta.x) < radius :
            kind == BossAttackKind.SonicWave ? Mathf.Abs(delta.x) < 16f && Mathf.Abs(delta.y) < radius :
            kind == BossAttackKind.Ring ? Mathf.Abs(delta.magnitude - age * speed) < radius :
            delta.sqrMagnitude < radius * radius;
        if (damage > 0 && contact && hitCooldown <= 0f)
        {
            onPlayerDamageRequested?.Raise(damage);
            hitCooldown = kind == BossAttackKind.Puddle ? 1f : 0.6f;
        }
    }
    private void Expire(bool puddle)
    {
        if (puddle) arena.Spawn(BossAttackKind.Puddle, new Vector2(Mathf.Clamp(body.position.x, arena.Center.x - 14f, arena.Center.x + 14f), arena.Center.y - 6.6f),
            Vector2.zero, 3, 0, 2f, 5f, 0f, appearance);
        arena.ReturnHazard(this);
    }
    private void LateUpdate()
    {
        bool telling = delay > 0f || kind == BossAttackKind.Telegraph;
        sprite.sprite = telling && telegraphFrames != null && telegraphFrames.Length > 0 ?
            telegraphFrames[(int)(Time.time * 20f) % telegraphFrames.Length] : appearance;
        if (!telling && (kind == BossAttackKind.Ring || kind == BossAttackKind.SlowField || kind == BossAttackKind.GravityWell || kind == BossAttackKind.Vortex) && vortexFrames != null && vortexFrames.Length > 0)
            sprite.sprite = vortexFrames[(int)(Time.time * 20f) % vortexFrames.Length];
        if (!telling && kind == BossAttackKind.Puddle && poisonFrames != null && poisonFrames.Length > 0)
            sprite.sprite = poisonFrames[(int)(Time.time * 20f) % poisonFrames.Length];
        sprite.color = telling ? new Color(1f, 0.6f, 0.1f, 0.5f) : kind == BossAttackKind.SlowField ? new Color(0.25f, 0.6f, 1f, 0.25f) :
            kind == BossAttackKind.Puddle ? new Color(0.25f, 1f, 0.4f, 0.45f) : Color.white;
        Vector3 size = kind == BossAttackKind.SonicWave ? new Vector3(32, 0.9f, 1) :
            kind == BossAttackKind.Lightning ? new Vector3(radius * 2, 18, 1) :
            kind == BossAttackKind.Ring ? Vector3.one * Mathf.Max(0.1f, age * speed * 2f) : Vector3.one * Mathf.Max(0.2f, radius * 2f);
        size.z = 1f; transform.localScale = size;
    }
}
