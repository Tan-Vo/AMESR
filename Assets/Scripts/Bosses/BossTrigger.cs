using UnityEngine;

public sealed class BossTrigger : MonoBehaviour
{
    [SerializeField] private BossKind boss;
    [SerializeField] private SO_BossEncounterEvent onBossEncounterRequested;
    [SerializeField] private SO_SaveSession session;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && session != null && !session.Data.DefeatedBossIds.Contains(boss.ToString()))
            onBossEncounterRequested?.Request(boss, other.transform);
    }
}
