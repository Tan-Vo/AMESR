using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class CosmicHorizonProjectBuilder
{
    private static void BuildPlayerEvents()
    {
        foreach (string name in new[] { "OnLowOxygen", "OnEmergencySurface", "OnJetpackOverheated", "OnSplash" }) Ensure<SO_GameEvent>("Events/" + name);
        foreach (string name in new[] { "OnFuelChanged", "OnSuffocationChanged", "OnLootPenalty" }) Ensure<SO_FloatEvent>("Events/" + name);
        Ensure<SO_IntEvent>("Events/OnPlayerDamageRequested");
    }
    private static GameObject BuildPlayerPrefab()
    {
        Folder("Assets/Prefabs/Player");
        Folder("Assets/Animation/CosmicHorizon");
        var player = new GameObject("Player");
        player.tag = "Player";
        player.layer = 6;
        var rb = player.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var collider = player.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.5f, 0.85f);
        var sprite = player.AddComponent<SpriteRenderer>();
        sprite.sprite = ArtSprite("pilot_idle");
        sprite.sortingLayerName = "Player";
        sprite.sharedMaterial = SpriteLitMaterial();
        var animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = BuildPlayerAnimator();
        player.AddComponent<PlayerStateMachine>();
        var stats = player.AddComponent<PlayerStats>(); WireChannels(stats);
        Set(stats, "gear", Ensure<SO_GearProfile>("Data/SO_GearProfile"));
        WireChannels(player.AddComponent<DiveCargo>());
        var controller = player.AddComponent<PlayerController>();
        WireChannels(controller);
        var ground = new GameObject("GroundCheck").transform;
        ground.SetParent(player.transform, false);
        ground.localPosition = new Vector3(0, -0.44f, 0);
        Set(controller, "groundCheck", ground, "groundLayer", (1 << 9) | (1 << 10), "animator", animator, "spriteRenderer", sprite);
        SetArray(controller, "lowOxygenFrames", 2, (p, i) => p.objectReferenceValue = ArtSprite(i == 0 ? "pilot_land" : "pilot_jump"));
        var jetpack = player.AddComponent<JetpackSystem>();
        WireChannels(jetpack);
        var thrusterObject = new GameObject("Thruster", typeof(ParticleSystem));
        thrusterObject.transform.SetParent(player.transform, false);
        thrusterObject.transform.localPosition = new Vector3(0, -0.35f, 0);
        thrusterObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
        var particles = thrusterObject.GetComponent<ParticleSystem>();
        var main = particles.main;
        main.playOnAwake = false;
        main.startLifetime = 0.2f;
        main.startSpeed = 2f;
        main.startSize = 0.2f;
        main.maxParticles = 24;
        var texture = particles.textureSheetAnimation;
        texture.enabled = true;
        texture.mode = ParticleSystemAnimationMode.Sprites;
        for (int i = 0; i < 4; i++) texture.AddSprite(ArtSprite("fx_thruster_" + i));
        Set(jetpack, "thrusterParticles", particles);
        WireChannels(player.AddComponent<OxygenSystem>());
        WireChannels(player.AddComponent<WaterSurfaceTransition>());
        var depth = player.AddComponent<DepthManager>();
        WireChannels(depth);
        SetArray(depth, "biomes", 4, (p, i) => p.objectReferenceValue = BiomeAssets()[i]);
        var feedback = player.AddComponent<PlayerVitalsFeedback>();
        WireChannels(feedback);
        var canvasObject = new GameObject("VitalsCanvas", typeof(Canvas));
        canvasObject.transform.SetParent(player.transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.GetComponent<Canvas>().sortingOrder = 100;
        Set(feedback, "waterOverlay", Overlay(canvasObject.transform, "WaterTint"), "oxygenOverlay", Overlay(canvasObject.transform, "OxygenWarning"));
        BuildDivingPlayer(player, canvasObject.transform);
        BuildCombatPlayer(player, canvasObject.transform);
        return PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Player/PF_Player.prefab");
    }
    private static Image Overlay(Transform parent, string name)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = obj.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = Color.clear;
        return image;
    }
    private static SO_BiomeData[] BiomeAssets() => new[] { "Shallows", "Twilight", "Abyssal", "EventHorizon" }.Select(name => Ensure<SO_BiomeData>("Data/SO_" + name)).ToArray();
    private static AnimatorController BuildPlayerAnimator()
    {
        string path = "Assets/Animation/CosmicHorizon/AC_Player.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
        foreach (var state in machine.states) machine.RemoveState(state.state);
        controller.parameters = new AnimatorControllerParameter[0];
        string[] bools = { "isRunning", "isJumping", "isSwimming", "isDiving", "isHarpooning", "isWaitering", "isHurt", "isDead", "isMining", "isShooting", "isSlashing" };
        foreach (string parameter in bools) controller.AddParameter(parameter, AnimatorControllerParameterType.Bool);
        foreach (string parameter in new[] { "Speed", "VelocityY" }) controller.AddParameter(parameter, AnimatorControllerParameterType.Float);
        foreach (string parameter in new[] { "Attack", "Hurt", "Die" }) controller.AddParameter(parameter, AnimatorControllerParameterType.Trigger);
        string[] names = { "Idle", "Walk", "Run", "Jump", "Swim", "Hurt", "Dead", "HarpoonAim", "Mining", "Shoot", "Slash", "Waiter" };
        string[][] paths = {
            new[] { "idle", "idle_breath" },
            new[] { "stride_left", "idle", "stride_right", "idle" },
            new[] { "run", "stride_left", "run", "stride_right" },
            new[] { "jump" },
            new[] { "swim", "swim_glide", "swim" },
            new[] { "land", "idle" },
            new[] { "land" },
            new[] { "aim" },
            new[] { "mine", "mine_work", "mine" },
            new[] { "aim", "aim_kick", "aim" },
            new[] { "aim", "run", "land" },
            new[] { "stride_left", "idle", "stride_right", "idle" }
        };
        for (int i = 0; i < names.Length; i++)
        {
            string clipPath = "Assets/Animation/CosmicHorizon/AN_Player_" + names[i] + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
            clip.frameRate = 12f;
            var keys = new ObjectReferenceKeyframe[paths[i].Length + 1];
            float interval = i == 0 ? 0.45f : 0.12f;
            for (int frame = 0; frame < keys.Length; frame++)
                keys[frame] = new ObjectReferenceKeyframe { time = frame * interval, value = ArtSprite("pilot_" + paths[i][Mathf.Min(frame, paths[i].Length - 1)]) };
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = i < 3 || i == 4 || i == 7 || i == 8 || i == 11;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            var state = machine.AddState(names[i]);
            state.motion = clip;
            if (i == 0) machine.defaultState = state;
            var transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = 0;
            transition.canTransitionToSelf = false;
            transition.AddCondition(i == 6 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "isDead");
            if (i == 6) continue;
            transition.AddCondition(i == 5 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "isHurt");
            if (i == 5) continue;
            transition.AddCondition(i == 11 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "isWaitering");
            if (i == 11) continue;
            transition.AddCondition(i == 7 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "isHarpooning");
            if (i == 7) continue;
            transition.AddCondition(i == 8 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "isMining");
            if (i == 8) continue;
            transition.AddCondition(i == 9 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "isShooting");
            if (i == 9) continue;
            transition.AddCondition(i == 10 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "isSlashing");
            if (i == 10) continue;
            transition.AddCondition(i == 4 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "isSwimming");
            if (i == 4) continue;
            transition.AddCondition(i == 3 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "isJumping");
            if (i == 3) continue;
            transition.AddCondition(i == 2 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "isRunning");
            if (i < 2) transition.AddCondition(i == 1 ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, 0.1f, "Speed");
        }
        EditorUtility.SetDirty(controller);
        return controller;
    }
    private static void BuildDiveScene(Scene scene, GameObject prefab, int biomeIndex)
    {
        EnsureScenePrefab<PlayerController>(scene, prefab);
        var player = scene.GetRootGameObjects().First(root => root.GetComponent<PlayerController>() != null);
        float[] spawnY = { 1f, -275f, -775f, -1275f };
        player.transform.position = new Vector3(0, spawnY[biomeIndex], 0);
        bool wet = biomeIndex == 0;
        Set(player.GetComponent<PlayerController>(), "spaceFlight", !wet);
        var water = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "WaterVolume");
        var dock = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "SurfaceDock");
        if (!wet)
        {
            if (water != null) Object.DestroyImmediate(water);
            if (dock != null) Object.DestroyImmediate(dock);
            BuildExpeditionTerrain(scene, player.transform.position, biomeIndex);
            BuildSuitTrail(player);
        }
        else if (water == null)
        {
            water = new GameObject("WaterVolume", typeof(BoxCollider2D));
            water.tag = "WaterSurface";
            water.layer = 4;
            water.transform.position = new Vector3(0, -2500, 0);
            var collider = water.GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(10000, 5000);
        }
        if (wet)
        {
            if (dock == null) dock = new GameObject("SurfaceDock", typeof(SpriteRenderer), typeof(BoxCollider2D));
            dock.layer = 9;
            dock.transform.position = new Vector3(-2, -0.5f, 0);
            ConfigurePlatform(dock, ArtSprite("terrain_deck"), new Vector2(6, 1));
        }
        var camera = scene.GetRootGameObjects().First(root => root.GetComponent<Camera>() != null);
        var view = camera.GetComponent<DiveSceneView>();
        if (view == null) view = camera.AddComponent<DiveSceneView>();
        Set(view, "target", player.transform);
        camera.transform.position = player.transform.position + Vector3.back * 10f;
        var legacyAtmosphere = camera.transform.Find("DiveAtmosphere");
        if (wet)
            BuildDiveAtmosphere(camera.transform);
        else if (legacyAtmosphere != null)
            Object.DestroyImmediate(legacyAtmosphere.gameObject);
    }

    private static void BuildSuitTrail(GameObject player)
    {
        var trail = player.transform.Find("SuitTrail")?.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = new GameObject("SuitTrail", typeof(TrailRenderer)).GetComponent<TrailRenderer>();
            trail.transform.SetParent(player.transform, false);
        }
        const string materialPath = "Assets/Materials/M_SuitTrail.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        trail.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        trail.sharedMaterial = material;
        trail.sortingLayerName = "Player";
        trail.sortingOrder = -1;
        trail.emitting = false;
        trail.time = 0.32f;
        trail.minVertexDistance = 0.035f;
        trail.startWidth = 0.135f;
        trail.endWidth = 0f;
        trail.startColor = new Color(0.76f, 0.95f, 1f, 0.8f);
        trail.endColor = new Color(0.31f, 0.66f, 0.93f, 0f);
        Set(player.GetComponent<PlayerController>(), "suitTrail", trail);
    }

    private static void BuildExpeditionTerrain(Scene scene, Vector3 origin, int biomeIndex)
    {
        var terrain = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "ExpeditionTerrain");
        if (terrain == null) terrain = new GameObject("ExpeditionTerrain");
        var sprite = ArtSprite(biomeIndex == 1 ? "terrain_regolith" : biomeIndex == 2 ? "terrain_basalt" : "terrain_wreck");
        for (int i = 0; i < 8; i++)
        {
            string name = "Ledge_" + i;
            var ledge = terrain.transform.Find(name)?.gameObject;
            if (ledge == null)
            {
                ledge = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D));
                ledge.transform.SetParent(terrain.transform, false);
            }
            ledge.layer = 9;
            ledge.transform.position = origin + new Vector3(-12f + i * 8f, -5f - Mathf.Abs(i - 2) * 0.4f, 0f);
            ConfigurePlatform(ledge, sprite, new Vector2(7f, 1.1f));
        }
    }

    private static void ConfigurePlatform(GameObject platform, Sprite sprite, Vector2 size)
    {
        var spriteSize = new Vector2(size.x, sprite.bounds.size.y);
        platform.transform.localScale = new Vector3(1f, size.y / spriteSize.y, 1f);
        var renderer = platform.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = SpriteLitMaterial();
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = spriteSize;
        platform.GetComponent<BoxCollider2D>().size = spriteSize;
    }

    private static void BuildDiveAtmosphere(Transform camera)
    {
        Folder("Assets/Materials");
        const string path = "Assets/Materials/M_DiveAtmosphere_0.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/DiveAtmosphere.shader");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_WaterTint", BiomeAssets()[0].WaterTintColor);
        EditorUtility.SetDirty(material);

        const string name = "DiveAtmosphere";
        var atmosphere = camera.Find(name);
        if (atmosphere == null)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            atmosphere = quad.transform;
            atmosphere.SetParent(camera, false);
        }
        atmosphere.localPosition = Vector3.forward * 10f;
        // Cover the maximum generated camera zoom even at ultrawide aspect ratios.
        atmosphere.localScale = new Vector3(84f, 28f, 1f);
        var renderer = atmosphere.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Environment_Back";
        renderer.sortingOrder = -20;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
