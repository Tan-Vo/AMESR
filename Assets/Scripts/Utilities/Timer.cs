using System;

public sealed class Timer
{
    private float duration;
    private float remaining;
    private bool isRunning;
    private Action onComplete;
    public bool IsFinished => remaining <= 0f;
    public float Remaining => remaining;

    /// <summary>Starts a countdown, replacing any previous callback.</summary>
    public void Start(float seconds, Action callback)
    {
        duration = Math.Max(0f, seconds);
        remaining = duration;
        onComplete = callback;
        isRunning = true;
    }
    /// <summary>Advances the countdown and invokes completion exactly once.</summary>
    public void Tick(float deltaTime)
    {
        if (!isRunning) return;
        remaining = Math.Max(0f, remaining - Math.Max(0f, deltaTime));
        if (remaining > 0f) return;
        isRunning = false;
        var callback = onComplete;
        onComplete = null;
        callback?.Invoke();
    }
    /// <summary>Stops the timer without completing it.</summary>
    public void Stop() { isRunning = false; onComplete = null; }
    /// <summary>Restores the duration without starting the timer.</summary>
    public void Reset() { Stop(); remaining = duration; }
}
