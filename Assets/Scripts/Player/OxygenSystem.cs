using UnityEngine;

[RequireComponent(typeof(PlayerStats), typeof(PlayerController), typeof(JetpackSystem))]
public sealed class OxygenSystem : MonoBehaviour
{
    [SerializeField] private float surfaceYPosition;
    [SerializeField] private float metersPerUnit = 2f;
    [SerializeField] private float gracePeriod = 5f;
    [SerializeField] private SO_GameEvent onLowOxygen;
    [SerializeField] private SO_GameEvent onEmergencySurface;
    [SerializeField] private SO_FloatEvent onSuffocationChanged;
    [SerializeField] private SO_FloatEvent onLootPenalty;
    private PlayerStats stats;
    private PlayerController controller;
    private JetpackSystem jetpack;
    private float graceTimer;
    private bool warned;
    private bool rescuing;
    private bool neededAir;
    public float Depth => Mathf.Max(0f, (surfaceYPosition - transform.position.y) * metersPerUnit);
    private void Awake() { stats = GetComponent<PlayerStats>(); controller = GetComponent<PlayerController>(); jetpack = GetComponent<JetpackSystem>(); }
    private void Update()
    {
        if (stats.IsDead()) return;
        if (!controller.RequiresAir)
        {
            if (neededAir) { stats.RefillOxygen(); onSuffocationChanged?.Raise(0f); }
            neededAir = false;
            warned = false;
            rescuing = false;
            graceTimer = 0f;
            return;
        }
        neededAir = true;
        if (rescuing) return;
        stats.ConsumeOxygen(OxygenMath.DrainRate(Depth, jetpack.IsJetpacking) * Time.deltaTime);
        if (stats.IsLowOxygen() && !warned) { warned = true; onLowOxygen?.Raise(); }
        if (!stats.IsLowOxygen()) warned = false;
        if (stats.CurrentOxygen > 0f)
        {
            if (graceTimer > 0f) onSuffocationChanged?.Raise(0f);
            graceTimer = 0f;
            return;
        }
        graceTimer += Time.deltaTime;
        onSuffocationChanged?.Raise(Mathf.Clamp01(graceTimer / Mathf.Max(0.01f, gracePeriod)));
        if (graceTimer < gracePeriod) return;
        rescuing = true;
        stats.ApplyRescuePenalty();
        onLootPenalty?.Raise(0.3f);
        if (controller.IsUnderwater)
            controller.RequestSurface(surfaceYPosition + 1f);
        else if (GameManager.Instance != null && GameManager.Instance.GetCurrentState() == GameState.Diving)
            GameManager.Instance.EndDiving();
        onEmergencySurface?.Raise();
    }
}
