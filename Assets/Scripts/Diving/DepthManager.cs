using UnityEngine;

public sealed class DepthManager : MonoBehaviour
{
    [SerializeField] private float surfaceYPosition;
    [SerializeField] private float metersPerUnit = 2f;
    [SerializeField] private SO_FloatEvent onDepthChanged;
    [SerializeField] private SO_BiomeData[] biomes;
    private float lastDepth = -1f;
    private void Update()
    {
        float depth = GetCurrentDepth();
        if (Mathf.Abs(depth - lastDepth) < 0.1f) return;
        lastDepth = depth;
        onDepthChanged?.Raise(depth);
    }
    /// <summary>Returns nonnegative depth in meters.</summary>
    public float GetCurrentDepth() => Mathf.Max(0f, (surfaceYPosition - transform.position.y) * metersPerUnit);
    /// <summary>Returns the biome whose half-open depth interval contains the player.</summary>
    public SO_BiomeData GetCurrentBiome()
    {
        float depth = GetCurrentDepth();
        if (biomes != null)
            foreach (var biome in biomes)
                if (biome != null && depth >= biome.DepthMin && depth < biome.DepthMax) return biome;
        return null;
    }
}
