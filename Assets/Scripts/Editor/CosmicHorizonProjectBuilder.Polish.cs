using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
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
        var biomes = BiomeAssets();
        for (int i = 0; i < biomes.Length; i++)
        {
            int index = i; var biome = biomes[i];
            var palette = StoryPalette.Dive(i);
            Set(biome, "waterTintColor", palette.Water, "waterTintIntensity", palette.WaterOpacity, "ambientLightIntensity", palette.Ambient);
            string[] layers = { "skybox", "distant", "atmospheric", "midground", "foreground" };
            SetArray(biome, "backgroundTiles", 5, (p, j) => p.objectReferenceValue = SpriteAt("Backgrounds/bg_parallax_" + layers[j] + "_" + (index * 4 + 1).ToString("00") + ".png"));
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
        var panel = Frame(canvas.transform, "TutorialPrompt", new Vector2(0, 310), new Vector2(1100, 110)); panel.raycastTarget = false;
        Set(tutorial, "panel", panel.gameObject, "instructions", ServiceText(panel.transform, "Instructions", Vector2.zero, new Vector2(1060, 100), 22));
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
    private static void BuildScenePolish(Scene scene)
    {
        if (!(StoryPalette.ForScene(scene.name) is StoryPalette palette)) return;
        var roots = scene.GetRootGameObjects();
        var camera = roots.First(go => go.GetComponent<Camera>() != null).GetComponent<Camera>();
        var oldFollow = camera.GetComponent<DiveSceneView>(); if (oldFollow != null) oldFollow.enabled = false;
        var follow = camera.GetComponent<CameraFollow>(); if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow>(); WireChannels(follow);
        var shake = camera.GetComponent<CameraShake>(); if (shake == null) shake = camera.gameObject.AddComponent<CameraShake>(); WireChannels(shake);
        var player = roots.FirstOrDefault(go => go.GetComponent<PlayerController>() != null);
        if (player != null) Set(follow, "target", player.transform);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = palette.Sky;
        camera.allowHDR = true;
        var cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;

        var root = roots.FirstOrDefault(go => go.name == "BiomePresentation") ?? new GameObject("BiomePresentation");
        var volume = GetOrAdd<Volume>(root);
        cameraData.volumeLayerMask |= 1 << root.layer;
        volume.sharedProfile = BuildSceneProfile(scene.name, palette);
        volume.isGlobal = true;
        volume.priority = 1f;

        string lightingName = scene.name == "SC_NebulaCantina" ? "CantinaLight" : "GlobalLight2D";
        var lighting = GetOrAdd<Light2D>(roots.FirstOrDefault(go => go.name == lightingName) ?? new GameObject(lightingName));
        lighting.lightType = Light2D.LightType.Global;
        lighting.color = Color.Lerp(Color.white, palette.Accent, palette.Biome < 0 ? .18f : .06f);
        lighting.intensity = palette.Ambient;
        TargetSortingLayers(lighting, SortingLayer.layers);

        var parallax = GetOrAdd<ParallaxController>(root);
        var zone = GetOrAdd<BiomeZone>(root);
        bool divingBackdrop = palette.Backdrop == null;
        parallax.enabled = divingBackdrop;
        zone.enabled = divingBackdrop;

        if (!divingBackdrop)
        {
            foreach (Transform child in root.transform)
                if (child.name.StartsWith("Parallax_", StringComparison.Ordinal) || child.name.StartsWith("BiomeProp_", StringComparison.Ordinal))
                    child.gameObject.SetActive(false);
            bool mothership = scene.name == "SC_Mothership";
            var backdrop = GetOrAdd<SpriteRenderer>(PresentationChild(camera.transform, "StoryBackdrop"));
            backdrop.sprite = SpriteAt("Backgrounds/" + palette.Backdrop);
            backdrop.color = palette.Scenery;
            backdrop.sortingLayerName = "BG_Skybox";
            backdrop.sharedMaterial = mothership ? SpriteLitMaterial() : StoryUnlitMaterial();
            backdrop.transform.localPosition = new Vector3(0f, 0f, 30f);
            GetOrAdd<StoryBackdrop>(backdrop.gameObject).Fit(camera);

            if (mothership)
                StoryLight(root.transform, "BridgeGlow", new Vector2(-5f, 1f), palette.Accent, .35f, 9f, "BG_Skybox");
            else if (scene.name == "SC_NebulaCantina")
            {
                StoryLight(root.transform, "CounterGlow", new Vector2(0f, 2f), palette.Accent, .38f, 6f, "Default", "Environment_Back", "Player");
                StoryLight(root.transform, "BoothGlow", new Vector2(-6f, 1.5f), new Color(1f, .74f, .39f), .24f, 5f, "Default", "Environment_Back", "Player");
            }
            return;
        }

        var biomes = BiomeAssets();
        var biome = biomes[palette.Biome];
        Set(parallax, "cameraTransform", camera.transform, "automaticScroll", 0f);
        var tiles = new SpriteRenderer[15];
        string[] layers = { "BG_Skybox", "BG_Distant", "BG_Atmospheric", "BG_Midground", "BG_Midground" };
        var unlit = StoryUnlitMaterial();
        for (int i = 0; i < tiles.Length; i++)
        {
            int layer = i / 3;
            var obj = PresentationChild(root.transform, "Parallax_" + i);
            var renderer = GetOrAdd<SpriteRenderer>(obj);
            var sprite = biome.BackgroundTiles[layer];
            renderer.sprite = sprite;
            renderer.color = palette.Scenery;
            renderer.sharedMaterial = unlit;
            renderer.sortingLayerName = layers[layer];
            renderer.sortingOrder = layer == 4 ? 1 : 0;
            obj.transform.localScale = new Vector3(50f / sprite.bounds.size.x, 22f / sprite.bounds.size.y, 1);
            tiles[i] = renderer;
        }
        SetArray(parallax, "tiles", tiles.Length, (p, i) => p.objectReferenceValue = tiles[i]);
        WireChannels(zone);
        SetArray(zone, "biomes", biomes.Length, (p, i) => p.objectReferenceValue = biomes[i]);
        SetArray(zone, "backgroundTiles", tiles.Length, (p, i) => p.objectReferenceValue = tiles[i]);
        Set(zone, "ambient", lighting);

        if (player != null)
            StoryLight(player.transform, "SuitGlow", new Vector2(0f, .3f), palette.Accent, .22f, 3.5f, "Player", "Creatures_Front", "Environment_Front");

        Vector3 origin = player != null ? player.transform.position : Vector3.zero;
        for (int i = 0; i < 12; i++)
        {
            var prop = PresentationChild(root.transform, "BiomeProp_" + i);
            prop.transform.position = origin + new Vector3(-16 + i * 3, -4 - i % 3, 0);
            var renderer = GetOrAdd<SpriteRenderer>(prop);
            renderer.sprite = biome.EnvironmentProps[i];
            renderer.color = palette.Scenery;
            renderer.sortingLayerName = "Environment_Back";
            renderer.sharedMaterial = SpriteLitMaterial();
        }
        if (!scene.name.StartsWith("SC_DiveZone", StringComparison.Ordinal)) return;
        for (int i = 0; i < 4; i++)
        {
            var type = (EnvironmentHazardType)i;
            if (root.transform.Find(type.ToString()) == null)
                BuildEnvironmentHazard(root.transform, origin + new Vector3(-9 + i * 6, -8, 0), type);
            root.transform.Find(type.ToString()).GetComponent<SpriteRenderer>().sharedMaterial = SpriteLitMaterial();
        }
    }

    private static T GetOrAdd<T>(GameObject obj) where T : Component => obj.GetComponent<T>() ?? obj.AddComponent<T>();

    private static GameObject PresentationChild(Transform parent, string name)
    {
        var obj = parent.Find(name)?.gameObject;
        if (obj == null)
        {
            obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
        }
        obj.SetActive(true);
        return obj;
    }

    private static void TargetSortingLayers(Light2D light, SortingLayer[] layers) =>
        SetArray(light, "m_ApplyToSortingLayers", layers.Length, (p, i) => p.intValue = layers[i].id);

    private static void StoryLight(Transform parent, string name, Vector2 position, Color color, float intensity, float radius, params string[] layerNames)
    {
        var light = GetOrAdd<Light2D>(PresentationChild(parent, name));
        light.transform.localPosition = position;
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * .15f;
        light.overlapOperation = Light2D.OverlapOperation.Additive;
        TargetSortingLayers(light, SortingLayer.layers.Where(layer => layerNames.Contains(layer.name)).ToArray());
    }

    private static Material StoryUnlitMaterial()
    {
        Folder("Assets/Materials");
        const string path = "Assets/Materials/M_StoryBackdrop.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) throw new InvalidOperationException("URP Sprite-Unlit-Default shader is missing.");
        material = new Material(shader);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static VolumeProfile BuildSceneProfile(string sceneName, StoryPalette palette)
    {
        var profile = Ensure<VolumeProfile>("Presentation/VP_" + sceneName);
        var vignette = ProfileOverride<Vignette>(profile);
        vignette.color.Override(palette.Sky);
        vignette.intensity.Override(palette.Vignette);
        vignette.smoothness.Override(.45f);
        var grade = ProfileOverride<ColorAdjustments>(profile);
        grade.colorFilter.Override(palette.Grade);
        grade.postExposure.Override(palette.Exposure);
        var bloom = ProfileOverride<Bloom>(profile);
        bloom.threshold.Override(.85f);
        bloom.intensity.Override(palette.Bloom);
        bloom.scatter.Override(.55f);
        EditorUtility.SetDirty(vignette);
        EditorUtility.SetDirty(grade);
        EditorUtility.SetDirty(bloom);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static T ProfileOverride<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet<T>(out var component))
        {
            component = profile.Add<T>();
            AssetDatabase.AddObjectToAsset(component, profile);
        }
        component.active = true;
        return component;
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
