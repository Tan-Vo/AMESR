using UnityEngine;

public sealed class CurrencyManager : Singleton<CurrencyManager>
{
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_IntEvent onCreditsAdded;
    [SerializeField] private SO_IntEvent onCreditsChanged;
    private void OnEnable() { onCreditsAdded?.RegisterListener(AddCredits); }
    private void OnDisable() { onCreditsAdded?.UnregisterListener(AddCredits); }
    /// <summary>Adds nonnegative credits with overflow saturation.</summary>
    public void AddCredits(int amount)
    {
        if (session == null || amount < 0) return;
        session.Data.Credits = (int)System.Math.Min(int.MaxValue, (long)session.Data.Credits + amount);
        onCreditsChanged?.Raise(session.Data.Credits);
    }
    /// <summary>Atomically spends credits when the balance covers the amount.</summary>
    public bool SpendCredits(int amount)
    {
        if (session == null || amount < 0 || session.Data.Credits < amount) return false;
        session.Data.Credits -= amount;
        onCreditsChanged?.Raise(session.Data.Credits);
        return true;
    }
    /// <summary>Returns the saved credit balance.</summary>
    public int GetCredits() => session != null ? session.Data.Credits : 0;
}
