using UnityEngine;

public sealed class CreatureAI_Passive : CreatureBase
{
    private float direction;
    private float wanderTimer;
    private float phase;
    /// <inheritdoc />
    public override void Initialize(SO_CreatureData definition, Transform player)
    {
        base.Initialize(definition, player);
        direction = Random.value < 0.5f ? -1f : 1f;
        wanderTimer = Random.Range(2f, 5f);
        phase = Random.Range(0f, Mathf.PI * 2f);
    }
    /// <inheritdoc />
    public override void UpdateBehavior()
    {
        if (State == CreatureState.Fleeing)
        {
            StateTimer -= Time.fixedDeltaTime;
            Vector2 away = Player != null ? (Vector2)(transform.position - Player.position) : Vector2.right;
            Body.linearVelocity = away.normalized * Data.MoveSpeed * 1.7f;
            if (StateTimer <= 0f) { State = CreatureState.Wandering; wanderTimer = 2f; }
            return;
        }
        wanderTimer -= Time.fixedDeltaTime;
        if (wanderTimer <= 0f) { direction = -direction; wanderTimer = Random.Range(2f, 5f); }
        Body.linearVelocity = new Vector2(direction * Data.MoveSpeed, Mathf.Sin(Time.fixedTime * 1.7f + phase) * 0.5f);
        if (Player != null && (Player.position - transform.position).sqrMagnitude < Data.DetectionRadius * Data.DetectionRadius)
        { State = CreatureState.Fleeing; StateTimer = 3f; }
    }
}
