using UnityEngine;

public sealed class ProjectilePool : MonoBehaviour
{
    [SerializeField] private ProjectileObjectPool energyPool;
    [SerializeField] private ProjectileObjectPool plasmaPool;
    [SerializeField] private LayerMask hitMask;
    /// <summary>Atomically reserves enough capacity for a single shot or full spread.</summary>
    public bool Fire(SO_WeaponData weapon, Vector2 origin, Vector2 direction, int pellets = 1, float cone = 0f)
    {
        var pool = weapon.WeaponType == WeaponType.Shotgun ? plasmaPool : energyPool;
        if (pool == null || pellets <= 0 || pool.AvailableCount < pellets) return false;
        for (int i = 0; i < pellets; i++)
        {
            float angle = pellets == 1 ? 0f : -cone * 0.5f + cone * i / (pellets - 1);
            Vector2 aim = Quaternion.Euler(0, 0, angle) * direction;
            pool.Get().Launch(pool, weapon, origin, aim, hitMask);
        }
        return true;
    }
}
