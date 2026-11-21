using UnityEngine;

[RequireComponent(typeof(PlayerStateMachine))]
public sealed class PlayerStats : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private int inventoryCapacity = 20;
    [SerializeField] private SO_FloatEvent onOxygenChanged;
    [SerializeField] private SO_FloatEvent onHPChanged;
    [SerializeField] private SO_FloatEvent onFuelChanged;
    [SerializeField] private SO_GameEvent onPlayerDied;
    [SerializeField] private SO_GameEvent onPlayerHurt;
    [SerializeField] private SO_IntEvent onPlayerDamageRequested;
    private PlayerStateMachine stateMachine;
    private float invulnerability;
    public int CurrentHP { get; private set; }
    public float CurrentOxygen { get; private set; }
    public float CurrentFuel { get; private set; }
    public int MaxHP => maxHP;
    public float MaxOxygen => maxOxygen;
    public int InventoryCapacity => inventoryCapacity;
    private void Awake()
    {
        stateMachine = GetComponent<PlayerStateMachine>();
        CurrentHP = Mathf.Max(1, maxHP);
        CurrentOxygen = Mathf.Max(1f, maxOxygen);
        CurrentFuel = Mathf.Max(1f, maxFuel);
    }
    private void Start() { Publish(); }
    private void OnEnable() { onPlayerDamageRequested?.RegisterListener(TakeDamage); }
    private void OnDisable() { onPlayerDamageRequested?.UnregisterListener(TakeDamage); }
    private void Update() { if (invulnerability > 0f) invulnerability -= Time.deltaTime; }
    /// <summary>Applies damage with a short hit immunity window.</summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead() || invulnerability > 0f) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        invulnerability = 0.5f;
        onHPChanged?.Raise(CurrentHP / (float)Mathf.Max(1, maxHP));
        stateMachine.ChangeState(IsDead() ? PlayerState.Dead : PlayerState.Hurt);
        if (IsDead()) onPlayerDied?.Raise(); else onPlayerHurt?.Raise();
    }
    /// <summary>Restores health without reviving a dead player.</summary>
    public void Heal(int amount)
    {
        if (IsDead() || amount <= 0) return;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        onHPChanged?.Raise(CurrentHP / (float)Mathf.Max(1, maxHP));
    }
    /// <summary>Applies the emergency rescue health penalty without killing the player.</summary>
    public void ApplyRescuePenalty()
    {
        if (IsDead()) return;
        CurrentHP = Mathf.Max(1, CurrentHP - Mathf.CeilToInt(maxHP * 0.5f));
        onHPChanged?.Raise(CurrentHP / (float)Mathf.Max(1, maxHP));
    }
    /// <summary>Consumes oxygen and publishes its normalized value.</summary>
    public void ConsumeOxygen(float amount) { CurrentOxygen = Mathf.Max(0f, CurrentOxygen - Mathf.Max(0f, amount)); onOxygenChanged?.Raise(CurrentOxygen / Mathf.Max(1f, maxOxygen)); }
    /// <summary>Refills oxygen at the surface or an oxygen station.</summary>
    public void RefillOxygen() { CurrentOxygen = maxOxygen; onOxygenChanged?.Raise(1f); }
    /// <summary>Consumes jetpack fuel.</summary>
    public void ConsumeFuel(float amount) { CurrentFuel = Mathf.Max(0f, CurrentFuel - Mathf.Max(0f, amount)); onFuelChanged?.Raise(CurrentFuel / Mathf.Max(1f, maxFuel)); }
    /// <summary>Refills jetpack fuel.</summary>
    public void RefillFuel() { CurrentFuel = maxFuel; onFuelChanged?.Raise(1f); }
    /// <summary>Returns whether health is exhausted.</summary>
    public bool IsDead() => CurrentHP <= 0;
    /// <summary>Returns whether oxygen is at or below the warning threshold.</summary>
    public bool IsLowOxygen() => CurrentOxygen <= maxOxygen * 0.2f;
    private void Publish() { onHPChanged?.Raise(1f); onOxygenChanged?.Raise(1f); onFuelChanged?.Raise(1f); }
}
