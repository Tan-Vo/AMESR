using UnityEngine;

public sealed class BossArenaDirector : MonoBehaviour
{
    [SerializeField] private SO_BossEncounterEvent onBossEncounterRequested;
    [SerializeField] private SO_PlayerEffectEvent onPlayerEffectRequested;
    [SerializeField] private SO_IntEvent onPlayerDamageRequested;
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_QuestProgressEvent onQuestProgressRequested;
    [SerializeField] private SO_GameEvent onPlayerDied;
    [SerializeField] private SO_GameEvent onEmergencySurface;
    [SerializeField] private SO_LootSpawnEvent onLootSpawnRequested;
    [SerializeField] private SO_LootPickupEvent onLootReleased;
    [SerializeField] private BossPool[] bossPools;
    [SerializeField] private BossHazardPool hazardPool;
    [SerializeField] private BossAddPool addPool;
    [SerializeField] private LootDropPool lootPool;
    [SerializeField] private SO_ItemData legendaryLoot;
    [SerializeField] private SO_ItemData[] rareLoot;
    [SerializeField] private Sprite jellyfishAdd;
    [SerializeField] private Sprite whaleAdd;
    private readonly BossHazard[] hazards = new BossHazard[160];
    private readonly BossAdd[] adds = new BossAdd[8];
    private readonly LootPickup[] drops = new LootPickup[30];
    private int hazardCount, addCount, dropCount;
    public Transform Player => onBossEncounterRequested != null ? onBossEncounterRequested.Player : null;
    public Vector2 Center => onBossEncounterRequested.Center;
    public BossBase CurrentBoss { get; private set; }
    public bool Finished { get; private set; }
    private void OnEnable()
    {
        onPlayerDied?.RegisterListener(Abort); onEmergencySurface?.RegisterListener(Abort);
        onLootSpawnRequested?.RegisterListener(SpawnLoot); onLootReleased?.RegisterListener(ReturnLoot);
    }
    private void OnDisable()
    {
        onPlayerDied?.UnregisterListener(Abort); onEmergencySurface?.UnregisterListener(Abort);
        onLootSpawnRequested?.UnregisterListener(SpawnLoot); onLootReleased?.UnregisterListener(ReturnLoot);
    }
    private void Start()
    {
        if (Player == null || !onBossEncounterRequested.InProgress) return;
        transform.position = Center;
        onPlayerEffectRequested?.Raise(PlayerEffectKind.Teleport, Center + Vector2.left * 8f);
        var pool = bossPools[(int)onBossEncounterRequested.Kind];
        CurrentBoss = pool.Get();
        if (CurrentBoss != null) CurrentBoss.Activate(this);
    }
    /// <summary>Spawns one prewarmed attack, dropping excess requests rather than allocating.</summary>
    public void Spawn(BossAttackKind kind, Vector2 position, Vector2 direction, int damage, float speed, float radius, float duration, float delay, Sprite sprite)
    {
        if (Finished || hazardCount == hazards.Length) return;
        var hazard = hazardPool.Get();
        if (hazard == null) return;
        hazard.Initialize(this, kind, position, direction, damage, speed, radius, duration, delay, sprite);
        hazards[hazardCount++] = hazard;
    }
    /// <summary>Returns a completed hazard.</summary>
    public void ReturnHazard(BossHazard hazard)
    {
        for (int i = 0; i < hazardCount; i++)
            if (hazards[i] == hazard) { hazards[i] = hazards[--hazardCount]; hazards[hazardCount] = null; hazardPool.Return(hazard); return; }
    }
    /// <summary>Creates a bounded number of damageable boss minions.</summary>
    public void SpawnAdd(BossBase owner, bool shields, int hp, float angle, bool charge)
    {
        if (addCount == adds.Length) return;
        var add = addPool.Get();
        if (add == null) return;
        add.Initialize(this, owner, shields, hp, angle, charge, shields ? jellyfishAdd : whaleAdd);
        adds[addCount++] = add;
    }
    /// <summary>Removes a killed minion from the shield/orbit registry.</summary>
    public void ReturnAdd(BossAdd add)
    {
        for (int i = 0; i < addCount; i++)
            if (adds[i] == add) { adds[i] = adds[--addCount]; adds[addCount] = null; addPool.Return(add); return; }
    }
    /// <summary>Checks whether living phase-three adds protect this boss.</summary>
    public bool HasShieldAdds(BossBase owner)
    {
        for (int i = 0; i < addCount; i++) if (adds[i].IsAlive && adds[i].ShieldsBoss && adds[i].Owner == owner) return true;
        return false;
    }
    /// <summary>Requests player damage through the existing health channel.</summary>
    public void HitPlayer(int damage) { if (!Finished) onPlayerDamageRequested?.Raise(damage); }
    /// <summary>Requests additive pull velocity without overriding the player's input.</summary>
    public void PullPlayer(Vector2 center, float speed) { if (!Finished) onPlayerEffectRequested?.Raise(PlayerEffectKind.Pull, center, speed); }
    /// <summary>Records a unique victory, clears attacks, and drops legendary plus two-to-three rare rewards.</summary>
    public void Defeated(BossBase boss)
    {
        if (Finished) return;
        string id = onBossEncounterRequested.Kind.ToString();
        bool first = !session.Data.DefeatedBossIds.Contains(id);
        if (first) session.Data.DefeatedBossIds.Add(id);
        if (first) onQuestProgressRequested?.Raise(ObjectiveType.DefeatBoss, id, 1);
        Vector3 position = boss.transform.position;
        ClearCombat();
        Spawn(BossAttackKind.Explosion, position, Vector2.zero, 0, 0, 4f, 1f, 0f, boss.AttackSprite);
        Finished = true;
        if (!first) return;
        VictoryLoot(legendaryLoot, position, id + ":legendary");
        int count = Random.Range(2, 4);
        for (int i = 0; i < count; i++) if (rareLoot.Length > 0)
            VictoryLoot(rareLoot[Random.Range(0, rareLoot.Length)], position + Vector3.right * (i - 1), id + ":rare:" + i);
    }
    private void VictoryLoot(SO_ItemData item, Vector3 position, string receipt)
    {
        if (item != null && RewardLedger.Store(session.Data, receipt, item.name, 1)) SpawnLoot(item, 1, position, receipt);
    }
    private void ClearCombat()
    {
        while (hazardCount > 0) ReturnHazard(hazards[hazardCount - 1]);
        while (addCount > 0) ReturnAdd(adds[addCount - 1]);
    }
    private void Abort() { Finished = true; ClearCombat(); }
    private void SpawnLoot(SO_ItemData item, int amount, Vector3 position)
    { SpawnLoot(item, amount, position, null); }
    private void SpawnLoot(SO_ItemData item, int amount, Vector3 position, string receipt)
    {
        if (item == null || dropCount == drops.Length) return;
        var drop = lootPool.Get();
        if (drop == null) return;
        drop.Initialize(item, amount, position, receipt); drops[dropCount++] = drop;
    }
    private void ReturnLoot(LootPickup drop)
    {
        for (int i = 0; i < dropCount; i++)
            if (drops[i] == drop) { drops[i] = drops[--dropCount]; drops[dropCount] = null; lootPool.Return(drop); return; }
    }
}
