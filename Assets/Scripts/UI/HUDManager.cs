using UnityEngine;
using UnityEngine.UI;

public sealed class HUDManager : MonoBehaviour
{
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_InventoryAccess inventory;
    [SerializeField] private SO_FloatEvent onHPChanged, onOxygenChanged, onFuelChanged, onDepthChanged;
    [SerializeField] private SO_WeaponHUDEvent onWeaponHUDChanged;
    [SerializeField] private SO_BiomeData[] biomes;
    [SerializeField] private string locationName;
    [SerializeField] private bool showRange = true;
    [SerializeField] private bool showDepth = true;
    [SerializeField] private Image healthFill, oxygenFill, fuelFill, weaponIcon;
    [SerializeField] private Image depthFill;
    [SerializeField] private Text healthLabel, oxygenLabel, fuelLabel, depthLabel, creditsLabel, weaponLabel, quickLabel;
    private float hp = 1, oxygen = 1, fuel = 1, depth;
    private void OnEnable()
    {
        onHPChanged?.RegisterListener(Health); onOxygenChanged?.RegisterListener(Oxygen); onFuelChanged?.RegisterListener(Fuel); onDepthChanged?.RegisterListener(Depth);
        onWeaponHUDChanged?.RegisterListener(Weapon);
    }
    private void OnDisable()
    {
        onHPChanged?.UnregisterListener(Health); onOxygenChanged?.UnregisterListener(Oxygen); onFuelChanged?.UnregisterListener(Fuel); onDepthChanged?.UnregisterListener(Depth);
        onWeaponHUDChanged?.UnregisterListener(Weapon);
    }
    private void Health(float value) { hp = value; }
    private void Oxygen(float value) { oxygen = value; }
    private void Fuel(float value) { fuel = value; }
    private void Depth(float value) { depth = value; }
    private void Weapon(SO_WeaponData data, int ammo, bool reloading)
    {
        if (data == null) return;
        weaponIcon.sprite = data.Icon;
        weaponLabel.text = data.WeaponName + "  " + (data.WeaponType == WeaponType.Harpoon || data.WeaponType == WeaponType.PlasmaBlade ? "∞" : ammo.ToString()) + (reloading ? " Reloading…" : "");
    }
    private void Update()
    {
        for (int i = 0; i < 5; i++)
            if (Input.GetKeyDown(KeyCode.F1 + i)) inventory.Use(inventory.ConsumableIndex(i));
    }
    private void LateUpdate()
    {
        weaponIcon.enabled = weaponIcon.sprite != null;
        healthFill.fillAmount = hp; oxygenFill.fillAmount = oxygen; fuelFill.fillAmount = fuel;
        oxygenFill.color = oxygen < 0.2f ? Color.Lerp(Color.red, Color.white, (Mathf.Sin(Time.unscaledTime * 8f) + 1) * 0.5f) : Color.cyan;
        healthLabel.text = "HP " + Mathf.CeilToInt(hp * 100) + "/100"; oxygenLabel.text = "O₂ " + Mathf.CeilToInt(oxygen * 100) + "%"; fuelLabel.text = "FUEL " + Mathf.CeilToInt(fuel * 100) + "%";
        string biomeName = "Surface";
        foreach (var biome in biomes) if (depth >= biome.DepthMin && depth < biome.DepthMax) { biomeName = biome.BiomeName; depthLabel.color = biome.WaterTintColor * 2f; break; }
        string location = string.IsNullOrEmpty(locationName) ? biomeName : locationName;
        if (!string.IsNullOrEmpty(locationName)) depthLabel.color = new Color(0.82f, 0.92f, 1f);
        string reading = showRange ? (showDepth ? "DEPTH " : "RANGE ") + depth.ToString("0") + " m\n" : "";
        depthLabel.text = location.ToUpperInvariant() + "\n" + reading + "DAY " + session.Data.CurrentDay;
        creditsLabel.text = "₡ " + session.Data.Credits.ToString("N0");
        quickLabel.text = "I: cargo • F1–F5: consumables • Esc: pause";
        depthFill.fillAmount = showRange ? Mathf.Clamp01(depth / 3000f) : 0f; depthFill.color = depthLabel.color;
    }
}
