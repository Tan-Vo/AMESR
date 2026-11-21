using UnityEngine;

public sealed class Weapon_Sniper : WeaponBase
{
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private LineRenderer tracer;
    private Vector2 start;
    private Vector2 end;
    private float visibleUntil;
    protected override bool Shoot(Vector2 direction)
    {
        var hit = Physics2D.Raycast(Origin, direction, Data.Range, hitMask);
        if (hit.collider != null) OnHit(hit.collider);
        start = Origin;
        end = hit.collider != null ? hit.point : Origin + direction * Data.Range;
        visibleUntil = Time.time + 0.08f;
        return true;
    }
    private void LateUpdate()
    {
        if (tracer == null) return;
        tracer.enabled = Time.time < visibleUntil;
        if (tracer.enabled) { tracer.SetPosition(0, start); tracer.SetPosition(1, end); }
    }
}
