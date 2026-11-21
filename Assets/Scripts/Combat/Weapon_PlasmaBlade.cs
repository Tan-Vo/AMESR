using UnityEngine;

public sealed class Weapon_PlasmaBlade : WeaponBase
{
    private int combo;
    private float lastHit = -10f;
    protected override bool Shoot(Vector2 direction)
    {
        if (Time.time - lastHit > 1f) combo = 0;
        int amount = Data.BaseDamage + (combo == 0 ? 0 : combo == 1 ? 3 : 8);
        Damage?.Melee(Origin, direction, Data.Range, amount);
        combo = (combo + 1) % 3;
        lastHit = Time.time;
        return true;
    }
}
