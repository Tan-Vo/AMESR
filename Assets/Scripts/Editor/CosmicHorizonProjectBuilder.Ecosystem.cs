using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class CosmicHorizonProjectBuilder
{
    private static readonly string[] Fauna = { "abyssal_viper_angler", "biolum_jellyfish_comb", "crystal_isopod_crustacean", "prehistoric_trilobite_shell", "void_electric_eel" };
    private static readonly string[] Fish = { "fish_001_Aurora_Jellyfish", "fish_004_Bony_Wreckfish", "fish_006_Cerebral_Crab" };
    private static SO_CreatureData[] CreatureAssets() => Fauna.Concat(Fish).Select(name => Ensure<SO_CreatureData>("Data/SO_" + name)).ToArray();
    private static void ClearUrp2DLayerCaches()
    {
        var assembly = typeof(Light2D).Assembly;
        foreach (var cache in new[] { (type: "LayerUtility", field: "s_LayerBatches"), (type: "Light2DManager", field: "s_SortingLayers") })
        {
            var field = assembly.GetType("UnityEngine.Rendering.Universal." + cache.type)?.GetField(cache.field, BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null)
            {
                Debug.LogWarning($"Cosmic Horizon: URP 2D cache {cache.type}.{cache.field} was not found; skipping it.");
                continue;
            }
            try { field.SetValue(null, null); }
            catch (Exception exception) { Debug.LogWarning($"Cosmic Horizon: could not clear URP 2D cache {cache.type}.{cache.field}: {exception.Message}"); }
        }
        Debug.LogWarning("Cosmic Horizon updated sorting layers and the render pipeline. If you still see render errors after building the vertical slice, restart Unity.");
    }
    private static void BuildEcosystemData()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
        if (pipeline == null) throw new InvalidOperationException("Missing existing UniversalRP asset.");
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        ClearUrp2DLayerCaches();
        Ensure<SO_CreatureEvent>("Events/OnCreatureReleased");
        Ensure<SO_LootSpawnEvent>("Events/OnLootSpawnRequested");
        Ensure<SO_LootPickupEvent>("Events/OnLootReleased");
        Ensure<SO_ItemCollectionEvent>("Events/OnItemCollectionRequested");
        var meat = Ensure<SO_ItemData>("Data/SO_AlienMeat");
        var crystal = Ensure<SO_ItemData>("Data/SO_QuantumCrystal");
        int[] minDepth = { 500, 0, 500, 0, 1500, 0, 0, 0 };
        int[] maxDepth = { 2500, 1500, 2500, 1500, 10000, 500, 1500, 500 };
        int[] behavior = { 3, 1, 1, 0, 3, 1, 0, 0 };
        var all = CreatureAssets();
        for (int i = 0; i < all.Length; i++)
        {
            var data = all[i];
            Set(data, "depthZoneMin", minDepth[i], "depthZoneMax", maxDepth[i], "behaviorType", behavior[i],
                "maxHP", i == 4 ? 60 : 20 + i * 3, "moveSpeed", i == 4 ? 3.5f : 2f, "mass", i == 4 ? 5f : 1f,
                "catchDifficulty", Mathf.Clamp(i + 1, 1, 10), "sellPrice", 20 + i * 15);
            if (i >= Fauna.Length)
            {
                string name = Fish[i - Fauna.Length];
                Set(data, "creatureName", name.Substring(9).Replace('_', ' '), "icon", SpriteAt("DaveTheDiver_Fish/" + name + ".png"));
                SetArray(data, "animationFrames", 1, (p, j) => p.objectReferenceValue = data.Icon);
            }
            int speciesIndex = i;
            SetArray(data, "lootTable", 2, (p, j) =>
            {
                p.FindPropertyRelative("item").objectReferenceValue = j == 0 ? meat : crystal;
                p.FindPropertyRelative("weight").intValue = j == 0 ? 70 - speciesIndex * 4 : 30 + speciesIndex * 4;
                p.FindPropertyRelative("minAmount").intValue = 1;
                p.FindPropertyRelative("maxAmount").intValue = j == 0 ? 3 : 1;
            });
        }
        var biomes = BiomeAssets();
        Color[] colors = { new Color(0.04f, 0.42f, 0.65f), new Color(0.17f, 0.08f, 0.4f), new Color(0.03f, 0.06f, 0.2f), new Color(0.08f, 0.01f, 0.12f) };
        for (int i = 0; i < biomes.Length; i++)
        {
            var biome = biomes[i];
            Set(biome, "waterTintColor", colors[i], "waterTintIntensity", 0.12f + i * 0.03f, "ambientLightIntensity", 0.8f - i * 0.18f);
            var eligible = all.Where(data => data.DepthZoneMin < biome.DepthMax && data.DepthZoneMax > biome.DepthMin).ToArray();
            SetArray(biome, "spawnableCreatures", eligible.Length, (p, j) => p.objectReferenceValue = eligible[j]);
            SetArray(biome, "spawnWeights", eligible.Length, (p, j) => p.floatValue = eligible[j].BehaviorType == CreatureBehavior.Aggressive ? 1f + i : 3f);
        }
        BuildDivingData();
        BuildCombatData();
    }
    private static GameObject BuildEcosystemPrefab()
    {
        Folder("Assets/Prefabs/Creatures");
        BuildDivingPrefabs();
        BuildCreaturePrefab<CreatureBase>("PF_Creature");
        var passive = BuildCreaturePrefab<CreatureAI_Passive>("PF_Creature_Passive");
        var aggressive = BuildCreaturePrefab<CreatureAI_Aggressive>("PF_Creature_Aggressive");
        var schooling = BuildCreaturePrefab<CreatureAI_Schooling>("PF_Creature_Schooling");
        var loot = new GameObject("LootDrop", typeof(SpriteRenderer), typeof(CircleCollider2D));
        loot.tag = "Loot";
        loot.layer = 11;
        loot.GetComponent<CircleCollider2D>().isTrigger = true;
        loot.GetComponent<CircleCollider2D>().radius = 0.3f;
        loot.GetComponent<SpriteRenderer>().sprite = Ensure<SO_ItemData>("Data/SO_AlienMeat").Icon;
        loot.GetComponent<SpriteRenderer>().sortingLayerName = "Creatures_Front";
        loot.GetComponent<SpriteRenderer>().sharedMaterial = SpriteLitMaterial();
        WireChannels(loot.AddComponent<LootPickup>());
        var lootPrefab = PrefabUtility.SaveAsPrefabAsset(loot, "Assets/Prefabs/Creatures/PF_LootDrop.prefab");
        var root = new GameObject("Ecosystem");
        var flock = root.AddComponent<FlockManager>();
        var spawner = root.AddComponent<CreatureSpawner>();
        WireChannels(spawner);
        SetArray(spawner, "biomes", 4, (p, i) => p.objectReferenceValue = BiomeAssets()[i]);
        var passivePool = Pool<CreaturePool>(root.transform, "PassivePool", passive.GetComponent<CreatureBase>());
        var aggressivePool = Pool<CreaturePool>(root.transform, "AggressivePool", aggressive.GetComponent<CreatureBase>());
        var schoolingPool = Pool<CreaturePool>(root.transform, "SchoolingPool", schooling.GetComponent<CreatureBase>());
        var lootPool = Pool<LootDropPool>(root.transform, "LootPool", lootPrefab.GetComponent<LootPickup>());
        Set(spawner, "passivePool", passivePool, "aggressivePool", aggressivePool, "schoolingPool", schoolingPool, "lootPool", lootPool, "flock", flock);
        return PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Creatures/PF_Ecosystem.prefab");
    }
    private static T Pool<T>(Transform parent, string name, Component prefab) where T : Component
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var pool = obj.AddComponent<T>();
        Set(pool, "prefab", prefab, "initialSize", 30, "poolParent", obj.transform);
        return pool;
    }
    private static GameObject BuildCreaturePrefab<T>(string name) where T : CreatureBase
    {
        var creature = new GameObject(name, typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(CircleCollider2D));
        creature.layer = 7;
        creature.tag = "Creature";
        var rb = creature.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        creature.GetComponent<CircleCollider2D>().radius = 0.3f;
        creature.GetComponent<SpriteRenderer>().sprite = SpriteAt("Creatures/fauna_biolum_jellyfish_comb_01.png");
        creature.GetComponent<SpriteRenderer>().sortingLayerName = "Creatures_Front";
        creature.GetComponent<SpriteRenderer>().sharedMaterial = SpriteLitMaterial();
        var component = creature.AddComponent<T>();
        WireChannels(component);
        Set(component, "data", Ensure<SO_CreatureData>("Data/SO_biolum_jellyfish_comb"));
        var capture = creature.AddComponent<CaptureTarget>();
        WireChannels(capture);
        Set(capture, "catalog", Ensure<SO_CatchCatalog>("Data/SO_CatchCatalog"));
        BuildDamageReceiver(creature, component);
        return PrefabUtility.SaveAsPrefabAsset(creature, "Assets/Prefabs/Creatures/" + name + ".prefab");
    }
    private static void BuildEcosystemScene(Scene scene, GameObject prefab)
    {
        EnsureScenePrefab<CreatureSpawner>(scene, prefab);
        var roots = scene.GetRootGameObjects();
        var player = roots.First(root => root.GetComponent<PlayerController>() != null);
        var ecosystem = roots.First(root => root.GetComponent<CreatureSpawner>() != null);
        var camera = roots.First(root => root.GetComponent<Camera>() != null).GetComponent<Camera>();
        Set(ecosystem.GetComponent<CreatureSpawner>(), "player", player.transform, "worldCamera", camera);
        Set(ecosystem.GetComponent<FlockManager>(), "player", player.transform);
        BuildDivingScene(scene, player.transform);
        var lighting = roots.FirstOrDefault(root => root.name == "GlobalLight2D");
        if (lighting == null) lighting = new GameObject("GlobalLight2D", typeof(Light2D));
        var light = lighting.GetComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 0.8f;
        var layers = SortingLayer.layers;
        SetArray(light, "m_ApplyToSortingLayers", layers.Length, (p, i) => p.intValue = layers[i].id);
        var database = roots.FirstOrDefault(root => root.GetComponent<CreatureDatabase>() != null);
        if (database == null) database = new GameObject("CreatureDatabase", typeof(CreatureDatabase));
        var definitions = CreatureAssets();
        SetArray(database.GetComponent<CreatureDatabase>(), "creatures", definitions.Length, (p, i) => p.objectReferenceValue = definitions[i]);
    }
    private static Material SpriteLitMaterial()
    {
        Folder("Assets/Materials");
        const string path = "Assets/Materials/M_SpriteLit.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (shader == null) throw new InvalidOperationException("URP Sprite-Lit-Default shader is missing.");
        material = new Material(shader);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
