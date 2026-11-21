using UnityEngine;

public sealed class DayNightCycleManager : MonoBehaviour
{
    [SerializeField] private float timeScale = 60f;
    [SerializeField] private SO_IntEvent onStateEntered;
    [SerializeField] private SO_GameEvent onNoon;
    [SerializeField] private SO_GameEvent onDiveWarning;
    [SerializeField] private SO_GameEvent onSunset;
    [SerializeField] private SO_GameEvent onMidnight;
    private float clockSeconds = 8f * 3600f;
    private bool ticking;
    public float Hour => clockSeconds / 3600f;
    private void OnEnable() { onStateEntered?.RegisterListener(StateEntered); }
    private void OnDisable() { onStateEntered?.UnregisterListener(StateEntered); }
    private void StateEntered(int state)
    {
        var value = (GameState)state;
        if (value == GameState.Paused) return;
        if (!ticking && value == GameState.Diving) clockSeconds = 8f * 3600f;
        if (!ticking && value == GameState.CantinaOpen) clockSeconds = 19f * 3600f;
        ticking = value == GameState.Diving || value == GameState.CantinaOpen;
    }
    private void Update()
    {
        if (!ticking) return;
        float oldTime = clockSeconds;
        clockSeconds = Mathf.Min(24f * 3600f, clockSeconds + Time.deltaTime * Mathf.Max(0f, timeScale));
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
