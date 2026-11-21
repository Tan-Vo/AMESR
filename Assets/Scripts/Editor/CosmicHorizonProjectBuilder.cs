using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CosmicHorizonProjectBuilder
{
    private const string Root = "Assets/ScriptableObjects";
    private static readonly string[] GameEvents = { "OnPlayerDied", "OnPlayerHurt", "OnCreatureCaught", "OnDishServed", "OnCustomerLeft", "OnBossDefeated", "OnGameStateChanged", "OnDayEnded", "OnNightEnded", "OnSaveRequested", "OnNoon", "OnDiveWarning", "OnSunset", "OnMidnight" };

    /// <summary>Creates or updates generated project assets without replacing their GUIDs.</summary>
    [MenuItem("Cosmic Horizon/Build Vertical Slice")]
    public static void BuildVerticalSlice()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        ConfigureProject();
        BuildData();
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureProject()
    {
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = settings.FindProperty("tags");
        foreach (string tag in "Player Enemy Boss MiniBoss Creature Fish Projectile Harpoon GrapplePoint Loot InteractableNPC InteractableObject Customer DishPickup ServingCounter FurnitureSlot WaterSurface DepthTrigger BiomeZone Hazard SavePoint".Split(' '))
        {
            if (Enumerable.Range(0, tags.arraySize).Any(i => tags.GetArrayElementAtIndex(i).stringValue == tag)) continue;
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
        foreach (string name in GameEvents) Ensure<SO_GameEvent>("Events/" + name);
        foreach (string name in new[] { "OnOxygenChanged", "OnHPChanged", "OnDepthChanged" }) Ensure<SO_FloatEvent>("Events/" + name);
        foreach (string name in new[] { "OnCreditsChanged", "OnReputationChanged", "OnStateRequested" }) Ensure<SO_IntEvent>("Events/" + name);
        Ensure<SO_StringEvent>("Events/OnSceneRequested");
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
            int variant = i + 1;
            string[] backgrounds = { "skybox", "distant", "atmospheric", "midground", "foreground" };
            SetArray(data, "backgroundTiles", 5, (p, j) => p.objectReferenceValue = SpriteAt("Backgrounds/bg_parallax_" + backgrounds[j] + "_" + variant.ToString("00") + ".png"));
        }
        var weapon = Ensure<SO_WeaponData>("Data/SO_BasicHarpoon");
        Set(weapon, "weaponName", "Gravity Harpoon", "icon", SpriteAt("Weapons/harpoon_tip_basic_gravity.png"), "projectileSprite", SpriteAt("Weapons/harpoon_tip_basic_gravity.png"));
        var dialogue = Ensure<SO_DialogueLine>("Data/SO_Welcome");
        Set(dialogue, "speakerName", "Tan", "portrait", SpriteAt("Characters/portrait_tan_neutral.png"));
        SetArray(dialogue, "lines", 1, (p, i) => p.stringValue = "Welcome to the Cosmic Horizon.");
        var quest = Ensure<SO_QuestData>("Data/SO_FirstCatch");
        Set(quest, "questName", "First Catch", "description", "Catch three creatures.", "creditReward", 100, "icon", creatures[0].Icon);
        SetArray(quest, "objectives", 1, (p, i) => { p.FindPropertyRelative("requiredAmount").intValue = 3; p.FindPropertyRelative("description").stringValue = "Catch any three creatures."; });
    }

    private static Sprite SpriteAt(string path)
    {
        string fullPath = "Assets/Sprites/" + path;
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
    {
        string path = Root + "/" + relative + ".asset";
        Folder(path.Substring(0, path.LastIndexOf('/')));
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void Set(UnityEngine.Object target, params object[] pairs)
    {
        var serialized = new SerializedObject(target);
        for (int i = 0; i < pairs.Length; i += 2)
        {
            var property = serialized.FindProperty((string)pairs[i]);
            switch (pairs[i + 1])
            {
                case string value: property.stringValue = value; break;
                case int value: property.intValue = value; break;
                case float value: property.floatValue = value; break;
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
