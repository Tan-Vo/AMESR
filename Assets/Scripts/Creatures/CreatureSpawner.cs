using System.Collections.Generic;
using UnityEngine;

public sealed class CreatureSpawner : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private SO_BiomeData[] biomes;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxActiveCreatures = 30;
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private CreaturePool passivePool;
    [SerializeField] private CreaturePool aggressivePool;
    [SerializeField] private CreaturePool schoolingPool;
    [SerializeField] private LootDropPool lootPool;
    [SerializeField] private FlockManager flock;
    [SerializeField] private SO_FloatEvent onDepthChanged;
    [SerializeField] private SO_CreatureEvent onCreatureReleased;
    [SerializeField] private SO_LootSpawnEvent onLootSpawnRequested;
    [SerializeField] private SO_LootPickupEvent onLootReleased;
    private readonly Dictionary<SO_CreatureData, ObjectPool<CreatureBase>> creaturePools = new();
    private CreatureBase[] active;
    private LootPickup[] drops;
    private float[] weights;
    private SO_BiomeData currentBiome;
    private float depth;
    private float timer;
    private int count;
    private int dropCount;
    public int ActiveCount => count;
    public SO_BiomeData CurrentBiome => currentBiome;
    private void Start() { if (player != null) SetDepth(-player.position.y * 2f); }
    private void Awake()
    {
        active = new CreatureBase[Mathf.Clamp(maxActiveCreatures, 1, 30)];
        drops = new LootPickup[30];
        int maxSpecies = 0;
        if (biomes != null)
            foreach (var biome in biomes)
            {
                if (biome == null || biome.SpawnableCreatures == null) continue;
                maxSpecies = Mathf.Max(maxSpecies, biome.SpawnableCreatures.Length);
                foreach (var data in biome.SpawnableCreatures)
                {
                    if (data == null) continue;
                    creaturePools[data] = data.BehaviorType == CreatureBehavior.Schooling ? schoolingPool :
                        data.BehaviorType == CreatureBehavior.Aggressive || data.BehaviorType == CreatureBehavior.Territorial || data.BehaviorType == CreatureBehavior.Boss ? aggressivePool : passivePool;
                }
            }
        weights = new float[maxSpecies];
    }
    private void OnEnable()
    {
        onDepthChanged?.RegisterListener(SetDepth);
        onCreatureReleased?.RegisterListener(Release);
        onLootSpawnRequested?.RegisterListener(SpawnLoot);
        onLootReleased?.RegisterListener(ReleaseLoot);
    }
    private void OnDisable()
    {
        onDepthChanged?.UnregisterListener(SetDepth);
        onCreatureReleased?.UnregisterListener(Release);
        onLootSpawnRequested?.UnregisterListener(SpawnLoot);
        onLootReleased?.UnregisterListener(ReleaseLoot);
    }
    private void SetDepth(float value) { depth = Mathf.Max(0f, value); }
    private void FixedUpdate()
    {
        if (player == null) return;
        currentBiome = null;
        if (biomes != null)
            foreach (var biome in biomes)
                if (biome != null && depth >= biome.DepthMin && depth < biome.DepthMax) { currentBiome = biome; break; }
        for (int i = count - 1; i >= 0; i--)
        {
            if ((active[i].transform.position - player.position).sqrMagnitude > 400f ||
                currentBiome == null || depth < active[i].Data.DepthZoneMin || depth >= active[i].Data.DepthZoneMax)
                Release(active[i]);
        }
        timer -= Time.fixedDeltaTime;
        if (timer > 0f) return;
        timer = Mathf.Max(0.1f, spawnInterval);
        Spawn();
    }
    private void Spawn()
    {
        if (currentBiome == null || currentBiome.SpawnableCreatures == null) return;
        var table = currentBiome.SpawnableCreatures;
        for (int i = 0; i < weights.Length; i++) weights[i] = 0f;
        for (int i = 0; i < table.Length; i++)
        {
            var data = table[i];
            if (data == null || depth < data.DepthZoneMin || depth >= data.DepthZoneMax ||
                currentBiome.SpawnWeights == null || i >= currentBiome.SpawnWeights.Length) continue;
            weights[i] = currentBiome.SpawnWeights[i];
        }
        int index = WeightedSelection.SelectIndex(weights, System.Math.Min(Random.value, 0.99999994f));
        if (index < 0) return;
        var definition = table[index];
        if (!creaturePools.TryGetValue(definition, out var pool) || pool == null) return;
        if (count >= active.Length) ReleaseFurthest();
        var creature = pool.Get();
        if (creature == null) return;
        float halfHeight = worldCamera != null ? worldCamera.orthographicSize : 7f;
        float horizontal = worldCamera != null ? halfHeight * worldCamera.aspect : 13f;
        float x = Mathf.Max(spawnRadius, horizontal + 2f) * (Random.value < 0.5f ? -1f : 1f);
        creature.transform.position = new Vector3(player.position.x + x, Mathf.Min(-1f, player.position.y + Random.Range(-halfHeight, halfHeight)), 0f);
        creature.Initialize(definition, player);
        if (creature is CreatureAI_Schooling school && (flock == null || !flock.Register(school)))
        { pool.Return(creature); return; }
        active[count++] = creature;
    }
    private void ReleaseFurthest()
    {
        int furthest = 0;
        float distance = -1f;
        for (int i = 0; i < count; i++)
        {
            float candidate = (active[i].transform.position - player.position).sqrMagnitude;
            if (candidate > distance) { distance = candidate; furthest = i; }
        }
        if (count > 0) Release(active[furthest]);
    }
    private void Release(CreatureBase creature)
    {
        for (int i = 0; i < count; i++)
        {
            if (active[i] != creature) continue;
            if (creature is CreatureAI_Schooling school && flock != null) flock.Unregister(school);
            active[i] = active[--count];
            active[count] = null;
            if (creaturePools.TryGetValue(creature.Data, out var pool)) pool.Return(creature);
            return;
        }
    }
    private void SpawnLoot(SO_ItemData item, int quantity, Vector3 position)
    {
        if (lootPool == null || item == null || quantity <= 0) return;
        if (dropCount == drops.Length) ReleaseLoot(drops[0]);
        var pickup = lootPool.Get();
        if (pickup == null) return;
        pickup.Initialize(item, quantity, position);
        drops[dropCount++] = pickup;
    }
    private void ReleaseLoot(LootPickup pickup)
    {
        for (int i = 0; i < dropCount; i++)
        {
            if (drops[i] != pickup) continue;
            drops[i] = drops[--dropCount];
            drops[dropCount] = null;
            lootPool.Return(pickup);
            return;
        }
    }
}
