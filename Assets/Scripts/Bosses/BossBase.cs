using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Collider2D))]
public abstract class BossBase : MonoBehaviour, IDamageable, IDamageGate
{
    [SerializeField] private SO_CreatureData bossData;
    [SerializeField] private Sprite phase2Sprite;
    [SerializeField] private Sprite attackSprite;
    [SerializeField] private SO_GameEvent onBossDefeated;
    protected Rigidbody2D Body { get; private set; }
    protected BossArenaDirector Arena { get; private set; }
    protected Vector2 Center => Arena.Center;
    protected Vector2 PlayerPosition => Arena.Player != null ? Arena.Player.position : Center;
    protected float PhaseTime { get; private set; }
    private SpriteRenderer sprite;
    private float transitionRemaining;
    private float flash;
    private bool initialized;
    public int CurrentPhase { get; private set; } = 1;
    public int CurrentHP { get; private set; }
    public int MaxHP => bossData != null ? Mathf.Max(1, bossData.MaxHP) : 1;
    public string BossName => bossData != null ? bossData.CreatureName : name;
    public bool IsAlive => initialized && CurrentHP > 0;
    public bool CanTakeDamage => IsAlive && transitionRemaining <= 0f && !Arena.HasShieldAdds(this);
    public Sprite AttackSprite => attackSprite;
    protected virtual void Awake() { Body = GetComponent<Rigidbody2D>(); sprite = GetComponent<SpriteRenderer>(); }
    /// <summary>Initializes a prewarmed boss and gives the arena a short intro window.</summary>
    public void Activate(BossArenaDirector arena)
    {
        Arena = arena; CurrentHP = MaxHP; CurrentPhase = 1; PhaseTime = 0f;
        transitionRemaining = 1.5f; initialized = true;
        Body.position = Center + new Vector2(10, 1);
        Body.linearVelocity = Vector2.zero;
        EnterPhase();
    }
    protected virtual void EnterPhase() { }
    private void FixedUpdate()
    {
        if (!IsAlive || Arena.Player == null || Arena.Finished) return;
        transitionRemaining = Mathf.Max(0f, transitionRemaining - Time.fixedDeltaTime);
        if (transitionRemaining > 0f) return;
        PhaseTime += Time.fixedDeltaTime;
        if (CurrentPhase == 1) Phase1Pattern();
        else if (CurrentPhase == 2) Phase2Pattern();
        else Phase3Pattern();
    }
    protected abstract void Phase1Pattern();
    protected abstract void Phase2Pattern();
    protected abstract void Phase3Pattern();
    /// <summary>Applies damage, transitions at exact health thresholds, and defeats once.</summary>
    public void TakeDamage(int amount)
    {
        if (!CanTakeDamage || amount <= 0) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount); flash = 0.12f;
        if (CurrentHP == 0) { OnDefeated(); return; }
        int next = BossPhaseMath.Phase(CurrentHP, MaxHP);
        if (next > CurrentPhase) TransitionPhase(next);
    }
    /// <summary>Starts a brief invulnerable phase transition.</summary>
    public void TransitionPhase(int phase)
    {
        if (!IsAlive || phase <= CurrentPhase || phase > 3) return;
        CurrentPhase = phase; PhaseTime = 0f; transitionRemaining = 1f;
        Body.linearVelocity = Vector2.zero;
        EnterPhase();
    }
    protected virtual void OnDefeated()
    {
        Body.linearVelocity = Vector2.zero;
        Arena.Defeated(this);
        onBossDefeated?.Raise();
    }
    protected void Attack(BossAttackKind kind, Vector2 position, Vector2 direction, int damage, float speed, float radius, float duration, float delay = 0f)
    { Arena.Spawn(kind, position, direction, damage, speed, radius, duration, delay, attackSprite); }
    protected void MoveTo(Vector2 position)
    {
        position.x = Mathf.Clamp(position.x, Center.x - 13f, Center.x + 13f);
        position.y = Mathf.Clamp(position.y, Center.y - 4.5f, Center.y + 5f);
        Body.MovePosition(position);
    }
    private void LateUpdate()
    {
        if (!initialized) return;
        flash = Mathf.Max(0f, flash - Time.deltaTime);
        sprite.sprite = CurrentPhase >= 2 && phase2Sprite != null ? phase2Sprite : bossData.Icon;
        sprite.color = !IsAlive ? new Color(1, 1, 1, 0.25f) : flash > 0f ? Color.white * 2f : CurrentPhase == 3 ? new Color(1f, 0.35f, 0.35f) : Color.white;
    }
}
