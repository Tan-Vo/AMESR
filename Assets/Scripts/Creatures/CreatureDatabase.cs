using System.Collections.Generic;
using UnityEngine;

public sealed class CreatureDatabase : Singleton<CreatureDatabase>
{
    [SerializeField] private SO_CreatureData[] creatures;
    /// <summary>Finds a definition by its stable asset ID or displayed creature name.</summary>
    public SO_CreatureData FindByName(string name)
    {
        if (creatures != null)
            foreach (var data in creatures)
                if (data != null && (data.name == name || data.CreatureName == name)) return data;
        return null;
    }
    /// <summary>Fills a caller-owned list with creatures eligible at a depth.</summary>
    public void FindByDepth(float depth, List<SO_CreatureData> results)
    {
        results.Clear();
        if (creatures == null) return;
        foreach (var data in creatures)
            if (data != null && depth >= data.DepthZoneMin && depth < data.DepthZoneMax) results.Add(data);
    }
    /// <summary>Fills a caller-owned list with the biome's spawn definitions.</summary>
    public void FindByBiome(SO_BiomeData biome, List<SO_CreatureData> results)
    {
        results.Clear();
        if (biome == null || biome.SpawnableCreatures == null) return;
        foreach (var data in biome.SpawnableCreatures) if (data != null && !results.Contains(data)) results.Add(data);
    }
}
