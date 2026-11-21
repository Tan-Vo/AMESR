using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] private SO_WeaponData data;
    [SerializeField] private Transform firePoint;
    [SerializeField] private ProjectilePool projectilePool;
    [SerializeField] private SO_DamageEvent onDamageRequested;
    private float cooldown;
    private float reloadRemaining;
    public virtual SO_WeaponData Data => data;
    public int CurrentAmmo { get; private set; }
    public bool IsReloading => reloadRemaining > 0f;
    protected Vector2 Origin => firePoint != null ? firePoint.position : transform.position;
    protected ProjectilePool Projectiles => projectilePool;
    protected SO_DamageEvent Damage => onDamageRequested;
    protected virtual void Awake() { CurrentAmmo = data != null ? Mathf.Max(0, data.MaxAmmo) : 0; }
    private void FixedUpdate()
    {
        cooldown = Mathf.Max(0f, cooldown - Time.fixedDeltaTime);
        if (reloadRemaining <= 0f) return;
        reloadRemaining -= Time.fixedDeltaTime;
        if (reloadRemaining <= 0f) CurrentAmmo = Mathf.Max(0, data.MaxAmmo);
    }
    /// <summary>Attempts a shot, consuming ammo only when its implementation succeeds.</summary>
    public virtual bool Fire(Vector2 direction)
    {
        if (data == null || cooldown > 0f || IsReloading || direction.sqrMagnitude < 0.001f) return false;
        bool consumesAmmo = !data.IsMelee && data.WeaponType != WeaponType.Harpoon;
        if (consumesAmmo && CurrentAmmo <= 0) { Reload(); return false; }
        if (!Shoot(direction.normalized)) return false;
        if (consumesAmmo) CurrentAmmo--;
        cooldown = 1f / Mathf.Max(0.01f, data.FireRate);
        return true;
    }
    /// <summary>Starts a timed reload without spending any reserve ammunition.</summary>
    public virtual void Reload()
    {
        if (data == null || data.IsMelee || data.WeaponType == WeaponType.Harpoon || IsReloading || CurrentAmmo >= data.MaxAmmo) return;
        reloadRemaining = Mathf.Max(0.01f, data.ReloadTime);
    }
    /// <summary>Notifies the weapon when selected or holstered.</summary>
    public virtual void SetSelected(bool selected) { }
    /// <summary>Dispatches a direct hit through the shared damage channel.</summary>
    public virtual void OnHit(Collider2D target) { if (data != null) onDamageRequested?.Hit(target, data.BaseDamage); }
    protected abstract bool Shoot(Vector2 direction);
}
