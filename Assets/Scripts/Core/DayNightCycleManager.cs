using UnityEngine;

public sealed class DayNightCycleManager : MonoBehaviour
{
    [SerializeField] private SO_IntEvent onStateEntered;
    [SerializeField] private SO_FloatEvent onPhaseProgressChanged;
    [SerializeField] private SO_GameEvent onNoon;
    [SerializeField] private SO_GameEvent onDiveWarning;
    [SerializeField] private SO_GameEvent onSunset;
    [SerializeField] private SO_GameEvent onMidnight;
    private float clockSeconds = 8f * 3600f;
    private bool ticking;
    private bool service;
    public float Hour => clockSeconds / 3600f;
    private void OnEnable() { onStateEntered?.RegisterListener(StateEntered); onPhaseProgressChanged?.RegisterListener(Progress); }
    private void OnDisable() { onStateEntered?.UnregisterListener(StateEntered); onPhaseProgressChanged?.UnregisterListener(Progress); }
    private void StateEntered(int state)
    {
        var value = (GameState)state;
        if (value == GameState.Paused || value == GameState.BossFight || value == GameState.Loading) return;
        if (ticking && (value == GameState.Surfacing || value == GameState.NightShop)) Progress(1f);
        if (!ticking && value == GameState.Diving) clockSeconds = 8f * 3600f;
        if (!ticking && value == GameState.CantinaOpen) clockSeconds = 19f * 3600f;
        ticking = value == GameState.Diving || value == GameState.CantinaOpen;
        service = value == GameState.CantinaOpen;
    }
    private void Progress(float progress)
    {
        if (!ticking) return;
        float oldTime = clockSeconds;
        clockSeconds = PhaseClockMath.Hour(service ? 19f : 8f, service ? 24f : 18f, progress) * 3600f;
        Milestone(oldTime, 12, onNoon);
        Milestone(oldTime, 15, onDiveWarning);
        Milestone(oldTime, 18, onSunset);
        Milestone(oldTime, 24, onMidnight);
    }
    private void Milestone(float oldTime, int hour, SO_GameEvent channel)
    {
        float threshold = hour * 3600f;
        if (oldTime < threshold && clockSeconds >= threshold) channel?.Raise();
    }
}
