using UnityEngine;

public sealed class Weapon_PlasmaShotgun : WeaponBase
{
    protected override bool Shoot(Vector2 direction) => Projectiles != null && Projectiles.Fire(Data, Origin, direction, 5, 30f);
}
