using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Economy/Gear Profile")]
public sealed class SO_GearProfile : ScriptableObject
{
    [SerializeField] private SO_SaveSession session;
    /// <summary>Returns the persisted level of a named gear category.</summary>
    public int Level(UpgradeCategory category)
    {
        if (session == null) return 0;
        session.Data.UpgradeLevels.TryGetValue("SO_" + category, out int level); return Mathf.Clamp(level, 0, 10);
    }
    public float OxygenCapacity => 100f + Level(UpgradeCategory.OxygenTank) * 10f;
    public float FuelCapacity => 100f + Level(UpgradeCategory.Jetpack) * 10f;
    public float HarpoonMultiplier => 1f + Level(UpgradeCategory.HarpoonPower) * 0.1f;
    public int Armor => Level(UpgradeCategory.SuitArmor) * 2;
    public float SafeDepth => 500f + Level(UpgradeCategory.DiveDepth) * 500f;
}
