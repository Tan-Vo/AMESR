using UnityEngine;

public sealed class Boss_VoidWhale : BossBase
{
    private float wave, slam, well, charge, stormRemaining, debrisTimer, chargeTell, chargeRemaining;
    private Vector2 chargeDirection;
    protected override void EnterPhase()
    {
        wave = 5f; slam = 8f; well = 10f; charge = 12f;
        if (CurrentPhase == 3) for (int i = 0; i < 3; i++) Arena.SpawnAdd(this, false, 10, i * Mathf.PI * 2f / 3f, true);
    }
    protected override void Phase1Pattern() { Pattern(); }
    protected override void Phase2Pattern() { Pattern(); }
    protected override void Phase3Pattern() { Pattern(); }
    private void Pattern()
    {
        float dt = Time.fixedDeltaTime;
        float rate = CurrentPhase == 3 ? 1.4f : 1f;
        wave -= dt * rate; slam -= dt * rate;
        if (wave <= 0f)
        {
            wave += 5f;
            int count = CurrentPhase == 1 ? 1 : 2;
            for (int i = 0; i < count; i++)
                Attack(BossAttackKind.SonicWave, Center + new Vector2(30, count == 1 ? Mathf.Clamp(PlayerPosition.y - Center.y, -5f, 5f) : i == 0 ? -3f : 3f),
                    Vector2.left, 20, 10f * rate, 0.45f, 6f, 0.6f);
        }
        if (slam <= 0f)
        {
            slam += 8f;
            Attack(BossAttackKind.Shockwave, new Vector2(Body.position.x, Center.y - 6f), Vector2.zero, 15, 0, 6f, 0.4f, 0.8f);
            if (CurrentPhase == 3) { stormRemaining = 6f; debrisTimer = 0f; }
        }
        if (CurrentPhase >= 2)
        {
            well -= dt * rate;
            if (well <= 0f) { well += 10f; Attack(BossAttackKind.GravityWell, Center, Vector2.zero, 25, 0, 1.2f, 4f, 0.8f); }
        }
        if (CurrentPhase == 3)
        {
            stormRemaining -= dt;
            if (stormRemaining > 0f)
            {
                debrisTimer -= dt;
                if (debrisTimer <= 0f) { debrisTimer += 0.25f; Attack(BossAttackKind.Asteroid, Center + new Vector2(Random.Range(-14f, 14f), 9f), Vector2.down, 12, 12f, 0.5f, 3f, 0.3f); }
            }
            charge -= dt;
            if (charge <= 0f) { charge += 12f; chargeTell = 1f; Attack(BossAttackKind.Telegraph, Body.position, Vector2.zero, 0, 0, 3f, 1f); }
            if (chargeTell > 0f) { chargeTell -= dt; if (chargeTell <= 0f) { chargeRemaining = 1.5f; chargeDirection = (PlayerPosition - Body.position).normalized; } }
            if (chargeRemaining > 0f)
            {
                chargeRemaining -= dt; MoveTo(Body.position + chargeDirection * 22f * dt);
                if (Vector2.Distance(Body.position, PlayerPosition) < 3f) Arena.HitPlayer(30);
                return;
            }
        }
        Vector2 target = CurrentPhase == 1 ? Center + new Vector2(Mathf.Sin(PhaseTime * 0.3f) * 8f, 3f) :
            Center + new Vector2(Mathf.Cos(PhaseTime * 0.3f) * 11f, Mathf.Sin(PhaseTime * 0.3f) * 5f);
        MoveTo(Vector2.MoveTowards(Body.position, target, dt * 3f));
    }
}
