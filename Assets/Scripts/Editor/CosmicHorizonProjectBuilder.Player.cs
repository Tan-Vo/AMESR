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
        sprite.sprite = SpriteAt("Characters/Player/Idle/player_idle_01.png");
        sprite.sortingLayerName = "Player";
        sprite.sharedMaterial = SpriteLitMaterial();
        var animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = BuildPlayerAnimator();
        player.AddComponent<PlayerStateMachine>();
        WireChannels(player.AddComponent<PlayerStats>());
        WireChannels(player.AddComponent<DiveCargo>());
        var controller = player.AddComponent<PlayerController>();
        var ground = new GameObject("GroundCheck").transform;
        ground.SetParent(player.transform, false);
        ground.localPosition = new Vector3(0, -0.44f, 0);
        Set(controller, "groundCheck", ground, "groundLayer", (1 << 9) | (1 << 10), "animator", animator, "spriteRenderer", sprite);
        SetArray(controller, "lowOxygenFrames", 4, (p, i) => p.objectReferenceValue = SpriteAt("Characters/player_anim_low_o2_panic_" + (i + 1).ToString("00") + ".png"));
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
        texture.AddSprite(SpriteAt("Characters/Planetary_Actions/pioneer_06_sprint_thruster.png"));
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
            Frames("Characters/Player/Idle/player_idle_", 10),
            Frames("Characters/Player/Walk/player_walk_", 8),
            Frames("Characters/Player/Run/player_run_", 8),
            Frames("Characters/Player/Jump/player_jump_", 12),
            Frames("Characters/player_anim_swim_thrust_", 6),
            new[] { "Characters/Player/Actions/player_action_hurt_01_impact.png", "Characters/Player/Actions/player_action_hurt_02_recover.png" },
            new[] { "Characters/Player/Actions/player_action_hurt_01_impact.png" },
            Frames("Characters/player_anim_harpoon_aim_", 6),
            new[] { "Characters/Player/Actions/player_action_mine_drill_active.png" },
            new[] { "Characters/Player/Actions/player_action_shoot_01_aim.png", "Characters/Player/Actions/player_action_shoot_02_fire.png", "Characters/Player/Actions/player_action_shoot_03_recoil.png" },
            new[] { "Characters/Player/Actions/player_action_slash_01_windup.png", "Characters/Player/Actions/player_action_slash_02_strike.png", "Characters/Player/Actions/player_action_slash_03_recover.png" },
            Frames("Characters/player_anim_waiter_mode_", 7)
        };
        for (int i = 0; i < names.Length; i++)
        {
            string clipPath = "Assets/Animation/CosmicHorizon/AN_Player_" + names[i] + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
            clip.frameRate = 12f;
            var keys = new ObjectReferenceKeyframe[paths[i].Length + 1];
            for (int frame = 0; frame < keys.Length; frame++) keys[frame] = new ObjectReferenceKeyframe { time = frame / 12f, value = SpriteAt(paths[i][Mathf.Min(frame, paths[i].Length - 1)]) };
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
    private static string[] Frames(string prefix, int count) => Enumerable.Range(1, count).Select(i => prefix + i.ToString("00") + ".png").ToArray();
    private static void BuildDiveScene(Scene scene, GameObject prefab, int biomeIndex)
    {
        EnsureScenePrefab<PlayerController>(scene, prefab);
        var player = scene.GetRootGameObjects().First(root => root.GetComponent<PlayerController>() != null);
        float[] spawnY = { 1f, -275f, -775f, -1275f };
        player.transform.position = new Vector3(0, spawnY[biomeIndex], 0);
        if (!scene.GetRootGameObjects().Any(root => root.name == "WaterVolume"))
        {
            var water = new GameObject("WaterVolume", typeof(BoxCollider2D));
            water.tag = "WaterSurface";
            water.layer = 4;
            water.transform.position = new Vector3(0, -2500, 0);
            var collider = water.GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(10000, 5000);
        }
        if (!scene.GetRootGameObjects().Any(root => root.name == "SurfaceDock"))
        {
            var dock = new GameObject("SurfaceDock", typeof(SpriteRenderer), typeof(BoxCollider2D));
            dock.layer = 9;
            dock.transform.position = new Vector3(-2, -0.5f, 0);
            dock.transform.localScale = new Vector3(6, 1, 1);
            dock.GetComponent<BoxCollider2D>().size = Vector2.one;
            dock.GetComponent<SpriteRenderer>().sprite = SpriteAt("Tilesets/tile_008.png");
            dock.GetComponent<SpriteRenderer>().sharedMaterial = SpriteLitMaterial();
        }
        var camera = scene.GetRootGameObjects().First(root => root.GetComponent<Camera>() != null);
        var view = camera.GetComponent<DiveSceneView>();
        if (view == null) view = camera.AddComponent<DiveSceneView>();
        Set(view, "target", player.transform);
        camera.transform.position = player.transform.position + Vector3.back * 10f;
    }
}
