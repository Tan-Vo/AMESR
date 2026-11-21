using UnityEngine;

public sealed class Weapon_Blaster : WeaponBase
{
    protected override bool Shoot(Vector2 direction) => Projectiles != null && Projectiles.Fire(Data, Origin, direction);
}
