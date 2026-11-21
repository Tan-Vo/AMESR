using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Data/WeaponData")]
public class SO_WeaponData : ScriptableObject
{
    [SerializeField] private string weaponName;
    public string WeaponName => weaponName;
    [SerializeField] private Sprite icon;
    public Sprite Icon => icon;
    [SerializeField] private Sprite projectileSprite;
    public Sprite ProjectileSprite => projectileSprite;
    [SerializeField] private WeaponType weaponType;
    public WeaponType WeaponType => weaponType;
    [SerializeField] private int baseDamage = 10;
    public int BaseDamage => baseDamage;
    [SerializeField] private float fireRate = 1f;
    public float FireRate => fireRate;
    [SerializeField] private float projectileSpeed = 15f;
    public float ProjectileSpeed => projectileSpeed;
    [SerializeField] private int maxAmmo = 30;
    public int MaxAmmo => maxAmmo;
    [SerializeField] private float reloadTime = 1.5f;
    public float ReloadTime => reloadTime;
    [SerializeField] private float range = 15f;
    public float Range => range;
    [SerializeField] private bool isAutomatic;
    public bool IsAutomatic => isAutomatic;
    [SerializeField] private bool isMelee;
    public bool IsMelee => isMelee;
    [SerializeField] private SO_WeaponData[] upgradePath;
    public SO_WeaponData[] UpgradePath => upgradePath;
    [SerializeField] private int upgradeCostBase = 100;
    public int UpgradeCostBase => upgradeCostBase;
}
