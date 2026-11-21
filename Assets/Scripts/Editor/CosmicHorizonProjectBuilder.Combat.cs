using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class CosmicHorizonProjectBuilder
{
    private static readonly string[] TipNames = { "BasicHarpoon", "PlasmaHarpoon", "CryoHarpoon", "ShockHarpoon", "DarkMatterHarpoon" };
    private static void BuildCombatData()
    {
        Ensure<SO_DamageEvent>("Events/OnDamageRequested");
        Ensure<SO_FloatingTextEvent>("Events/OnFloatingTextRequested");
        string[] sprites = { "basic_gravity", "blazing_plasma", "cryo_freeze", "shock_pulse", "dark_matter" };
        for (int i = 0; i < TipNames.Length; i++)
        {
            var weapon = Ensure<SO_WeaponData>("Data/SO_" + TipNames[i]);
            Set(weapon, "weaponName", TipNames[i], "weaponType", (int)WeaponType.Harpoon, "harpoonEffect", i, "baseDamage", 10,
                "fireRate", 1f, "projectileSpeed", 18f, "maxAmmo", 0, "range", 15f, "isMelee", false, "isAutomatic", false,
                "icon", SpriteAt("Weapons/harpoon_tip_" + sprites[i] + ".png"), "projectileSprite", SpriteAt("Weapons/harpoon_tip_" + sprites[i] + ".png"));
        }
        string[] names = { "Blaster", "PlasmaShotgun", "Sniper", "PlasmaBlade" };
        string[] icons = { "primary_gun_cyber_pulse_rifle", "primary_gun_plasma_shotgun", "primary_gun_laser_sniper", "tool_laser_dagger" };
        WeaponType[] types = { WeaponType.Blaster, WeaponType.Shotgun, WeaponType.Sniper, WeaponType.PlasmaBlade };
        int[] damage = { 8, 5, 35, 12 }, ammo = { 30, 8, 5, 0 };
        float[] rates = { 3f, 1f, 0.5f, 2.5f }, ranges = { 20f, 5f, 40f, 2f };
        for (int i = 0; i < names.Length; i++)
        {
            var weapon = Ensure<SO_WeaponData>("Data/SO_" + names[i]);
            Set(weapon, "weaponName", names[i], "weaponType", (int)types[i], "baseDamage", damage[i], "maxAmmo", ammo[i],
                "fireRate", rates[i], "range", ranges[i], "reloadTime", 1.5f, "projectileSpeed", 22f,
                "isAutomatic", i == 0, "isMelee", i == 3, "icon", SpriteAt("Weapons/" + icons[i] + ".png"),
                "projectileSprite", SpriteAt("Weapons/ammo_quantum_battery_cell_01.png"));
        }
        var battery = Ensure<SO_ItemData>("Data/SO_QuantumBattery");
        Set(battery, "itemName", "Quantum Battery", "icon", SpriteAt("Weapons/ammo_quantum_battery_cell_01.png"), "category", (int)ItemCategory.Material);
    }
    private static void BuildDamageReceiver(GameObject obj, MonoBehaviour target)
    {
        var receiver = obj.AddComponent<DamageReceiver>();
        WireChannels(receiver);
        Set(receiver, "damageable", target, "hitbox", obj.GetComponent<Collider2D>());
        if (target is PlayerStats) Set(receiver, "gear", Ensure<SO_GearProfile>("Data/SO_GearProfile"));
    }
    private static ProjectilePool BuildCombatPools(GameObject player)
    {
        Folder("Assets/Prefabs/Combat");
        Folder("Assets/Prefabs/UI");
        var projectile = new GameObject("Projectile", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(Projectile));
        ConfigureProjectileBody(projectile);
        projectile.tag = "Projectile";
        projectile.layer = 8;
        projectile.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
        var sprite = projectile.GetComponent<SpriteRenderer>();
        sprite.sprite = SpriteAt("Weapons/ammo_quantum_battery_cell_01.png");
        sprite.sharedMaterial = SpriteLitMaterial();
        sprite.sortingLayerName = "Weapons_FX";
        WireChannels(projectile.GetComponent<Projectile>());
        var prefab = PrefabUtility.SaveAsPrefabAsset(projectile, "Assets/Prefabs/Combat/PF_Projectile.prefab");
        var root = new GameObject("CombatPools");
        root.transform.SetParent(player.transform, false);
        var energy = Pool<ProjectileObjectPool>(root.transform, "EnergyPool", prefab.GetComponent<Projectile>());
        var plasma = Pool<ProjectileObjectPool>(root.transform, "PlasmaPool", prefab.GetComponent<Projectile>());
        Set(energy, "initialSize", 48);
        Set(plasma, "initialSize", 48);
        var pool = root.AddComponent<ProjectilePool>();
        Set(pool, "energyPool", energy, "plasmaPool", plasma, "hitMask", (1 << 7) | (1 << 9) | (1 << 10));
        var floating = new GameObject("FloatingText", typeof(TextMesh), typeof(FloatingText));
        var text = floating.GetComponent<TextMesh>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 48;
        text.characterSize = 0.08f;
        text.anchor = TextAnchor.MiddleCenter;
        text.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        text.GetComponent<MeshRenderer>().sortingLayerName = "Overlay";
        var textPrefab = PrefabUtility.SaveAsPrefabAsset(floating, "Assets/Prefabs/UI/PF_FloatingText.prefab");
        var textPool = Pool<FloatingTextPool>(root.transform, "FloatingTextPool", textPrefab.GetComponent<FloatingText>());
        Set(textPool, "initialSize", 64);
        var presenter = root.AddComponent<FloatingTextSpawner>();
        WireChannels(presenter);
        Set(presenter, "pool", textPool);
        return pool;
    }
    private static void ConfigureProjectileBody(GameObject obj)
    {
        var body = obj.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        obj.GetComponent<CircleCollider2D>().isTrigger = true;
        obj.GetComponent<CircleCollider2D>().radius = 0.1f;
    }
    private static void BuildCombatPlayer(GameObject player, Transform canvas)
    {
        BuildDamageReceiver(player, player.GetComponent<PlayerStats>());
        var pool = BuildCombatPools(player);
        var firePoint = player.transform.Find("FirePoint");
        WeaponBase[] weapons = {
            player.AddComponent<Weapon_Harpoon>(), player.AddComponent<Weapon_Blaster>(), player.AddComponent<Weapon_PlasmaShotgun>(),
            player.AddComponent<Weapon_Sniper>(), player.AddComponent<Weapon_PlasmaBlade>()
        };
        string[] names = { "BasicHarpoon", "Blaster", "PlasmaShotgun", "Sniper", "PlasmaBlade" };
        for (int i = 0; i < weapons.Length; i++)
        {
            WireChannels(weapons[i]);
            Set(weapons[i], "data", Ensure<SO_WeaponData>("Data/SO_" + names[i]), "firePoint", firePoint, "projectilePool", pool);
        }
        Set(weapons[0], "harpoon", player.GetComponent<HarpoonSystem>());
        SetArray(weapons[0], "tips", TipNames.Length, (p, i) => p.objectReferenceValue = Ensure<SO_WeaponData>("Data/SO_" + TipNames[i]));
        Set(weapons[3], "hitMask", (1 << 7) | (1 << 9) | (1 << 10), "tracer", Cable(player.transform, "SniperTracer", Color.magenta));
        var gun = new GameObject("EquippedWeapon", typeof(SpriteRenderer));
        gun.transform.SetParent(firePoint, false);
        gun.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
        gun.GetComponent<SpriteRenderer>().sortingLayerName = "Weapons_FX";
        gun.GetComponent<SpriteRenderer>().sharedMaterial = SpriteLitMaterial();
        var label = new GameObject("WeaponStatus", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(canvas, false);
        var rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 15);
        rect.sizeDelta = new Vector2(900, 75);
        var text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        var controller = player.AddComponent<WeaponController>();
        WireChannels(controller);
        SetArray(controller, "weapons", weapons.Length, (p, i) => p.objectReferenceValue = weapons[i]);
        Set(controller, "weaponSprite", gun.GetComponent<SpriteRenderer>(), "weaponLabel", text);
    }
}
