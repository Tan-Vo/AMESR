using UnityEngine;

public enum CreatureState { Idle, Wandering, Fleeing, Chasing, Attacking, Stunned, Caught, Dead }

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Collider2D))]
public class CreatureBase : MonoBehaviour, IDamageable
{
    [SerializeField] private SO_CreatureData data;
    [SerializeField] private SO_CreatureEvent onCreatureReleased;
    [SerializeField] private SO_LootSpawnEvent onLootSpawnRequested;
    [SerializeField] private SO_GameEvent onCreatureCaught;
    [SerializeField] private SO_QuestProgressEvent onQuestProgressRequested;
    [SerializeField] private SO_IntEvent onPlayerDamageRequested;
    protected Rigidbody2D Body { get; private set; }
    protected Transform Player { get; private set; }
    protected float StateTimer { get; set; }
    public SO_CreatureData Data => data;
    public CreatureState State { get; protected set; }
    public int CurrentHP { get; private set; }
    public bool IsAlive => State != CreatureState.Dead && State != CreatureState.Caught;
    public bool IsHooked { get; private set; }
    private float slowRemaining;
    public float MotionMultiplier => slowRemaining > 0f ? 0.5f : 1f;
    /// <summary>Applies a 50% slow without changing physics outside its simulation step.</summary>
    public void ApplySlow(float seconds) { slowRemaining = Mathf.Max(slowRemaining, seconds); }
    /// <summary>Allows a capture cable to temporarily own movement.</summary>
    public void SetHooked(bool value) { IsHooked = value; }
    private SpriteRenderer sprite;
    private Collider2D hitbox;
    private float flash;
    private float age;
    private bool initialized;
    private bool stopPending;

    protected virtual void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<Collider2D>();
    }
    /// <summary>Resets a pooled creature with data and a target before its first simulation step.</summary>
    public virtual void Initialize(SO_CreatureData definition, Transform player)
    {
        data = definition;
        Player = player;
        CurrentHP = Mathf.Max(1, data.MaxHP);
        Body.mass = Mathf.Max(0.1f, data.Mass);
        Body.simulated = true;
        Body.linearVelocity = Vector2.zero;
        hitbox.enabled = true;
        State = CreatureState.Wandering;
        StateTimer = flash = age = 0f;
        initialized = true;
        IsHooked = false;
        slowRemaining = 0f;
        stopPending = false;
        sprite.color = Color.white;
        sprite.sprite = data.Icon;
    }
    private void FixedUpdate()
    {
        if (!initialized || data == null) return;
        slowRemaining = Mathf.Max(0f, slowRemaining - Time.fixedDeltaTime);
        if (stopPending)
        {
            stopPending = false;
            Body.linearVelocity = Vector2.zero;
            hitbox.enabled = false;
        }
        if (State == CreatureState.Dead || State == CreatureState.Caught)
        {
            StateTimer -= Time.fixedDeltaTime;
            if (StateTimer <= 0f) { initialized = false; onCreatureReleased?.Raise(this); }
            return;
        }
        if (State == CreatureState.Stunned)
        {
            StateTimer -= Time.fixedDeltaTime;
            Body.linearVelocity = Vector2.zero;
            if (StateTimer <= 0f) State = CreatureState.Wandering;
            return;
        }
        if (IsHooked) return;
        UpdateBehavior();
        if (Body.position.y > -0.5f)
        {
            Body.position = new Vector2(Body.position.x, -0.5f);
            Body.linearVelocity = Body.linearVelocity.WithY(Mathf.Min(0f, Body.linearVelocity.y));
        }
    }
    /// <summary>Runs physics behavior; subclasses supply specialized steering.</summary>
    public virtual void UpdateBehavior()
    {
        Body.linearVelocity = new Vector2(data.MoveSpeed, Mathf.Sin(Time.fixedTime) * 0.4f) * MotionMultiplier;
    }
    /// <summary>Applies damage, flashes the sprite, and handles lethal hits.</summary>
    public void TakeDamage(int amount)
    {
        if (!initialized || !IsAlive || amount <= 0) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        flash = 0.12f;
        if (CurrentHP == 0) Die();
    }
    /// <summary>Stops simulation, requests one weighted loot drop, and recycles after a death delay.</summary>
    public virtual void Die()
    {
        if (!initialized || !IsAlive) return;
        State = CreatureState.Dead;
        StateTimer = 1f;
        stopPending = true;
        if (LootTable.TryRoll(data.LootTable, out var drop))
        {
            int min = Mathf.Max(1, drop.MinAmount);
            int max = Mathf.Max(min, drop.MaxAmount);
            int amount = min + (int)(System.Math.Min(UnityEngine.Random.value, 0.99999994f) * ((double)max - min + 1));
            onLootSpawnRequested?.Raise(drop.Item, amount, transform.position);
        }
    }
    /// <summary>Marks a successful capture and recycles; the capture system owns inventory admission.</summary>
    public void OnCaught()
    {
        if (!initialized || !IsAlive) return;
        State = CreatureState.Caught;
        StateTimer = 0.2f;
        stopPending = true;
        onCreatureCaught?.Raise();
        onQuestProgressRequested?.Raise(ObjectiveType.CatchCreature, data.name, 1);
    }
    /// <summary>Stuns a living creature for a bounded duration.</summary>
    public void Stun(float seconds)
    {
        if (!IsAlive) return;
        State = CreatureState.Stunned;
        StateTimer = Mathf.Max(0f, seconds);
    }
    protected void HitPlayer(int damage) { onPlayerDamageRequested?.Raise(damage); }
    private void LateUpdate()
    {
        if (!initialized || data == null) return;
        age += Time.deltaTime;
        flash = Mathf.Max(0f, flash - Time.deltaTime);
        sprite.color = flash > 0f ? Color.white * 2f : State == CreatureState.Dead ? new Color(1, 1, 1, Mathf.Clamp01(StateTimer)) : Color.white;
        if (data.AnimationFrames != null && data.AnimationFrames.Length > 0)
            sprite.sprite = data.AnimationFrames[(int)(age * 10f) % data.AnimationFrames.Length];
        if (Mathf.Abs(Body.linearVelocity.x) > 0.01f) sprite.flipX = Body.linearVelocity.x < 0f;
    }
}
