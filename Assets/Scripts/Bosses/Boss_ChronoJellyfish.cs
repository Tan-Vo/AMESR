using UnityEngine;

public sealed class Boss_ChronoJellyfish : BossBase
{
    private float field, whip, ring, teleport, lightning, strikeTimer;
    private int strikes, whipSide;
    protected override void EnterPhase()
    {
        field = 7f; whip = 3f; ring = 5f; teleport = CurrentPhase == 3 ? 4f : 8f; lightning = 8f;
        if (CurrentPhase == 1) Body.position = Center + Vector2.up * 3f;
        if (CurrentPhase == 3)
            for (int i = 0; i < 2; i++) Arena.SpawnAdd(this, true, Mathf.Max(1, MaxHP / 4), i * Mathf.PI, false);
    }
    protected override void Phase1Pattern() { Pattern(); }
    protected override void Phase2Pattern() { Pattern(); }
    protected override void Phase3Pattern() { Pattern(); }
    private void Pattern()
    {
        float dt = Time.fixedDeltaTime;
        field -= dt; whip -= dt;
        if (field <= 0f)
        {
            field += 7f;
            int count = CurrentPhase == 1 ? 1 : 2;
            for (int i = 0; i < count; i++)
                Attack(BossAttackKind.SlowField, PlayerPosition + new Vector2(count == 1 ? 0 : i == 0 ? -2.5f : 2.5f, 0), Vector2.zero, 0, 0, 5f, CurrentPhase == 1 ? 4f : 6f, 0.5f);
        }
        if (whip <= 0f)
        {
            whip += 3f; whipSide = 1 - whipSide;
            for (int i = 0; i < 2; i++)
            {
                Vector2 direction = (i == whipSide ? Vector2.right : Vector2.left);
                Attack(BossAttackKind.Sweep, Body.position, direction, 12, 12f, 1f, 2f, 0.6f + i * 0.25f);
            }
        }
        if (CurrentPhase >= 2)
        {
            ring -= dt; teleport -= dt;
            if (ring <= 0f) { ring += 5f; for (int i = 0; i < 4; i++) Attack(BossAttackKind.Ring, Body.position, Vector2.zero, 10, 6f, 0.4f, 5f, 0.6f + i * 0.45f); }
            if (teleport <= 0f) { teleport += CurrentPhase == 3 ? 4f : 8f; Body.position = Center + new Vector2(Random.Range(-10f, 10f), Random.Range(1f, 5f)); }
        }
        else MoveTo(Center + new Vector2(Mathf.Sin(PhaseTime * 0.6f) * 7f, 3f + Mathf.Sin(PhaseTime * 1.2f) * 2f));
        if (CurrentPhase != 3) return;
        lightning -= dt;
        if (lightning <= 0f) { lightning += 8f; strikes = 5; strikeTimer = 0f; }
        if (strikes <= 0) return;
        strikeTimer -= dt;
        if (strikeTimer <= 0f)
        {
            strikeTimer += 0.25f; strikes--;
            Attack(BossAttackKind.Lightning, new Vector2(PlayerPosition.x, Center.y), Vector2.zero, 18, 0, 0.6f, 0.18f, 1f);
        }
    }
}
