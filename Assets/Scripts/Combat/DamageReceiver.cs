using UnityEngine;

public sealed class DamageReceiver : MonoBehaviour
{
    [SerializeField] private SO_GearProfile gear;
    [SerializeField] private MonoBehaviour damageable;
    [SerializeField] private Collider2D hitbox;
    [SerializeField] private int armor;
    [SerializeField] private SO_DamageEvent onDamageRequested;
    [SerializeField] private SO_FloatingTextEvent onFloatingTextRequested;
    private IDamageable target;
    private CreatureBase creature;
    private float burnTimer;
    private int burnTicks;
    public Collider2D Hitbox => hitbox;
    public bool IsAlive => isActiveAndEnabled && target != null && target.IsAlive;
    public bool IsPlayer => damageable is PlayerStats;
    private void Awake() { target = damageable as IDamageable; creature = damageable as CreatureBase; }
    private void OnEnable() { burnTicks = 0; onDamageRequested?.Register(this); }
    private void OnDisable() { burnTicks = 0; onDamageRequested?.Unregister(this); }
    /// <summary>Resolves armor and applies optional elemental status effects to this entity.</summary>
    public void Receive(int damage, float multiplier, HarpoonEffect effect)
    {
        if (!IsAlive || target is IDamageGate gate && !gate.CanTakeDamage) return;
        int bonus = effect == HarpoonEffect.BlazingPlasma ? 5 :
            effect == HarpoonEffect.DarkMatter && (CompareTag("Boss") || creature != null && creature.Data.BehaviorType == CreatureBehavior.Boss) ? 10 : 0;
        int resolved = DamageSystem.CalculateDamage((int)System.Math.Min(int.MaxValue, (long)damage + bonus), multiplier, effect == HarpoonEffect.DarkMatter ? 0 : armor + (IsPlayer && gear != null ? gear.Armor : 0));
        target.TakeDamage(resolved);
        onFloatingTextRequested?.Raise(transform.position + Vector3.up * 0.6f, resolved.ToString(), Color.yellow);
        if (creature == null || !creature.IsAlive) return;
        if (effect == HarpoonEffect.CryoFreeze) creature.ApplySlow(2f);
        if (effect == HarpoonEffect.BlazingPlasma && Random.value < 0.35f) { burnTicks = 3; burnTimer = 1f; }
    }
    /// <summary>Applies a stun to a creature endpoint.</summary>
    public void Stun(float duration) { if (IsAlive && creature != null) creature.Stun(duration); }
    private void FixedUpdate()
    {
        if (burnTicks <= 0 || !IsAlive) return;
        burnTimer -= Time.fixedDeltaTime;
        if (burnTimer > 0f) return;
        burnTimer += 1f;
        burnTicks--;
        target.TakeDamage(2);
        onFloatingTextRequested?.Raise(transform.position, "2", new Color(1f, 0.4f, 0.1f));
    }
}
