using UnityEngine;

public sealed class CreatureAI_Aggressive : CreatureBase
{
    private float cooldown;
    private Vector2 lunge;
    private bool hit;
    private float phase;
    /// <inheritdoc />
    public override void Initialize(SO_CreatureData definition, Transform player)
    {
        base.Initialize(definition, player);
        cooldown = 0f;
        hit = false;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }
    /// <inheritdoc />
    public override void UpdateBehavior()
    {
        cooldown = Mathf.Max(0f, cooldown - Time.fixedDeltaTime);
        Vector2 delta = Player != null ? (Vector2)(Player.position - transform.position) : Vector2.zero;
        if (State == CreatureState.Attacking)
        {
            StateTimer -= Time.fixedDeltaTime;
            Body.linearVelocity = lunge * Data.MoveSpeed * 3f;
            if (!hit && Player != null && delta.sqrMagnitude < 0.8f * 0.8f) { hit = true; HitPlayer(10); }
            if (StateTimer <= 0f) { State = CreatureState.Wandering; cooldown = 1.5f; }
            return;
        }
        if (Player != null && delta.sqrMagnitude < Data.DetectionRadius * Data.DetectionRadius)
        {
            State = CreatureState.Chasing;
            Body.linearVelocity = Vector2.MoveTowards(Body.linearVelocity, delta.normalized * Data.MoveSpeed, 8f * Time.fixedDeltaTime);
            if (cooldown <= 0f && delta.sqrMagnitude < 2.5f * 2.5f)
            {
                State = CreatureState.Attacking;
                StateTimer = 0.3f;
                lunge = delta.normalized;
                hit = false;
            }
        }
        else { State = CreatureState.Wandering; Body.linearVelocity = new Vector2(Mathf.Sin(Time.fixedTime * 0.5f + phase) * Data.MoveSpeed, Mathf.Cos(Time.fixedTime) * 0.3f); }
    }
}
