using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerStats), typeof(PlayerController))]
public sealed class PlayerVitalsFeedback : MonoBehaviour
{
    [SerializeField] private Image waterOverlay;
    [SerializeField] private Image oxygenOverlay;
    [SerializeField] private SO_FloatEvent onSuffocationChanged;
    private PlayerStats stats;
    private PlayerController controller;
    private DepthManager depth;
    private float suffocation;
    private void Awake() { stats = GetComponent<PlayerStats>(); controller = GetComponent<PlayerController>(); depth = GetComponent<DepthManager>(); }
    private void OnEnable() { onSuffocationChanged?.RegisterListener(SetSuffocation); }
    private void OnDisable() { onSuffocationChanged?.UnregisterListener(SetSuffocation); }
    private void SetSuffocation(float value) { suffocation = value; }
    private void LateUpdate()
    {
        var biome = depth != null ? depth.GetCurrentBiome() : null;
        if (waterOverlay != null)
        {
            Color tint = biome != null ? biome.WaterTintColor : new Color(0.05f, 0.25f, 0.5f);
            tint.a = controller.IsUnderwater ? biome != null ? biome.WaterTintIntensity : 0.15f : 0f;
            waterOverlay.color = tint;
        }
        if (oxygenOverlay != null)
        {
            float warning = controller.IsUnderwater && stats.IsLowOxygen() ? 0.12f + 0.1f * Mathf.Sin(Time.unscaledTime * 7f) : 0f;
            oxygenOverlay.color = Color.Lerp(new Color(1f, 0f, 0f, warning), new Color(0f, 0f, 0f, 0.95f), suffocation);
        }
    }
}
