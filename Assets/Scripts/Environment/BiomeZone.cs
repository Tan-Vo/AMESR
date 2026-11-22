using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class BiomeZone : MonoBehaviour
{
    [SerializeField] private SO_FloatEvent onDepthChanged;
    [SerializeField] private SO_IntEvent onBiomeChanged;
    [SerializeField] private SO_BiomeData[] biomes;
    [SerializeField] private SpriteRenderer[] backgroundTiles;
    [SerializeField] private Light2D ambient;
    private int current = -1;
    private void OnEnable() { onDepthChanged?.RegisterListener(Depth); }
    private void OnDisable() { onDepthChanged?.UnregisterListener(Depth); }
    private void Depth(float depth)
    {
        for (int i = 0; i < biomes.Length; i++)
        {
            if (depth < biomes[i].DepthMin || depth >= biomes[i].DepthMax || current == i) continue;
            current = i;
            if (ambient != null) ambient.intensity = biomes[i].AmbientLightIntensity;
            var sprites = biomes[i].BackgroundTiles;
            if (backgroundTiles != null && sprites != null)
            {
                for (int j = 0; j < backgroundTiles.Length && j / 3 < sprites.Length; j++)
                    if (backgroundTiles[j] != null) backgroundTiles[j].sprite = sprites[j / 3];
            }
            onBiomeChanged?.Raise(i);
            break;
        }
    }
}
