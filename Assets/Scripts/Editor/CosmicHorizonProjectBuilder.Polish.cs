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
            "category", (int)ItemCategory.Consumable, "maxStack", 5, "baseSellPrice", 25, "icon", ArtSprite("item_medkit"));
        string[] titles = { "First Catch", "Deep Diver", "Chef's Kiss", "Maw Hunter", "Crystal Collector", "Night's Profit", "Gear Up", "Chrono Breaker", "Beyond the Horizon", "Void Hunter" };
        string[] descriptions = { "Catch three creatures.", "Reach 200 meters.", "Serve five customers in one night without angry departures.", "Defeat the Abyssal Maw.", "Collect ten quantum crystals.", "Earn 500 credits in one night.", "Upgrade gear to level two.", "Defeat the Chrono Jellyfish.", "Reach 2500 meters.", "Defeat the Void Leviathan." };
        ObjectiveType[] types = { ObjectiveType.CatchCreature, ObjectiveType.ReachDepth, ObjectiveType.ServeCustomers, ObjectiveType.DefeatBoss, ObjectiveType.CollectItem, ObjectiveType.EarnCredits, ObjectiveType.UpgradeGear, ObjectiveType.DefeatBoss, ObjectiveType.ReachDepth, ObjectiveType.DefeatBoss };
        string[] targets = { "", "", "NoAngry", "AbyssalMaw", "SO_QuantumCrystal", "", "", "ChronoJellyfish", "", "VoidLeviathan" };
        int[] amounts = { 3, 200, 5, 1, 10, 500, 2, 1, 2500, 1 };
        var quests = StarterQuests();
        for (int i = 0; i < quests.Length; i++)
        {
            int index = i;
            Set(quests[i], "questName", titles[i], "description", descriptions[i], "questType", (int)QuestType.MainStory, "creditReward", 50 + 25 * i, "icon", ArtSprite("item_exotic"));
            SetArray(quests[i], "objectives", 1, (p, j) => {
                p.FindPropertyRelative("type").enumValueIndex = (int)types[index];
                p.FindPropertyRelative("targetId").stringValue = targets[index];
                p.FindPropertyRelative("requiredAmount").intValue = amounts[index];
                p.FindPropertyRelative("description").stringValue = descriptions[index];
            });
        }
        for (int i = 0; i < 4; i++)
        {
            var biome = BiomeAssets()[i];
            var palette = StoryPalette.Dive(i);
            Set(biome, "waterTintColor", palette.Water, "waterTintIntensity", palette.WaterOpacity, "ambientLightIntensity", palette.Ambient);
            var backdrop = BackdropSprite(palette.Backdrop);
            SetArray(biome, "backgroundTiles", 5, (p, j) => p.objectReferenceValue = backdrop);
            SetArray(biome, "environmentProps", 1, (p, j) => p.objectReferenceValue = ArtSprite("terrain_" + DestinationProps[i]));
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
        var roots = scene.GetRootGameObjects();
        if (scene.name == "SC_BossArena")
        {
            var arena = roots.First(go => go.GetComponent<BossArenaDirector>() != null);
            BuildBackdrop(arena.transform, null, "arena", order: 10);
            return;
        }
        if (!(StoryPalette.ForScene(scene.name) is StoryPalette palette)) return;
        var camera = roots.First(go => go.GetComponent<Camera>() != null).GetComponent<Camera>();
        var legacyBackdrop = camera.transform.Find("StoryBackdrop");
        if (legacyBackdrop != null) UnityEngine.Object.DestroyImmediate(legacyBackdrop.gameObject);
        var oldFollow = camera.GetComponent<DiveSceneView>(); if (oldFollow != null) oldFollow.enabled = false;
        var follow = camera.GetComponent<CameraFollow>(); if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow>(); WireChannels(follow);
        var shake = camera.GetComponent<CameraShake>(); if (shake == null) shake = camera.gameObject.AddComponent<CameraShake>(); WireChannels(shake);
        var player = roots.FirstOrDefault(go => go.GetComponent<PlayerController>() != null);
        if (player != null) Set(follow, "target", player.transform);
        int destination = DestinationIndex(scene.name);
        int biomeIndex = palette.Biome;
        bool expedition = biomeIndex >= 0;
        Set(follow, "explorationZoom", expedition ? 5.9f : 7f, "swimmingZoom", 5.4f,
            "offset", new Vector2(0f, biomeIndex > 0 ? 0.35f : 1f));
        var root = roots.FirstOrDefault(go => go.name == "BiomePresentation");
        if (root == null) root = new GameObject("BiomePresentation");
        var parallax = root.GetComponent<ParallaxController>();
        if (parallax != null)
        {
            DestroyGeneratedChildren(root.transform, "Parallax_");
            DestroyGeneratedChildren(root.transform, "BiomeProp_");
            UnityEngine.Object.DestroyImmediate(parallax);
        }
        BuildBackdrop(root.transform, camera, palette.Backdrop, biomeIndex == 0);
        ApplySceneGrade(camera, root, scene.name, palette);
        var lighting = roots.Select(go => go.GetComponent<Light2D>()).FirstOrDefault(light => light != null);
        if (lighting == null)
            lighting = new GameObject("GlobalLight2D", typeof(Light2D)).GetComponent<Light2D>();
        lighting.lightType = Light2D.LightType.Global;
        lighting.color = Color.Lerp(Color.white, palette.Accent, expedition ? .06f : .18f);
        lighting.intensity = palette.Ambient;
        TargetSortingLayers(lighting, SortingLayer.layers);
        var zone = root.GetComponent<BiomeZone>();
        if (!expedition)
        {
            if (zone != null) UnityEngine.Object.DestroyImmediate(zone);
            if (scene.name == "SC_NebulaCantina")
            {
                StoryLight(root.transform, "CounterGlow", new Vector2(0f, 2f), palette.Accent, .38f, 6f, "Default", "Environment_Back", "Player");
                StoryLight(root.transform, "BoothGlow", new Vector2(-6f, 1.5f), new Color(1f, .74f, .39f), .24f, 5f, "Default", "Environment_Back", "Player");
            }
            return;
        }
        if (zone == null) zone = root.AddComponent<BiomeZone>();
        WireChannels(zone);
        var biomes = BiomeAssets();
        SetArray(zone, "biomes", biomes.Length, (p, i) => p.objectReferenceValue = biomes[i]);
        SetArray(zone, "backgroundTiles", 0, null);
        Set(zone, "ambient", lighting);
        StoryLight(player.transform, "SuitGlow", new Vector2(0f, .3f), palette.Accent, .25f, 3.5f, "Player", "Creatures_Front", "Environment_Front");
        Vector3 origin = player.transform.position;
        for (int i = 0; i < 8; i++)
        {
            string name = "BiomeProp_" + i;
            var prop = root.transform.Find(name)?.gameObject;
            if (prop == null)
            {
                prop = new GameObject(name, typeof(SpriteRenderer));
                prop.transform.SetParent(root.transform, false);
            }
            float scale = 1.1f + 0.4f * (i % 4);
            prop.transform.localScale = new Vector3(i % 2 == 0 ? scale : -scale, scale, 1f);
            prop.transform.position = origin + new Vector3(-11 + i * 7.5f, -4.9f + (biomeIndex == 0 ? -i * 0.7f : 0f), 0f);
            var renderer = prop.GetComponent<SpriteRenderer>();
            renderer.sprite = biomes[biomeIndex].EnvironmentProps[0];
            renderer.sortingLayerName = "Environment_Back";
            renderer.sharedMaterial = SpriteLitMaterial();
        }
        if (destination < 0) return;
        for (int i = 0; i < 4; i++)
        {
            var type = (EnvironmentHazardType)i;
            var position = origin + new Vector3(-9 + i * 6, -8, 0);
            BuildEnvironmentHazard(root.transform, position, type);
        }
    }

    private static void ApplySceneGrade(Camera camera, GameObject presentation, string sceneName, StoryPalette palette)
    {
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = palette.Sky;
        camera.allowHDR = true;
        var cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.volumeLayerMask |= 1 << presentation.layer;
        var volume = presentation.GetComponent<Volume>();
        if (volume == null) volume = presentation.AddComponent<Volume>();
        volume.sharedProfile = BuildSceneProfile(sceneName, palette);
        volume.isGlobal = true;
        volume.priority = 1f;
    }

    private static void TargetSortingLayers(Light2D light, SortingLayer[] layers) =>
        SetArray(light, "m_ApplyToSortingLayers", layers.Length, (p, i) => p.intValue = layers[i].id);

    private static void StoryLight(Transform parent, string name, Vector2 position, Color color, float intensity, float radius, params string[] layerNames)
    {
        var light = parent.Find(name)?.GetComponent<Light2D>();
        if (light == null)
        {
            light = new GameObject(name, typeof(Light2D)).GetComponent<Light2D>();
            light.transform.SetParent(parent, false);
        }
        light.transform.localPosition = position;
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * .15f;
        light.overlapOperation = Light2D.OverlapOperation.Additive;
        TargetSortingLayers(light, SortingLayer.layers.Where(layer => layerNames.Contains(layer.name)).ToArray());
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

    private static void DestroyGeneratedChildren(Transform parent, string prefix)
    {
        foreach (var child in parent.Cast<Transform>().Where(t => t.name.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            UnityEngine.Object.DestroyImmediate(child.gameObject);
    }
    private static void BuildEnvironmentHazard(Transform parent, Vector3 position, EnvironmentHazardType type)
    {
        var obj = parent.Find(type.ToString())?.gameObject;
        if (obj == null) obj = new GameObject(type.ToString(), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(EnvironmentHazard));
        obj.transform.SetParent(parent, false); obj.transform.position = position; obj.layer = type == EnvironmentHazardType.CrushingWall ? 9 : 12;
        obj.GetComponent<BoxCollider2D>().isTrigger = type != EnvironmentHazardType.CrushingWall;
        obj.GetComponent<BoxCollider2D>().size = type == EnvironmentHazardType.CrushingWall ? new Vector2(0.8f, 4f) : new Vector2(2f, 2f);
        obj.GetComponent<SpriteRenderer>().sortingLayerName = "Environment_Front";
        var hazard = obj.GetComponent<EnvironmentHazard>(); WireChannels(hazard); Set(hazard, "hazardType", (int)type);
        string effect = type == EnvironmentHazardType.PoisonCloud ? "toxic" : type == EnvironmentHazardType.LavaVent ? "heat" : "shock";
        SetArray(hazard, "frames", 4, (p, i) => p.objectReferenceValue = ArtSprite("fx_" + effect + "_" + i));
        if (type != EnvironmentHazardType.CrushingWall) return;
        SetArray(hazard, "frames", 1, (p, i) => p.objectReferenceValue = ArtSprite("terrain_scrap"));
        obj.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Sliced; obj.GetComponent<SpriteRenderer>().size = new Vector2(0.8f, 4f);
        var body = obj.GetComponent<Rigidbody2D>(); if (body == null) body = obj.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
        var fixedWall = parent.Find("CrushingBackstop")?.gameObject;
        if (fixedWall == null) fixedWall = new GameObject("CrushingBackstop", typeof(BoxCollider2D), typeof(SpriteRenderer));
        fixedWall.transform.SetParent(parent, false);
        fixedWall.transform.position = position + Vector3.right * 2.4f; fixedWall.layer = 9;
        fixedWall.GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 4f);
        fixedWall.GetComponent<SpriteRenderer>().sprite = ArtSprite("terrain_scrap");
        fixedWall.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Sliced; fixedWall.GetComponent<SpriteRenderer>().size = new Vector2(0.8f, 4f);
        fixedWall.GetComponent<SpriteRenderer>().sortingLayerName = "Environment_Front";
    }
}
