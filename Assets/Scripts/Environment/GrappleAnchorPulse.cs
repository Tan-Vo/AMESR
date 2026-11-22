using UnityEngine;

public sealed class GrappleAnchorPulse : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer glow;
    [SerializeField] private float bobHeight = 0.06f;
    [SerializeField] private float frequency = 0.8f;
    [SerializeField] private Color glowColor = new Color(0.1f, 1f, 1f, 0.3f);
    private Vector3 origin;
    private void OnEnable() { if (visual != null) origin = visual.localPosition; }
    private void OnDisable() { if (visual != null) visual.localPosition = origin; }
    private void LateUpdate()
    {
        if (visual == null || glow == null) return;
        float wave = PresentationRules.AnchorWave(Time.time, frequency, transform.position.x * 0.35f);
        visual.localPosition = origin + Vector3.up * (wave * bobHeight);
        Color color = glowColor; color.a *= 0.7f + wave * 0.3f;
        glow.color = color;
    }
}
