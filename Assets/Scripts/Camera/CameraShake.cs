using UnityEngine;

[DefaultExecutionOrder(600)]
public sealed class CameraShake : MonoBehaviour
{
    [SerializeField] private SO_FloatEvent onCameraTrauma;
    [SerializeField] private SO_GameEvent onPlayerHurt;
    [SerializeField] private SO_GameEvent onBossDefeated;
    [SerializeField] private float decay = 1.2f;
    [SerializeField] private float maxOffset = 0.6f;
    [SerializeField] private float maxAngle = 2f;
    [SerializeField] private float frequency = 25f;
    private float trauma;
    private void OnEnable() { onCameraTrauma?.RegisterListener(AddTrauma); onPlayerHurt?.RegisterListener(Hit); onBossDefeated?.RegisterListener(Explosion); }
    private void OnDisable() { onCameraTrauma?.UnregisterListener(AddTrauma); onPlayerHurt?.UnregisterListener(Hit); onBossDefeated?.UnregisterListener(Explosion); }
    private void Hit() { AddTrauma(0.3f); }
    private void Explosion() { AddTrauma(1f); }
    /// <summary>Adds a bounded trauma impulse.</summary>
    public void AddTrauma(float amount) { if (float.IsFinite(amount)) trauma = Mathf.Clamp01(trauma + amount); }
    private void LateUpdate()
    {
        trauma = TraumaMath.Decay(trauma, Time.deltaTime, decay);
        float amplitude = TraumaMath.Amplitude(trauma);
        float t = Time.time * frequency;
        transform.position += new Vector3(Mathf.PerlinNoise(17, t) * 2 - 1, Mathf.PerlinNoise(43, t) * 2 - 1) * maxOffset * amplitude;
        transform.rotation = Quaternion.Euler(0, 0, (Mathf.PerlinNoise(71, t) * 2 - 1) * maxAngle * amplitude);
    }
}
