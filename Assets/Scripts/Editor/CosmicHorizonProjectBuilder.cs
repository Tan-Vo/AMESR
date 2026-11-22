using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class CosmicHorizonProjectBuilder
{
    private const string Root = "Assets/ScriptableObjects";
    private static readonly string[] GameEvents = { "OnPlayerDied", "OnPlayerHurt", "OnCreatureCaught", "OnDishServed", "OnCustomerLeft", "OnBossDefeated", "OnGameStateChanged", "OnDayEnded", "OnNightEnded", "OnSaveRequested", "OnNoon", "OnDiveWarning", "OnSunset", "OnMidnight" };

    /// <summary>Creates or updates generated project assets without replacing their GUIDs.</summary>
    [MenuItem("Cosmic Horizon/Build Vertical Slice")]
    public static void BuildVerticalSlice()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) throw new InvalidOperationException("Wait for script compilation and asset import to finish, then rebuild.");
        ConfigureProject();
        BuildEventChannels();
        BuildData();
        BuildScenes();
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureProject()
    {
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = settings.FindProperty("tags");
        foreach (string tag in "Player Enemy Boss MiniBoss Creature Fish Projectile Harpoon GrapplePoint Loot InteractableNPC InteractableObject Customer DishPickup ServingCounter FurnitureSlot WaterSurface DepthTrigger BiomeZone Hazard SavePoint".Split(' '))
        {
            if (UnityEditorInternal.InternalEditorUtility.tags.Contains(tag) || Enumerable.Range(0, tags.arraySize).Any(i => tags.GetArrayElementAtIndex(i).stringValue == tag)) continue;
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        }
        var layers = settings.FindProperty("layers");
        string[] names = { "Player", "Enemy", "Projectile", "Ground", "Platform", "Interactable", "Trigger", "UI_WorldSpace" };
        for (int i = 0; i < names.Length; i++) layers.GetArrayElementAtIndex(i + 6).stringValue = names[i];
        layers.GetArrayElementAtIndex(3).stringValue = "IgnoreRaycast";
        var sorting = settings.FindProperty("m_SortingLayers");
        string[] order = { "BG_Skybox", "BG_Distant", "BG_Atmospheric", "BG_Midground", "Environment_Back", "Creatures_Back", "Default", "Creatures_Front", "Player", "Weapons_FX", "Environment_Front", "BG_Foreground", "UI_WorldSpace", "Overlay" };
        var ids = Enumerable.Range(0, sorting.arraySize).ToDictionary(i => sorting.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue, i => sorting.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").longValue);
        sorting.arraySize = order.Length;
        for (int i = 0; i < order.Length; i++)
        {
            var entry = sorting.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("name").stringValue = order[i];
            entry.FindPropertyRelative("uniqueID").longValue = ids.TryGetValue(order[i], out long id) ? id : 100000 + i;
            entry.FindPropertyRelative("locked").boolValue = false;
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
        for (int a = 6; a <= 12; a++)
        for (int b = 0; b < 32; b++)
        {
            bool collide = a switch
            {
                6 => new[] { 4, 7, 9, 10, 11, 12 }.Contains(b),
                7 => new[] { 6, 8, 9, 10 }.Contains(b),
                8 => new[] { 7, 9 }.Contains(b),
                12 => b == 6,
                _ => !new[] { 6, 7, 8, 12 }.Contains(b)
            };
            if (a == 9 || a == 10 || a == 11)
                collide = b == 6 || (b == 7 && a != 11) || (b == 8 && a == 9) || (!new[] { 6, 7, 8, 12 }.Contains(b));
            Physics2D.IgnoreLayerCollision(a, b, !collide);
        }
    }

    private static void BuildData()
    {
        foreach (string name in GameEvents.Concat(new[] { "OnCaptureSave", "OnSaveLoaded", "OnSaveFailed" })) Ensure<SO_GameEvent>("Events/" + name);
        foreach (string name in new[] { "OnOxygenChanged", "OnHPChanged", "OnDepthChanged" }) Ensure<SO_FloatEvent>("Events/" + name);
        foreach (string name in new[] { "OnCreditsChanged", "OnReputationChanged", "OnStateRequested", "OnStateEntered" }) Ensure<SO_IntEvent>("Events/" + name);
        foreach (string name in new[] { "OnSceneRequested", "OnSceneLoaded", "OnSceneFailed" }) Ensure<SO_StringEvent>("Events/" + name);
        BuildPlayerEvents();
        Ensure<SO_SaveSession>("Data/SO_SaveSession");
        var meat = Ensure<SO_ItemData>("Data/SO_AlienMeat");
        Set(meat, "itemName", "Alien Meat", "icon", SpriteAt("Creatures/alien_creature_001.png"), "category", 0);
        var crystal = Ensure<SO_ItemData>("Data/SO_QuantumCrystal");
        Set(crystal, "itemName", "Quantum Crystal", "icon", SpriteAt("Items/crystal_quantum_biolum_gem_01.png"), "category", 1);
        var recipe = Ensure<SO_DishRecipe>("Data/SO_CosmicSushi");
        Set(recipe, "dishName", "Cosmic Sushi", "dishIcon", SpriteAt("Dishes/dish_sushi_nigiri_maki_06.png"), "dishSprite", SpriteAt("Dishes/dish_sushi_nigiri_maki_06.png"));
        SetArray(recipe, "ingredients", 1, (p, i) => { p.FindPropertyRelative("item").objectReferenceValue = meat; p.FindPropertyRelative("quantity").intValue = 1; });
        string[] species = { "abyssal_viper_angler", "biolum_jellyfish_comb", "crystal_isopod_crustacean", "prehistoric_trilobite_shell", "void_electric_eel" };
        var creatures = new SO_CreatureData[species.Length];
        for (int i = 0; i < species.Length; i++)
        {
            var data = creatures[i] = Ensure<SO_CreatureData>("Data/SO_" + species[i]);
            string prefix = "Creatures/fauna_" + species[i] + "_";
            Set(data, "creatureName", species[i].Replace('_', ' '), "icon", SpriteAt(prefix + "01.png"), "behaviorType", i == 0 || i == 4 ? 3 : 1);
            SetArray(data, "animationFrames", 20, (p, frame) => p.objectReferenceValue = SpriteAt(prefix + (frame + 1).ToString("00") + ".png"));
            SetArray(data, "lootTable", 1, (p, index) => { p.FindPropertyRelative("item").objectReferenceValue = meat; p.FindPropertyRelative("weight").intValue = 100; p.FindPropertyRelative("minAmount").intValue = 1; p.FindPropertyRelative("maxAmount").intValue = 3; });
            SetArray(data, "usableInRecipes", 1, (p, index) => p.objectReferenceValue = recipe);
        }
        foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
        {
            var data = Ensure<SO_UpgradeData>("Data/SO_" + category);
            Set(data, "upgradeName", category.ToString(), "category", (int)category, "icon", SpriteAt("Gear/gear_tech_001.png"), "costMultiplier", category == UpgradeCategory.OxygenTank || category == UpgradeCategory.Jetpack ? 2.15f : 1.618f);
            SetArray(data, "bonusPerLevel", 10, (p, i) => p.floatValue = (i + 1) * 10);
        }
        string[] biomes = { "Shallows", "Twilight", "Abyssal", "EventHorizon" };
        int[] depths = { 0, 500, 1500, 2500, 10000 };
        for (int i = 0; i < biomes.Length; i++)
        {
            var data = Ensure<SO_BiomeData>("Data/SO_" + biomes[i]);
            Set(data, "biomeName", biomes[i], "depthMin", depths[i], "depthMax", depths[i + 1]);
            SetArray(data, "spawnableCreatures", creatures.Length, (p, j) => p.objectReferenceValue = creatures[j]);
            SetArray(data, "spawnWeights", creatures.Length, (p, j) => p.floatValue = 1);
            SetArray(data, "backgroundTiles", 5, (p, j) => p.objectReferenceValue = GeneratedBackground("dive_" + new[] { "shallows", "twilight", "abyssal", "eventhorizon" }[i]));
        }
        var weapon = Ensure<SO_WeaponData>("Data/SO_BasicHarpoon");
        Set(weapon, "weaponName", "Gravity Harpoon", "icon", SpriteAt("Weapons/harpoon_tip_basic_gravity.png"), "projectileSprite", SpriteAt("Weapons/harpoon_tip_basic_gravity.png"));
        var dialogue = Ensure<SO_DialogueLine>("Data/SO_Welcome");
        Set(dialogue, "speakerName", "Tan", "portrait", SpriteAt("Characters/portrait_tan_neutral.png"));
        SetArray(dialogue, "lines", 1, (p, i) => p.stringValue = "Welcome to the Cosmic Horizon.");
        var quest = Ensure<SO_QuestData>("Data/SO_FirstCatch");
        Set(quest, "questName", "First Catch", "description", "Catch three creatures.", "creditReward", 100, "icon", creatures[0].Icon);
        SetArray(quest, "objectives", 1, (p, i) => { p.FindPropertyRelative("requiredAmount").intValue = 3; p.FindPropertyRelative("description").stringValue = "Catch any three creatures."; });
        BuildEcosystemData();
        BuildCantinaData();
        BuildBossData();
        BuildPolishData();
        BuildUIData();
    }

    private static void BuildScenes()
    {
        Folder("Assets/Prefabs/Core");
        Folder("Assets/Scenes");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildUIWidgets();
            var worldPrefabs = BuildWorldSystemsPrefabs();
            var game = new GameObject("GameManager");
            var manager = game.AddComponent<GameManager>();
            WireChannels(manager);
            Set(manager, "starterSupply", Ensure<SO_ItemData>("Data/SO_Medkit"));
            WireChannels(game.AddComponent<UIFlowRouter>());
            var clock = game.AddComponent<DayNightCycleManager>();
            WireChannels(clock);
            var gamePrefab = PrefabUtility.SaveAsPrefabAsset(game, "Assets/Prefabs/Core/PF_GameManager.prefab");
            var save = new GameObject("SaveLoadManager");
            WireChannels(save.AddComponent<SaveLoadManager>());
            var savePrefab = PrefabUtility.SaveAsPrefabAsset(save, "Assets/Prefabs/Core/PF_SaveLoadManager.prefab");
            var transition = new GameObject("SceneTransitionManager");
            var transitionManager = transition.AddComponent<SceneTransitionManager>();
            WireChannels(transitionManager);
            var canvasObject = new GameObject("FadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transition.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var group = canvasObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var imageObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            imageObject.GetComponent<Image>().color = Color.black;
            Set(transitionManager, "fadeCanvasGroup", group);
            var transitionPrefab = PrefabUtility.SaveAsPrefabAsset(transition, "Assets/Prefabs/Core/PF_SceneTransitionManager.prefab");
            var playerPrefab = BuildPlayerPrefab();
            var ecosystemPrefab = BuildEcosystemPrefab();
            var customerPrefab = BuildCustomerPrefab();
            var bossPrefabs = BuildBossPrefabs();
            string[] names = { "SC_MainMenu", "SC_Tutorial", "SC_Mothership", "SC_DiveZone1_Shallows", "SC_DiveZone2_Twilight", "SC_DiveZone3_Abyssal", "SC_DiveZone4_EventHorizon", "SC_NebulaCantina", "SC_BossArena" };
            var buildScenes = new EditorBuildSettingsScene[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string path = "Assets/Scenes/" + names[i] + ".unity";
                Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null
                    ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)
                    : EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                EditorSceneManager.SaveScene(scene, path);
                if (names[i] != "SC_BossArena")
                {
                    EnsureScenePrefab<GameManager>(scene, gamePrefab);
                    EnsureScenePrefab<SaveLoadManager>(scene, savePrefab);
                    EnsureScenePrefab<SceneTransitionManager>(scene, transitionPrefab);
                    EnsureScenePrefab<AudioManager>(scene, worldPrefabs[0]);
                    EnsureScenePrefab<QuestManager>(scene, worldPrefabs[1]);
                    EnsureScenePrefab<TutorialManager>(scene, worldPrefabs[2]);
                    EnsureScenePrefab<CurrencyManager>(scene, worldPrefabs[3]);
                    if (!scene.GetRootGameObjects().Any(root => root.GetComponent<Camera>() != null))
                    {
                        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                        camera.tag = "MainCamera";
                        camera.transform.position = new Vector3(0, 0, -10);
                        camera.GetComponent<Camera>().orthographic = true;
                        camera.GetComponent<Camera>().orthographicSize = 7;
                    }
                }
                if (names[i] == "SC_BossArena" && !scene.GetRootGameObjects().Any(root => root.GetComponent<Camera>() != null))
                {
                    var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camera.tag = "MainCamera";
                    camera.transform.position = new Vector3(0, 0, -10);
                    camera.GetComponent<Camera>().orthographic = true; camera.GetComponent<Camera>().orthographicSize = 9;
                }
                foreach (var camera in scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).Where(value => value != null)) camera.aspect = 16f / 9f;
                if (names[i] == "SC_Tutorial" || names[i].StartsWith("SC_DiveZone", StringComparison.Ordinal))
                {
                    BuildDiveScene(scene, playerPrefab, i >= 3 && i <= 6 ? i - 3 : 0);
                    BuildEcosystemScene(scene, ecosystemPrefab);
                    BuildBossTrigger(scene, i >= 3 && i <= 6 ? i - 3 : 0);
                }
                if (names[i] == "SC_NebulaCantina") BuildCantinaScene(scene, playerPrefab, customerPrefab);
                if (names[i] == "SC_BossArena") BuildBossArena(scene, bossPrefabs, playerPrefab);
                BuildSceneUI(scene);
                BuildScenePolish(scene);
                EditorSceneManager.SaveScene(scene, path);
                buildScenes[i] = new EditorBuildSettingsScene(path, true);
            }
            EditorBuildSettings.scenes = buildScenes;
        }
        finally { if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    private static void EnsureScenePrefab<T>(Scene scene, GameObject prefab) where T : Component
    {
        if (!scene.GetRootGameObjects().Any(root => root.GetComponent<T>() != null))
            PrefabUtility.InstantiatePrefab(prefab, scene);
    }

    private static void WireChannels(UnityEngine.Object component)
    {
        if (component == null) throw new ArgumentNullException(nameof(component));
        var serialized = new SerializedObject(component);
        var property = serialized.GetIterator();
        while (property.NextVisible(true))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference || property.name == "m_Script") continue;
            if (property.name == "session")
                property.objectReferenceValue = Ensure<SO_SaveSession>("Data/SO_SaveSession");
            else if (property.name.StartsWith("on", StringComparison.Ordinal))
            {
                string name = char.ToUpperInvariant(property.name[0]) + property.name.Substring(1);
                Type owner = component.GetType(); FieldInfo field = null;
                while (owner != null && field == null) { field = owner.GetField(property.name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); owner = owner.BaseType; }
                if (field == null || !typeof(ScriptableObject).IsAssignableFrom(field.FieldType)) continue;
                property.objectReferenceValue = Ensure(field.FieldType, "Events/" + name);
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite SpriteAt(string path)
    {
        string fullPath = "Assets/Sprites/" + path;
        var frames = AssetDatabase.LoadAllAssetsAtPath(fullPath).OfType<Sprite>()
            .Where(frame => frame.rect.width < frame.texture.width || frame.rect.height < frame.texture.height)
            .OrderBy(frame => frame.name, StringComparer.Ordinal).ToArray();
        if (frames.Length > 0) return frames[0];
        if (PresentationRules.IsSheetAsset(path))
        {
            Debug.LogWarning("Rejected sheet-like sprite: " + fullPath + ". Slice it into unlabelled animation frames or select a single-image asset.");
            throw new InvalidOperationException("A whole sprite sheet cannot be used as a scene sprite: " + fullPath);
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
        if (sprite == null) throw new InvalidOperationException("Missing imported sprite: " + fullPath);
        return sprite;
    }

    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        Folder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    private static T Ensure<T>(string relative) where T : ScriptableObject
        => (T)Ensure(typeof(T), relative);

    private static void BuildEventChannels()
    {
        var types = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().Concat(TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            .Where(type => type.Assembly == typeof(GameManager).Assembly);
        foreach (var type in types)
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            if (field.IsDefined(typeof(SerializeField), true) && field.Name.StartsWith("on", StringComparison.Ordinal) && typeof(ScriptableObject).IsAssignableFrom(field.FieldType))
                Ensure(field.FieldType, "Events/" + char.ToUpperInvariant(field.Name[0]) + field.Name.Substring(1));
        foreach (string name in GameEvents) Ensure<SO_GameEvent>("Events/" + name);
    }

    private static ScriptableObject Ensure(Type type, string relative)
    {
        string path = Root + "/" + relative + ".asset";
        Folder(path.Substring(0, path.LastIndexOf('/')));
        var asset = AssetDatabase.LoadAssetAtPath(path, type) as ScriptableObject;
        if (asset != null) return asset;
        if (!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "..", path)))
        {
            asset = ScriptableObject.CreateInstance(type);
            if (asset == null) throw new InvalidOperationException("Cannot create " + type.FullName + ". Resolve script compilation errors before rebuilding.");
            AssetDatabase.CreateAsset(asset, path);
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        asset = AssetDatabase.LoadAssetAtPath(path, type) as ScriptableObject;
        if (asset == null) throw new InvalidOperationException("Cannot load " + path + " as " + type.FullName + ". Check its script reference and resolve compilation/import errors before rebuilding.");
        return asset;
    }

    private static void Set(UnityEngine.Object target, params object[] pairs)
    {
        if (target == null) throw new ArgumentNullException(nameof(target), "Cannot serialize a missing builder asset. Check script compilation/import errors.");
        var serialized = new SerializedObject(target);
        for (int i = 0; i < pairs.Length; i += 2)
        {
            var property = serialized.FindProperty((string)pairs[i]);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + " has no serialized field " + pairs[i]);
            switch (pairs[i + 1])
            {
                case null: property.objectReferenceValue = null; break;
                case string value: property.stringValue = value; break;
                case int value: property.intValue = value; break;
                case float value: property.floatValue = value; break;
                case Color value: property.colorValue = value; break;
                case Vector2 value: property.vector2Value = value; break;
                case Vector3 value: property.vector3Value = value; break;
                case bool value: property.boolValue = value; break;
                case UnityEngine.Object value: property.objectReferenceValue = value; break;
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetArray(UnityEngine.Object target, string field, int count, Action<SerializedProperty, int> fill)
    {
        var serialized = new SerializedObject(target);
        var array = serialized.FindProperty(field);
        array.arraySize = count;
        for (int i = 0; i < count; i++) fill(array.GetArrayElementAtIndex(i), i);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
