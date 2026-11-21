using UnityEngine;

public sealed class ReputationSystem : MonoBehaviour
{
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_IntEvent onReputationRequested;
    [SerializeField] private SO_IntEvent onReputationChanged;
    public ReputationRank Rank => ServiceMath.Rank(session != null ? session.Data.ReputationPoints : 0);
    private void OnEnable() { onReputationRequested?.RegisterListener(ChangePoints); }
    private void OnDisable() { onReputationRequested?.UnregisterListener(ChangePoints); }
    /// <summary>Updates saved reputation and publishes its star rank.</summary>
    public void ChangePoints(int delta)
    {
        if (session == null) return;
        session.Data.ReputationPoints = (int)System.Math.Clamp((long)session.Data.ReputationPoints + delta, 0L, int.MaxValue);
        session.Data.ReputationStars = (int)Rank + 1;
        onReputationChanged?.Raise(session.Data.ReputationStars);
    }
}
