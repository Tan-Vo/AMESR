using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class CosmicHorizonProjectBuilder
{
    private static SO_QuestData[] StarterQuests() => Enumerable.Range(1, 10).Select(i => Ensure<SO_QuestData>("Data/SO_Quest" + i.ToString("00"))).ToArray();
    private static void BuildPolishData()
    {
        Ensure<SO_QuestProgressEvent>("Events/OnQuestProgressRequested");
        Ensure<SO_StringEvent>("Events/OnQuestFailureRequested");
        Ensure<SO_AudioCueEvent>("Events/OnAudioCueRequested");
        foreach (string name in new[] { "OnTutorialStarted", "OnJumped", "OnLanded", "OnHarpoonFired", "OnMineCompleted", "OnDishReady" }) Ensure<SO_GameEvent>("Events/" + name);
        foreach (string name in new[] { "OnBiomeChanged", "OnWaterStateChanged" }) Ensure<SO_IntEvent>("Events/" + name);
        Ensure<SO_FloatEvent>("Events/OnCameraTrauma");
        Ensure<SO_FloatEvent>("Events/OnPhaseProgressChanged");
        Ensure<SO_FloatEvent>("Events/OnBgmVolumeChanged");
        Ensure<SO_FloatEvent>("Events/OnSfxVolumeChanged");
        var gear = Ensure<SO_GearProfile>("Data/SO_GearProfile"); WireChannels(gear);
        Set(Ensure<SO_ItemData>("Data/SO_Medkit"), "itemName", "Emergency Medkit", "description", "Restores 25 HP. Use from cargo or F1–F5.",
            "category", (int)ItemCategory.Consumable, "maxStack", 5, "baseSellPrice", 25, "icon", SpriteAt("UI_Icons/gear_energy_shield_gen.png"));
        string[] titles = { "First Catch", "Deep Diver", "Chef's Kiss", "Maw Hunter", "Crystal Collector", "Night's Profit", "Gear Up", "Chrono Breaker", "Beyond the Horizon", "Void Hunter" };
        string[] descriptions = { "Catch three creatures.", "Reach 200 meters.", "Serve five customers in one night without angry departures.", "Defeat the Abyssal Maw.", "Collect ten quantum crystals.", "Earn 500 credits in one night.", "Upgrade gear to level two.", "Defeat the Chrono Jellyfish.", "Reach 2500 meters.", "Defeat the Void Leviathan." };
        ObjectiveType[] types = { ObjectiveType.CatchCreature, ObjectiveType.ReachDepth, ObjectiveType.ServeCustomers, ObjectiveType.DefeatBoss, ObjectiveType.CollectItem, ObjectiveType.EarnCredits, ObjectiveType.UpgradeGear, ObjectiveType.DefeatBoss, ObjectiveType.ReachDepth, ObjectiveType.DefeatBoss };
        string[] targets = { "", "", "NoAngry", "AbyssalMaw", "SO_QuantumCrystal", "", "", "ChronoJellyfish", "", "VoidLeviathan" };
        int[] amounts = { 3, 200, 5, 1, 10, 500, 2, 1, 2500, 1 };
        var quests = StarterQuests();
        for (int i = 0; i < quests.Length; i++)
        {
            int index = i;
            Set(quests[i], "questName", titles[i], "description", descriptions[i], "questType", (int)QuestType.MainStory, "creditReward", 50 + 25 * i, "icon", SpriteAt("UI_Icons/gear_galaxy_map_pin.png"));
            SetArray(quests[i], "objectives", 1, (p, j) => {
                p.FindPropertyRelative("type").enumValueIndex = (int)types[index];
                p.FindPropertyRelative("targetId").stringValue = targets[index];
                p.FindPropertyRelative("requiredAmount").intValue = amounts[index];
                p.FindPropertyRelative("description").stringValue = descriptions[index];
            });
        }
        for (int i = 0; i < 4; i++)
        {
            int index = i; var biome = BiomeAssets()[i];
            Color[] tints = { new Color(0.1f, 0.6f, 0.85f), new Color(0.35f, 0.15f, 0.65f), new Color(0.05f, 0.2f, 0.4f), new Color(0.2f, 0.05f, 0.3f) };
            Set(biome, "waterTintColor", tints[i], "waterTintIntensity", 0.12f + i * 0.04f, "ambientLightIntensity", 0.9f - i * 0.15f);
            SetArray(biome, "backgroundTiles", 5, (p, j) => p.objectReferenceValue = GeneratedBackground("dive_" + new[] { "shallows", "twilight", "abyssal", "eventhorizon" }[index]));
            SetArray(biome, "environmentProps", 12, (p, j) => p.objectReferenceValue = SpriteAt("Environment/env_coral_" + (index * 12 + j + 1).ToString("000") + ".png"));
        }
        foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
        {
            var upgrade = Ensure<SO_UpgradeData>("Data/SO_" + category);
            SetArray(upgrade, "bonusPerLevel", upgrade.MaxLevel, (p, i) => p.floatValue = (i + 1) *
                (category == UpgradeCategory.SuitArmor ? 2 : category == UpgradeCategory.WaiterTray || category == UpgradeCategory.CantinaSeats ? 1 : category == UpgradeCategory.KitchenSpeed ? 15 : category == UpgradeCategory.DiveDepth ? 500 : 10));
        }
    }
    private static GameObject[] BuildWorldSystemsPrefabs()
    {
        var root = new GameObject("WorldSystems");
        var audio = root.AddComponent<AudioManager>(); WireChannels(audio);
        var music = root.AddComponent<AudioSource>(); music.playOnAwake = false; music.loop = true;
        var sound = root.AddComponent<AudioSource>(); sound.playOnAwake = false;
        Set(audio, "bgmSource", music, "sfxSource", sound);
        SetArray(audio, "bgmTracks", Enum.GetValues(typeof(GameState)).Length, (p, i) => p.objectReferenceValue = null);
        SetArray(audio, "sfxClips", Enum.GetValues(typeof(SfxType)).Length, (p, i) => p.objectReferenceValue = null);
        SetArray(audio, "biomeTracks", 4, (p, i) => p.objectReferenceValue = null);
        var questRoot = new GameObject("QuestManager");
        var quests = questRoot.AddComponent<QuestManager>(); WireChannels(quests);
        var definitions = StarterQuests(); SetArray(quests, "quests", definitions.Length, (p, i) => p.objectReferenceValue = definitions[i]);
        var currencyRoot = new GameObject("CurrencyManager"); WireChannels(currencyRoot.AddComponent<CurrencyManager>());
        var tutorialRoot = new GameObject("TutorialManager");
        var tutorial = tutorialRoot.AddComponent<TutorialManager>(); WireChannels(tutorial);
        var canvas = new GameObject("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler)); canvas.transform.SetParent(tutorialRoot.transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; canvas.GetComponent<Canvas>().sortingOrder = 500;
        canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        var panel = Frame(canvas.transform, "TutorialPrompt", Vector2.zero, new Vector2(1150, 150), "ui_panel_small"); panel.raycastTarget = false;
        Set(tutorial, "panel", panel.gameObject, "instructions", ServiceText(panel.transform, "Instructions", Vector2.zero, new Vector2(1050, 110), 30));
        Anchor(panel.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -270));
        panel.gameObject.SetActive(false);
        string[] sources = { "OnJumped", "OnLanded", "OnHarpoonFired", "OnCableSnapped", "OnCreatureCaught", "OnMineCompleted", "OnPlayerHurt", "OnPlayerDied", "OnDishReady", "OnDishServed", "OnBossDefeated", "OnSplash", "OnLowOxygen", "OnJetpackOverheated" };
        SfxType[] cues = { SfxType.Jump, SfxType.Land, SfxType.HarpoonFire, SfxType.CableSnap, SfxType.CreatureCaught, SfxType.MineComplete, SfxType.TakeDamage, SfxType.Die, SfxType.DishReady, SfxType.DishServed, SfxType.BossDefeat, SfxType.Splash, SfxType.OxygenWarning, SfxType.JetpackOverheat };
        for (int i = 0; i < sources.Length; i++)
        {
            var relay = root.AddComponent<AudioEventRelay>(); WireChannels(relay);
            Set(relay, "source", Ensure<SO_GameEvent>("Events/" + sources[i]), "cue", (int)cues[i]);
        }
        return new[] {
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Core/PF_AudioManager.prefab"),
            PrefabUtility.SaveAsPrefabAsset(questRoot, "Assets/Prefabs/Core/PF_QuestManager.prefab"),
            PrefabUtility.SaveAsPrefabAsset(tutorialRoot, "Assets/Prefabs/Core/PF_TutorialManager.prefab"),
            PrefabUtility.SaveAsPrefabAsset(currencyRoot, "Assets/Prefabs/Core/PF_CurrencyManager.prefab")
        };
    }
    private static Sprite GeneratedBackground(string name) => SpriteAt("Backgrounds/Generated/bg_" + name + ".png") ?? throw new InvalidOperationException("Import the generated background bg_" + name + ".png before building.");
    private static void BuildScenePolish(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        var camera = roots.First(go => go.GetComponent<Camera>() != null).GetComponent<Camera>();
        bool dive = scene.name == "SC_Tutorial" || scene.name.StartsWith("SC_DiveZone", StringComparison.Ordinal);
        bool boss = scene.name == "SC_BossArena";
        var oldFollow = camera.GetComponent<DiveSceneView>(); if (oldFollow != null) oldFollow.enabled = false;
        var follow = camera.GetComponent<CameraFollow>() ?? camera.gameObject.AddComponent<CameraFollow>(); WireChannels(follow);
        var shake = camera.GetComponent<CameraShake>() ?? camera.gameObject.AddComponent<CameraShake>(); WireChannels(shake);
        var player = roots.FirstOrDefault(go => go.GetComponent<PlayerController>() != null);
        var arena = roots.FirstOrDefault(go => go.GetComponent<BossArenaDirector>() != null);
        Set(follow, "target", boss ? arena.transform : player != null ? player.transform : null, "offset", Vector2.zero);
        camera.backgroundColor = new Color(0.02f, 0.04f, 0.08f); camera.clearFlags = CameraClearFlags.SolidColor;
        if (boss) { camera.transform.position = new Vector3(0, 0, -10); camera.depth = 1; var listener = camera.GetComponent<AudioListener>(); if (listener != null) listener.enabled = false; }
        var existing = roots.FirstOrDefault(go => go.name == "BiomePresentation"); if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
        if (boss) foreach (var prop in arena.GetComponentsInChildren<SpriteRenderer>().Where(renderer => renderer.name.StartsWith("BiomeProp_", StringComparison.Ordinal)).ToArray()) UnityEngine.Object.DestroyImmediate(prop.gameObject);
        int biomeIndex = scene.name.Contains("Twilight") ? 1 : scene.name.Contains("Abyssal") ? 2 : scene.name.Contains("EventHorizon") ? 3 : 0;
        string image = boss ? "bossarena" : scene.name == "SC_MainMenu" ? "mainmenu" : scene.name == "SC_Mothership" ? "mothership" : scene.name == "SC_NebulaCantina" ? "cantina" :
            "dive_" + new[] { "shallows", "twilight", "abyssal", "eventhorizon" }[biomeIndex];
        var background = GeneratedBackground(image);
        var root = new GameObject("BiomePresentation"); var parallax = root.AddComponent<ParallaxController>();
        Set(parallax, "cameraTransform", camera.transform, "automaticScroll", 0f);
        SetArray(parallax, "speeds", 5, (p, i) => p.floatValue = dive ? 0.08f : 1f);
        var tiles = new SpriteRenderer[15];
        float height = camera.orthographicSize * 2f;
        float scale = Mathf.Max(height * (16f / 9f) / background.bounds.size.x, height / background.bounds.size.y) * 1.15f;
        for (int i = 0; i < tiles.Length; i++)
        {
            var obj = new GameObject("Parallax_" + i, typeof(SpriteRenderer)); obj.transform.SetParent(root.transform, false);
            var renderer = obj.GetComponent<SpriteRenderer>(); renderer.sprite = background; renderer.sortingLayerName = "BG_Skybox";
            renderer.sortingOrder = boss ? 10 : 0; renderer.enabled = i < 3;
            obj.transform.localScale = Vector3.one * scale;
            obj.transform.position = new Vector3(camera.transform.position.x + (i % 3 - 1) * background.bounds.size.x * scale, camera.transform.position.y, 10 + i / 3);
            tiles[i] = renderer;
        }
        // Runtime parallax and biome channels use a five-layer, three-copy array.
        SetArray(parallax, "tiles", tiles.Length, (p, i) => p.objectReferenceValue = tiles[i]);
        Light2D lighting = null;
        if (!boss)
        {
            lighting = roots.Where(go => go != null).Select(go => go.GetComponent<Light2D>()).FirstOrDefault(light => light != null);
            if (lighting == null) { lighting = new GameObject("GlobalLight2D", typeof(Light2D)).GetComponent<Light2D>(); lighting.lightType = Light2D.LightType.Global; }
            var sorting = SortingLayer.layers;
            SetArray(lighting, "m_ApplyToSortingLayers", sorting.Length, (p, i) => p.intValue = sorting[i].id);
        }
        if (dive)
        {
            var zone = root.AddComponent<BiomeZone>(); WireChannels(zone);
            SetArray(zone, "biomes", 4, (p, i) => p.objectReferenceValue = BiomeAssets()[i]);
            SetArray(zone, "backgroundTiles", tiles.Length, (p, i) => p.objectReferenceValue = tiles[i]);
            Set(zone, "ambient", lighting);
        }
        if (!scene.name.StartsWith("SC_DiveZone", StringComparison.Ordinal) && !boss) return;
        Vector3 origin = player != null ? player.transform.position : Vector3.zero;
        var biome = BiomeAssets()[biomeIndex];
        for (int i = 0; i < 12; i++)
        {
            var prop = new GameObject("BiomeProp_" + i, typeof(SpriteRenderer)); prop.transform.SetParent(boss ? arena.transform : root.transform, false);
            var renderer = prop.GetComponent<SpriteRenderer>(); renderer.sprite = biome.EnvironmentProps[i]; renderer.sortingLayerName = "Environment_Back";
            float propScale = 0.8f + (i * 7 % 5) * 0.13f;
            prop.transform.localScale = Vector3.one * propScale;
            prop.transform.localPosition = origin + new Vector3(-27 + i * 4.8f + (i % 2) * 0.7f, -7f + (i * 7 % 4) * 0.45f + renderer.sprite.bounds.size.y * propScale * 0.5f, 0);
            if (!boss) renderer.sharedMaterial = SpriteLitMaterial();
        }
        if (!scene.name.StartsWith("SC_DiveZone", StringComparison.Ordinal)) return;
        for (int i = 0; i < 4; i++) BuildEnvironmentHazard(root.transform, origin + new Vector3(-9 + i * 6, -8, 0), (EnvironmentHazardType)i);
    }
    private static void BuildEnvironmentHazard(Transform parent, Vector3 position, EnvironmentHazardType type)
    {
        var obj = new GameObject(type.ToString(), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(EnvironmentHazard));
        obj.transform.SetParent(parent, false); obj.transform.position = position; obj.layer = type == EnvironmentHazardType.CrushingWall ? 9 : 12;
        obj.GetComponent<BoxCollider2D>().isTrigger = type != EnvironmentHazardType.CrushingWall;
        obj.GetComponent<BoxCollider2D>().size = type == EnvironmentHazardType.CrushingWall ? new Vector2(0.8f, 4f) : new Vector2(2f, 2f);
        obj.GetComponent<SpriteRenderer>().sortingLayerName = "Environment_Front";
        var hazard = obj.GetComponent<EnvironmentHazard>(); WireChannels(hazard); Set(hazard, "hazardType", (int)type);
        string prefix = type == EnvironmentHazardType.PoisonCloud ? "hazard_toxic_spore_acid_pool_" : type == EnvironmentHazardType.LavaVent ? "hazard_volcanic_magma_geyser_" :
            type == EnvironmentHazardType.ElectricField ? "hazard_cryo_ice_frost_spike_" : "hazard_gravity_vortex_rift_";
        SetArray(hazard, "frames", 20, (p, i) => p.objectReferenceValue = SpriteAt("VFX/" + prefix + (i + 1).ToString("00") + ".png"));
        if (type != EnvironmentHazardType.CrushingWall) return;
        SetArray(hazard, "frames", 1, (p, i) => p.objectReferenceValue = SpriteAt("Environment/station_arch_010.png"));
        obj.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Sliced; obj.GetComponent<SpriteRenderer>().size = new Vector2(0.8f, 4f);
        var body = obj.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
        var fixedWall = new GameObject("CrushingBackstop", typeof(BoxCollider2D), typeof(SpriteRenderer)); fixedWall.transform.SetParent(parent, false);
        fixedWall.transform.position = position + Vector3.right * 2.4f; fixedWall.layer = 9;
        fixedWall.GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 4f);
        fixedWall.GetComponent<SpriteRenderer>().sprite = SpriteAt("Environment/station_arch_010.png");
        fixedWall.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Sliced; fixedWall.GetComponent<SpriteRenderer>().size = new Vector2(0.8f, 4f);
        fixedWall.GetComponent<SpriteRenderer>().sortingLayerName = "Environment_Front";
    }
}
