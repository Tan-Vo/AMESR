using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public sealed class BossAdd : MonoBehaviour, IDamageable
{
    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private BossArenaDirector arena;
    private float angle, timer, tell, chargeRemaining, cooldown;
    private Vector2 chargeDirection;
    private bool charging;
    public BossBase Owner { get; private set; }
    public bool ShieldsBoss { get; private set; }
    public int CurrentHP { get; private set; }
    public bool IsAlive => isActiveAndEnabled && CurrentHP > 0;
    private void Awake() { body = GetComponent<Rigidbody2D>(); sprite = GetComponent<SpriteRenderer>(); }
    /// <summary>Initializes a shield jellyfish or ten-health orbiting mini-whale.</summary>
    public void Initialize(BossArenaDirector director, BossBase owner, bool shields, int health, float phase, bool charge, Sprite appearance)
    {
        arena = director; Owner = owner; ShieldsBoss = shields; CurrentHP = health; angle = phase; charging = charge;
        timer = 3f + phase; tell = chargeRemaining = cooldown = 0f;
        body.position = (Vector2)owner.transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3f;
        body.linearVelocity = Vector2.zero; sprite.sprite = appearance;
    }
    private void FixedUpdate()
    {
        if (!IsAlive || Owner == null || !Owner.IsAlive || arena.Player == null) return;
        float dt = Time.fixedDeltaTime;
        angle += dt; timer -= dt; cooldown -= dt;
        Vector2 player = arena.Player.position;
        if (timer <= 0f)
        {
            timer = 4f;
            if (charging) { tell = 0.4f; arena.Spawn(BossAttackKind.Telegraph, body.position, Vector2.zero, 0, 0, 1f, 0.4f, 0f, Owner.AttackSprite); }
            else arena.Spawn(BossAttackKind.AddBolt, body.position, (player - body.position).normalized, 8, 7f, 0.3f, 4f, 0.4f, Owner.AttackSprite);
        }
        if (tell > 0f) { tell -= dt; if (tell <= 0f) { chargeRemaining = 0.7f; chargeDirection = (player - body.position).normalized; } }
        if (chargeRemaining > 0f)
        {
            chargeRemaining -= dt; MoveTo(body.position + chargeDirection * 12f * dt);
            if ((player - body.position).sqrMagnitude < 0.8f && cooldown <= 0f) { arena.HitPlayer(10); cooldown = 0.8f; }
        }
        else MoveTo(Vector2.MoveTowards(body.position, (Vector2)Owner.transform.position + new Vector2(Mathf.Cos(angle) * 4f, Mathf.Sin(angle) * 2f), dt * 6f));
    }
    private void MoveTo(Vector2 position)
    {
        position.x = Mathf.Clamp(position.x, arena.Center.x - 15f, arena.Center.x + 15f);
        position.y = Mathf.Clamp(position.y, arena.Center.y - 6f, arena.Center.y + 7f);
        body.MovePosition(position);
    }
    /// <summary>Recycles a defeated add, immediately removing its shield contribution.</summary>
    public void TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        if (CurrentHP == 0) arena.ReturnAdd(this);
    }
}
