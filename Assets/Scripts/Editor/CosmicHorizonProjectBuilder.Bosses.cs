using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class CosmicHorizonProjectBuilder
{
    private static readonly string[] BossNames = { "AbyssalMaw", "ChronoJellyfish", "VoidLeviathan" };
    private static void BuildBossData()
    {
        Ensure<SO_BossEncounterEvent>("Events/OnBossEncounterRequested");
        Ensure<SO_PlayerEffectEvent>("Events/OnPlayerEffectRequested");
        Ensure<SO_PhysicsFieldEvent>("Events/OnPhysicsFieldRequested");
        Ensure<SO_GameEvent>("Events/OnBossReturnRequested");
        var core = Ensure<SO_ItemData>("Data/SO_LegendaryBossCore");
        Set(core, "itemName", "Legendary Precursor Core", "icon", ArtSprite("item_core"),
            "rarity", (int)ItemRarity.Legendary, "category", (int)ItemCategory.Material, "baseSellPrice", 1000);
        string[] rares = { "ExoticMatter", "VoidEssence" };
        for (int i = 0; i < rares.Length; i++)
            Set(Ensure<SO_ItemData>("Data/SO_" + rares[i]), "itemName", rares[i], "icon", ArtSprite(i == 0 ? "item_exotic" : "item_void"),
                "rarity", (int)ItemRarity.Epic, "category", (int)ItemCategory.Material, "baseSellPrice", 200 + i * 100);
        for (int i = 0; i < BossNames.Length; i++)
        {
            var data = Ensure<SO_CreatureData>("Data/SO_Boss_" + BossNames[i]);
            Set(data, "creatureName", BossNames[i], "maxHP", 300 + i * 100, "mass", 100f,
                "behaviorType", (int)CreatureBehavior.Boss, "icon", ArtSprite("boss_" + BossArt[i]));
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
            Set(boss, "bossData", data, "phase2Sprite", ArtSprite("boss_" + BossArt[i] + "_charged"),
                "attackSprite", ArtSprite("boss_" + BossArt[i] + "_attack"));
            BuildDamageReceiver(obj, boss);
            prefabs[i] = PrefabUtility.SaveAsPrefabAsset(obj, "Assets/Prefabs/Bosses/PF_Boss_" + BossNames[i] + ".prefab");
        }
        var hazard = new GameObject("BossHazard", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(BossHazard));
        ConfigureProjectileBody(hazard); hazard.layer = 8;
        hazard.GetComponent<SpriteRenderer>().sortingLayerName = "Weapons_FX";
        var component = hazard.GetComponent<BossHazard>(); WireChannels(component);
        SetArray(component, "telegraphFrames", 4, (p, i) => p.objectReferenceValue = ArtSprite("fx_warning_" + i));
        SetArray(component, "vortexFrames", 4, (p, i) => p.objectReferenceValue = ArtSprite("fx_rift_" + i));
        SetArray(component, "poisonFrames", 4, (p, i) => p.objectReferenceValue = ArtSprite("fx_toxic_" + i));
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
    private static void BuildBossArena(Scene scene, GameObject[] bosses)
    {
        var existing = scene.GetRootGameObjects().Select(root => root.GetComponent<BossArenaDirector>()).FirstOrDefault(director => director != null);
        if (existing != null)
        {
            BuildArenaWalls(existing.transform);
            Set(existing, "jellyfishAdd", ArtSprite("life_drone"), "whaleAdd", ArtSprite("life_sentinel"));
            return;
        }
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
            "jellyfishAdd", ArtSprite("life_drone"), "whaleAdd", ArtSprite("life_sentinel"));
        string[] rares = { "QuantumCrystal", "ExoticMatter", "VoidEssence" };
        SetArray(director, "rareLoot", rares.Length, (p, i) => p.objectReferenceValue = Ensure<SO_ItemData>("Data/SO_" + rares[i]));
        BuildArenaWalls(root.transform);
        var canvas = new GameObject("BossHUD", typeof(Canvas)); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 200;
        var label = ServiceText(canvas.transform, "BossStatus", new Vector2(0, 420), new Vector2(1500, 80), 24);
        var phases = root.AddComponent<BossPhaseManager>();
        WireChannels(phases);
        Set(phases, "arena", director, "bossLabel", label);
    }
    private static void BuildArenaWalls(Transform root)
    {
        ArenaWall(root, "Floor", new Vector2(0, -7.5f), new Vector2(36, 1));
        ArenaWall(root, "Ceiling", new Vector2(0, 8.5f), new Vector2(36, 1));
        ArenaWall(root, "LeftWall", new Vector2(-17.5f, 0), new Vector2(1, 18));
        ArenaWall(root, "RightWall", new Vector2(17.5f, 0), new Vector2(1, 18));
    }
    private static void ArenaWall(Transform root, string name, Vector2 position, Vector2 size)
    {
        var obj = root.Find(name)?.gameObject;
        if (obj == null) obj = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D));
        obj.layer = 9;
        obj.transform.SetParent(root, false); obj.transform.localPosition = position;
        bool vertical = size.y > size.x;
        obj.transform.localRotation = vertical ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
        ConfigurePlatform(obj, ArtSprite("terrain_wreck"), vertical ? new Vector2(size.y, size.x) : size);
        obj.GetComponent<SpriteRenderer>().sortingLayerName = "Environment_Back";
    }
    private static void BuildBossTrigger(Scene scene, int biomeIndex)
    {
        if (biomeIndex < 1 || biomeIndex > 3) return;
        var existing = scene.GetRootGameObjects().FirstOrDefault(obj => obj.GetComponent<BossTrigger>() != null);
        if (existing != null)
        {
            existing.GetComponent<SpriteRenderer>().sprite = ArtSprite("boss_" + BossArt[biomeIndex - 1]);
            return;
        }
        var player = scene.GetRootGameObjects().First(obj => obj.GetComponent<PlayerController>() != null);
        var obj = new GameObject("BossEncounter_" + BossNames[biomeIndex - 1], typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(BossTrigger));
        obj.transform.position = player.transform.position + new Vector3(12, -4, 0);
        obj.layer = 12;
        obj.GetComponent<BoxCollider2D>().isTrigger = true; obj.GetComponent<BoxCollider2D>().size = new Vector2(3, 5);
        obj.GetComponent<SpriteRenderer>().sprite = ArtSprite("boss_" + BossArt[biomeIndex - 1]);
        obj.GetComponent<SpriteRenderer>().sortingLayerName = "Environment_Back";
        var trigger = obj.GetComponent<BossTrigger>(); WireChannels(trigger); Set(trigger, "boss", biomeIndex - 1);
        var loader = new GameObject("BossEncounterLoader", typeof(BossEncounterLoader)); WireChannels(loader.GetComponent<BossEncounterLoader>());
    }
}
