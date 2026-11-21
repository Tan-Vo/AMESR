using UnityEngine;

public sealed class Boss_AbyssalMaw : BossBase
{
    private float sweep, ink, vortex, bite, biteTell, biteRemaining;
    private Vector2 biteDirection;
    protected override void EnterPhase()
    {
        sweep = CurrentPhase == 1 ? 4f : 3f; ink = 6f; vortex = 8f; bite = 10f;
        if (CurrentPhase == 1) Body.position = Center + new Vector2(10, -1);
    }
    protected override void Phase1Pattern() { Pattern(); }
    protected override void Phase2Pattern() { Pattern(); }
    protected override void Phase3Pattern() { Pattern(); }
    private void Pattern()
    {
        float dt = Time.fixedDeltaTime;
        sweep -= dt; ink -= dt;
        if (sweep <= 0f)
        {
            sweep += CurrentPhase == 1 ? 4f : 3f;
            int count = CurrentPhase == 1 ? 3 : 5;
            for (int i = 0; i < count; i++)
                Attack(BossAttackKind.Sweep, Center + new Vector2(-15, -5.8f), Vector2.right, 15, CurrentPhase == 3 ? 16f : 8f, 1.1f, 4f, 0.8f + i * 0.18f);
            if (CurrentPhase == 3) { FireInk(5); StartVortex(); }
        }
        if (ink <= 0f && CurrentPhase < 3)
        {
            ink += 6f;
            FireInk(CurrentPhase == 1 ? 3 : 5);
        }
        if (CurrentPhase >= 2)
        {
            vortex -= dt;
            if (vortex <= 0f && CurrentPhase == 2) { vortex += 8f; StartVortex(); }
        }
        if (CurrentPhase == 3)
        {
            bite -= dt;
            if (bite <= 0f)
            {
                bite += 10f; biteTell = 1.5f;
                Attack(BossAttackKind.Telegraph, Body.position, Vector2.zero, 0, 0, 3f, 1.5f);
            }
            if (biteTell > 0f)
            {
                biteTell -= dt;
                MoveTo(Body.position + Vector2.right * 3f * dt);
                if (biteTell <= 0f) { biteRemaining = 1.3f; biteDirection = (PlayerPosition - Body.position).normalized; }
                return;
            }
            if (biteRemaining > 0f)
            {
                biteRemaining -= dt;
                MoveTo(Body.position + biteDirection * 24f * dt);
                if (Vector2.Distance(Body.position, PlayerPosition) < 3f) Arena.HitPlayer(30);
                return;
            }
        }
        Vector2 target = Center + new Vector2(CurrentPhase == 1 ? 10f : 8f + Mathf.Sin(PhaseTime * 0.35f) * 3f, -1f);
        MoveTo(Vector2.MoveTowards(Body.position, target, dt * 2f));
    }
    private void FireInk(int count)
    {
        Vector2 aim = (PlayerPosition - Body.position).normalized;
        for (int i = 0; i < count; i++)
            Attack(BossAttackKind.InkShot, Body.position, Quaternion.Euler(0, 0, -25f + 50f * i / (count - 1)) * aim, 8, 8f, 0.35f, 5f);
    }
    private void StartVortex()
    {
        Attack(BossAttackKind.Vortex, Body.position, Vector2.zero, 0, CurrentPhase == 3 ? 8f : 5f, 2f, 3f, 0.5f);
    }
}
