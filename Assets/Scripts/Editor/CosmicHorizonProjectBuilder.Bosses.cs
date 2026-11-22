using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class CosmicHorizonProjectBuilder
{
    private static readonly string[] BossNames = { "AbyssalMaw", "ChronoJellyfish", "VoidLeviathan" };
    private static readonly string[] BossBodies = { "Creatures/fauna_abyssal_viper_angler_01.png", "Creatures/fauna_biolum_jellyfish_comb_01.png", "Bosses/mini_boss_064.png" };
    private static readonly string[] BossPhaseBodies = { "Creatures/fauna_abyssal_viper_angler_02.png", "Creatures/fauna_biolum_jellyfish_comb_02.png", "Bosses/mini_boss_068.png" };
    private static void BuildBossData()
    {
        Ensure<SO_BossEncounterEvent>("Events/OnBossEncounterRequested");
        Ensure<SO_PlayerEffectEvent>("Events/OnPlayerEffectRequested");
        Ensure<SO_PhysicsFieldEvent>("Events/OnPhysicsFieldRequested");
        Ensure<SO_GameEvent>("Events/OnBossReturnRequested");
        var core = Ensure<SO_ItemData>("Data/SO_LegendaryBossCore");
        Set(core, "itemName", "Legendary Precursor Core", "icon", SpriteAt("Items/crystal_quantum_biolum_gem_05.png"),
            "rarity", (int)ItemRarity.Legendary, "category", (int)ItemCategory.Material, "baseSellPrice", 1000);
        string[] rares = { "ExoticMatter", "VoidEssence" };
        for (int i = 0; i < rares.Length; i++)
            Set(Ensure<SO_ItemData>("Data/SO_" + rares[i]), "itemName", rares[i], "icon", SpriteAt("Items/crystal_quantum_biolum_gem_" + (i + 2).ToString("00") + ".png"),
                "rarity", (int)ItemRarity.Epic, "category", (int)ItemCategory.Material, "baseSellPrice", 200 + i * 100);
        for (int i = 0; i < BossNames.Length; i++)
        {
            var data = Ensure<SO_CreatureData>("Data/SO_Boss_" + BossNames[i]);
            Set(data, "creatureName", BossNames[i], "maxHP", 300 + i * 100, "mass", 100f,
                "behaviorType", (int)CreatureBehavior.Boss, "icon", SpriteAt(BossBodies[i]));
            SetArray(data, "lootTable", 1, (p, j) =>
            {
                p.FindPropertyRelative("item").objectReferenceValue = core;
                p.FindPropertyRelative("weight").intValue = 100;
                p.FindPropertyRelative("minAmount").intValue = p.FindPropertyRelative("maxAmount").intValue = 1;
            });
        }
    }
    private static GameObject[] BuildBossPrefabs()
    {
        Folder("Assets/Prefabs/Bosses");
        var prefabs = new GameObject[3];
        for (int i = 0; i < prefabs.Length; i++)
        {
            var obj = new GameObject("Boss_" + BossNames[i], typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(CircleCollider2D));
            obj.tag = "Boss"; obj.layer = 7; obj.transform.localScale = Vector3.one * 3f;
            obj.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            obj.GetComponent<Rigidbody2D>().gravityScale = 0f;
            obj.GetComponent<CircleCollider2D>().isTrigger = true;
            obj.GetComponent<CircleCollider2D>().radius = 0.8f;
            var data = Ensure<SO_CreatureData>("Data/SO_Boss_" + BossNames[i]);
            obj.GetComponent<SpriteRenderer>().sprite = data.Icon;
            obj.GetComponent<SpriteRenderer>().sharedMaterial = SpriteLitMaterial();
            obj.GetComponent<SpriteRenderer>().sortingLayerName = "Creatures_Front";
            BossBase boss = i == 0 ? obj.AddComponent<Boss_AbyssalMaw>() : i == 1 ? obj.AddComponent<Boss_ChronoJellyfish>() : obj.AddComponent<Boss_VoidWhale>();
            WireChannels(boss);
            Set(boss, "bossData", data, "phase2Sprite", SpriteAt(BossPhaseBodies[i]),
                "attackSprite", SpriteAt("VFX/hazard_gravity_vortex_rift_01.png"));
            BuildDamageReceiver(obj, boss);
            prefabs[i] = PrefabUtility.SaveAsPrefabAsset(obj, "Assets/Prefabs/Bosses/PF_Boss_" + BossNames[i] + ".prefab");
        }
        var hazard = new GameObject("BossHazard", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(BossHazard));
        ConfigureProjectileBody(hazard); hazard.layer = 8;
        hazard.GetComponent<SpriteRenderer>().sortingLayerName = "Weapons_FX";
        var component = hazard.GetComponent<BossHazard>(); WireChannels(component);
        SetArray(component, "telegraphFrames", 20, (p, i) => p.objectReferenceValue = SpriteAt("Bosses/boss_apex_hazard_telegraph_" + (i + 1).ToString("00") + ".png"));
        SetArray(component, "vortexFrames", 20, (p, i) => p.objectReferenceValue = SpriteAt("VFX/hazard_gravity_vortex_rift_" + (i + 1).ToString("00") + ".png"));
        SetArray(component, "poisonFrames", 20, (p, i) => p.objectReferenceValue = SpriteAt("VFX/hazard_toxic_spore_acid_pool_" + (i + 1).ToString("00") + ".png"));
        PrefabUtility.SaveAsPrefabAsset(hazard, "Assets/Prefabs/Bosses/PF_BossProjectile.prefab");
        var add = new GameObject("BossAdd", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(BossAdd));
        add.layer = 7; add.tag = "MiniBoss";
        ConfigureProjectileBody(add);
        add.GetComponent<SpriteRenderer>().sharedMaterial = SpriteLitMaterial();
        add.GetComponent<SpriteRenderer>().sortingLayerName = "Creatures_Front";
        add.GetComponent<CircleCollider2D>().radius = 0.5f;
        BuildDamageReceiver(add, add.GetComponent<BossAdd>());
        PrefabUtility.SaveAsPrefabAsset(add, "Assets/Prefabs/Bosses/PF_BossAdd.prefab");
        return prefabs;
    }
    private static void BuildBossArena(Scene scene, GameObject[] bosses, GameObject playerPrefab)
    {
        var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.GetComponent<BossArenaDirector>() != null);
        if (existing != null) { ConfigureArenaPresentation(scene, existing, playerPrefab); return; }
        var root = new GameObject("BossArena");
        var director = root.AddComponent<BossArenaDirector>(); WireChannels(director);
        var pools = new BossPool[bosses.Length];
        for (int i = 0; i < pools.Length; i++)
        { pools[i] = Pool<BossPool>(root.transform, BossNames[i] + "Pool", bosses[i].GetComponent<BossBase>()); Set(pools[i], "initialSize", 1); }
        SetArray(director, "bossPools", pools.Length, (p, i) => p.objectReferenceValue = pools[i]);
        var hazard = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bosses/PF_BossProjectile.prefab");
        var add = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bosses/PF_BossAdd.prefab");
        var loot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Creatures/PF_LootDrop.prefab");
        var hazardPool = Pool<BossHazardPool>(root.transform, "HazardPool", hazard.GetComponent<BossHazard>()); Set(hazardPool, "initialSize", 160);
        var addPool = Pool<BossAddPool>(root.transform, "AddPool", add.GetComponent<BossAdd>()); Set(addPool, "initialSize", 8);
        var lootPool = Pool<LootDropPool>(root.transform, "BossLootPool", loot.GetComponent<LootPickup>());
        Set(director, "hazardPool", hazardPool, "addPool", addPool, "lootPool", lootPool,
            "legendaryLoot", Ensure<SO_ItemData>("Data/SO_LegendaryBossCore"),
            "jellyfishAdd", SpriteAt(BossBodies[1]), "whaleAdd", SpriteAt(BossBodies[2]));
        string[] rares = { "QuantumCrystal", "ExoticMatter", "VoidEssence" };
        SetArray(director, "rareLoot", rares.Length, (p, i) => p.objectReferenceValue = Ensure<SO_ItemData>("Data/SO_" + rares[i]));
        ArenaWall(root.transform, "Floor", new Vector2(0, -7.5f), new Vector2(36, 1));
        ArenaWall(root.transform, "Ceiling", new Vector2(0, 8.5f), new Vector2(36, 1));
        ArenaWall(root.transform, "LeftWall", new Vector2(-17.5f, 0), new Vector2(1, 18));
        ArenaWall(root.transform, "RightWall", new Vector2(17.5f, 0), new Vector2(1, 18));
        var canvas = new GameObject("BossHUD", typeof(Canvas)); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 200;
        var label = ServiceText(canvas.transform, "BossStatus", new Vector2(0, 420), new Vector2(1500, 80), 24);
        var phases = root.AddComponent<BossPhaseManager>();
        WireChannels(phases);
        Set(phases, "arena", director, "bossLabel", label);
        ConfigureArenaPresentation(scene, root, playerPrefab);
    }
    private static void ConfigureArenaPresentation(Scene scene, GameObject root, GameObject playerPrefab)
    {
        var director = root.GetComponent<BossArenaDirector>(); WireChannels(director);
        foreach (var collider in root.GetComponentsInChildren<BoxCollider2D>())
        {
            var art = collider.GetComponent<SpriteRenderer>(); if (art != null) art.enabled = false;
        }
        var player = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "ArenaPlayer");
        if (player == null) { player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene); player.name = "ArenaPlayer"; }
        player.transform.position = new Vector3(-8, 0, 0);
        foreach (string name in new[] { "WeaponStatus", "DiveControls" })
        {
            var legacy = player.transform.Find("VitalsCanvas/" + name); if (legacy != null) legacy.gameObject.SetActive(false);
        }
        var preview = root.transform.Find("EncounterPreview");
        if (preview == null) { preview = new GameObject("EncounterPreview", typeof(SpriteRenderer)).transform; preview.SetParent(root.transform, false); }
        preview.localPosition = new Vector3(10, 1, 0); preview.localScale = Vector3.one * 3f;
        var sprite = preview.GetComponent<SpriteRenderer>(); sprite.sprite = SpriteAt(BossBodies[0]); sprite.sortingLayerName = "Creatures_Front"; sprite.sharedMaterial = SpriteUnlitMaterial();
        Set(director, "standalonePlayer", player, "encounterPreview", preview.gameObject);
        var light = root.transform.Find("ArenaLight");
        if (light == null) { light = new GameObject("ArenaLight", typeof(UnityEngine.Rendering.Universal.Light2D)).transform; light.SetParent(root.transform, false); }
        var ambient = light.GetComponent<UnityEngine.Rendering.Universal.Light2D>(); ambient.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
        ambient.color = Color.white; ambient.intensity = 1;
        Set(director, "standaloneLight", ambient);
        var layers = SortingLayer.layers; SetArray(ambient, "m_ApplyToSortingLayers", layers.Length, (p, i) => p.intValue = layers[i].id);
        if (root.transform.Find("ArenaWater") == null)
        {
            var water = new GameObject("ArenaWater", typeof(BoxCollider2D)); water.transform.SetParent(root.transform, false); water.layer = 4; water.tag = "WaterSurface";
            water.GetComponent<BoxCollider2D>().isTrigger = true; water.GetComponent<BoxCollider2D>().size = new Vector2(36, 18);
        }
    }
    private static void ArenaWall(Transform root, string name, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(BoxCollider2D)); obj.layer = 9;
        obj.transform.SetParent(root, false); obj.transform.localPosition = position; obj.transform.localScale = new Vector3(size.x, size.y, 1);
    }
    private static void BuildBossTrigger(Scene scene, int biomeIndex)
    {
        if (biomeIndex < 1 || biomeIndex > 3) return;
        var existing = scene.GetRootGameObjects().FirstOrDefault(obj => obj.GetComponent<BossTrigger>() != null);
        if (existing != null) { existing.GetComponent<SpriteRenderer>().sprite = SpriteAt(BossBodies[biomeIndex - 1]); return; }
        var player = scene.GetRootGameObjects().First(obj => obj.GetComponent<PlayerController>() != null);
        var obj = new GameObject("BossEncounter_" + BossNames[biomeIndex - 1], typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(BossTrigger));
        obj.transform.position = player.transform.position + new Vector3(12, -4, 0);
        obj.layer = 12;
        obj.GetComponent<BoxCollider2D>().isTrigger = true; obj.GetComponent<BoxCollider2D>().size = new Vector2(3, 5);
        obj.GetComponent<SpriteRenderer>().sprite = SpriteAt(BossBodies[biomeIndex - 1]);
        obj.GetComponent<SpriteRenderer>().sortingLayerName = "Environment_Back";
        var trigger = obj.GetComponent<BossTrigger>(); WireChannels(trigger); Set(trigger, "boss", biomeIndex - 1);
        var loader = new GameObject("BossEncounterLoader", typeof(BossEncounterLoader)); WireChannels(loader.GetComponent<BossEncounterLoader>());
    }
}
