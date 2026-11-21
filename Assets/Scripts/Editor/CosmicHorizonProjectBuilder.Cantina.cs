using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class CosmicHorizonProjectBuilder
{
    private static SO_DishRecipe[] CantinaRecipes() => Enumerable.Range(0, 9).Select(i => Ensure<SO_DishRecipe>("Data/SO_CantinaDish" + i)).ToArray();
    private static void BuildCantinaData()
    {
        foreach (string name in new[] { "OnCreditsAdded", "OnCreditsChanged", "OnUpgradeChanged", "OnReputationRequested", "OnReputationChanged" }) Ensure<SO_IntEvent>("Events/" + name);
        var stock = Ensure<SO_IngredientStock>("Data/SO_IngredientStock");
        Set(stock, "session", Ensure<SO_SaveSession>("Data/SO_SaveSession"));
        Ensure<SO_NightReport>("Data/SO_NightReport");
        var recipes = CantinaRecipes();
        var creatures = CreatureAssets();
        for (int i = 0; i < recipes.Length; i++)
        {
            var ingredient = i == 0 ? Ensure<SO_ItemData>("Data/SO_AlienMeat") : Ensure<SO_ItemData>("Data/SO_Catch_" + creatures[i - 1].name);
            var sprite = SpriteAt("Dishes/dish_space_" + (i + 1).ToString("000") + ".png");
            Set(recipes[i], "dishName", i == 0 ? "Cosmic Fillet" : creatures[i - 1].CreatureName + " Platter",
                "dishIcon", sprite, "dishSprite", sprite, "basePrice", 50 + i * 20, "cookTime", 3f + i * 0.15f, "servingsPerCook", 1);
            SetArray(recipes[i], "ingredients", 1, (p, j) => { p.FindPropertyRelative("item").objectReferenceValue = ingredient; p.FindPropertyRelative("quantity").intValue = 1; });
        }
        Set(Ensure<SO_UpgradeData>("Data/SO_WaiterTray"), "maxLevel", 2);
        Set(Ensure<SO_UpgradeData>("Data/SO_CantinaSeats"), "maxLevel", 4);
    }
    private static GameObject BuildCustomerPrefab()
    {
        Folder("Assets/Prefabs/Cantina");
        var customer = new GameObject("Customer", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(CapsuleCollider2D), typeof(CustomerAI));
        customer.GetComponent<CapsuleCollider2D>().isTrigger = true;
        customer.tag = "Customer";
        customer.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        customer.GetComponent<Rigidbody2D>().gravityScale = 0f;
        customer.GetComponent<SpriteRenderer>().sprite = SpriteAt("Characters/character_npc_001.png");
        customer.GetComponent<SpriteRenderer>().sortingLayerName = "Player";
        customer.GetComponent<SpriteRenderer>().sharedMaterial = SpriteLitMaterial();
        var bubble = new GameObject("Order", typeof(SpriteRenderer));
        bubble.transform.SetParent(customer.transform, false);
        bubble.transform.localPosition = Vector3.up * 0.85f;
        bubble.transform.localScale = Vector3.one * 0.6f;
        bubble.GetComponent<SpriteRenderer>().sortingLayerName = "UI_WorldSpace";
        var canvas = new GameObject("Patience", typeof(Canvas));
        canvas.transform.SetParent(customer.transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        canvas.GetComponent<Canvas>().sortingLayerName = "UI_WorldSpace";
        canvas.transform.localPosition = Vector3.up * 1.35f;
        canvas.transform.localScale = Vector3.one * 0.01f;
        var meter = DiveMeter(canvas.transform, "PatienceMeter", Vector2.zero, true);
        meter.sprite = SpriteAt("UI/hud_target_crosshair_radar_01.png"); meter.fillMethod = Image.FillMethod.Radial360;
        meter.rectTransform.sizeDelta = new Vector2(56, 56);
        meter.rectTransform.anchorMin = meter.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        meter.rectTransform.anchoredPosition = Vector2.zero;
        Set(customer.GetComponent<CustomerAI>(), "orderBubble", bubble.GetComponent<SpriteRenderer>(), "patienceMeter", meter);
        return PrefabUtility.SaveAsPrefabAsset(customer, "Assets/Prefabs/Cantina/PF_Customer.prefab");
    }
    private static Text ServiceText(Transform parent, string name, Vector2 position, Vector2 size, int fontSize = 20)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var label = obj.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
        return label;
    }
    private static GameObject Furniture(Transform parent, string name, string sprite, Vector2 position)
    {
        var obj = new GameObject(name, typeof(SpriteRenderer));
        obj.transform.SetParent(parent, false); obj.transform.position = position;
        obj.GetComponent<SpriteRenderer>().sprite = SpriteAt("Furniture/" + sprite + ".png");
        obj.GetComponent<SpriteRenderer>().sharedMaterial = SpriteLitMaterial();
        obj.GetComponent<SpriteRenderer>().sortingLayerName = "Environment_Back";
        return obj;
    }
    private static void BuildCantinaScene(Scene scene, GameObject playerPrefab, GameObject customerPrefab)
    {
        if (scene.GetRootGameObjects().Any(root => root.GetComponent<CantinaManager>() != null)) return;
        var root = new GameObject("CantinaService");
        var manager = root.AddComponent<CantinaManager>(); WireChannels(manager);
        var kitchen = root.AddComponent<KitchenQueue>(); WireChannels(kitchen);
        var reputation = root.AddComponent<ReputationSystem>(); WireChannels(reputation);
        var spawner = root.AddComponent<CustomerSpawner>();
        var pool = Pool<CustomerPool>(root.transform, "CustomerPool", customerPrefab.GetComponent<CustomerAI>()); Set(pool, "initialSize", 15);
        Set(spawner, "pool", pool);
        SetArray(spawner, "appearances", 100, (p, i) => p.objectReferenceValue = SpriteAt("Characters/character_npc_" + (i + 1).ToString("000") + ".png"));
        var entrance = Furniture(root.transform, "Entrance", "furn_crew_bunk_airlock_01", new Vector2(-10, 0));
        var exit = Furniture(root.transform, "Exit", "furn_crew_bunk_airlock_02", new Vector2(10, 0));
        var counter = Furniture(root.transform, "ServingCounter", "furn_power_charger_workbench_01", new Vector2(0, 0));
        Furniture(root.transform, "Kitchen", "furn_power_charger_workbench_02", new Vector2(0, 2));
        var ready = new GameObject("ReadyDish", typeof(SpriteRenderer)); ready.transform.SetParent(counter.transform, false);
        ready.transform.localPosition = Vector3.up * 0.8f; ready.GetComponent<SpriteRenderer>().sortingLayerName = "Weapons_FX";
        Set(kitchen, "readyDish", ready.GetComponent<SpriteRenderer>());
        var seats = new Transform[8];
        for (int i = 0; i < seats.Length; i++) seats[i] = Furniture(root.transform, "Seat" + i, "furn_neon_table_booth_01", new Vector2(-7 + i * 2, 0)).transform;
        SetArray(manager, "seats", seats.Length, (p, i) => p.objectReferenceValue = seats[i]);
        Set(manager, "stock", Ensure<SO_IngredientStock>("Data/SO_IngredientStock"), "report", Ensure<SO_NightReport>("Data/SO_NightReport"),
            "kitchen", kitchen, "spawner", spawner, "entranceDoor", entrance.transform, "exitDoor", exit.transform);
        var shop = root.AddComponent<ShopSystem>(); WireChannels(shop);
        var upgrades = System.Enum.GetValues(typeof(UpgradeCategory)).Cast<UpgradeCategory>().Select(category => Ensure<SO_UpgradeData>("Data/SO_" + category)).ToArray();
        SetArray(shop, "availableUpgrades", upgrades.Length, (p, i) => p.objectReferenceValue = upgrades[i]);
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene); player.transform.position = new Vector3(0, 0.1f, 0);
        foreach (var component in player.GetComponents<MonoBehaviour>())
            if (component is HarpoonSystem || component is GrappleSystem || component is MiningSystem || component is WeaponBase || component is WeaponController ||
                component is JetpackSystem || component is OxygenSystem || component is WaterSurfaceTransition) component.enabled = false;
        player.transform.Find("VitalsCanvas/DiveControls").gameObject.SetActive(false);
        var tray = new GameObject("WaiterTray", typeof(SpriteRenderer)); tray.transform.SetParent(player.transform, false); tray.transform.localPosition = new Vector3(0.4f, 0.4f);
        tray.GetComponent<SpriteRenderer>().sortingLayerName = "Weapons_FX";
        var serving = player.AddComponent<ServingSystem>(); WireChannels(serving);
        Set(serving, "cantina", manager, "kitchen", kitchen, "counter", counter.transform, "traySprite", tray.GetComponent<SpriteRenderer>());
        var floor = new GameObject("CantinaFloor", typeof(SpriteRenderer), typeof(BoxCollider2D)); floor.layer = 9;
        floor.transform.position = new Vector3(0, -1, 0); floor.transform.localScale = new Vector3(25, 1, 1);
        floor.GetComponent<SpriteRenderer>().sprite = SpriteAt("Tilesets/tile_008.png"); floor.GetComponent<SpriteRenderer>().sharedMaterial = SpriteLitMaterial();
        var camera = scene.GetRootGameObjects().First(obj => obj.GetComponent<Camera>() != null).GetComponent<Camera>();
        camera.orthographicSize = 7;
        var lighting = new GameObject("CantinaLight", typeof(UnityEngine.Rendering.Universal.Light2D));
        lighting.GetComponent<UnityEngine.Rendering.Universal.Light2D>().lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
        lighting.GetComponent<UnityEngine.Rendering.Universal.Light2D>().intensity = 0.9f;
        var canvas = new GameObject("CantinaCanvas", typeof(Canvas), typeof(GraphicRaycaster)); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var prep = new GameObject("MenuPrep", typeof(RectTransform)); prep.transform.SetParent(canvas.transform, false);
        var menu = root.AddComponent<MenuSetup>();
        var recipes = CantinaRecipes();
        Set(menu, "cantina", manager, "stock", Ensure<SO_IngredientStock>("Data/SO_IngredientStock"), "prepPanel", prep,
            "status", ServiceText(canvas.transform, "Status", new Vector2(0, -260), new Vector2(1100, 180), 18));
        SetArray(menu, "recipes", recipes.Length, (p, i) => p.objectReferenceValue = recipes[i]);
        var labels = new Text[recipes.Length];
        for (int i = 0; i < recipes.Length; i++)
        {
            labels[i] = ServiceText(prep.transform, "Recipe" + i, new Vector2(0, 220 - i * 42), new Vector2(1000, 40), 18);
            labels[i].raycastTarget = true;
            var button = labels[i].gameObject.AddComponent<Button>(); button.targetGraphic = labels[i];
            UnityEventTools.AddIntPersistentListener(button.onClick, menu.ToggleRecipe, i);
        }
        SetArray(menu, "recipeLabels", labels.Length, (p, i) => p.objectReferenceValue = labels[i]);
        var open = ServiceText(prep.transform, "OpenCantina", new Vector2(0, -175), new Vector2(400, 45));
        open.text = "OPEN CANTINA"; open.raycastTarget = true;
        var openButton = open.gameObject.AddComponent<Button>(); openButton.targetGraphic = open;
        UnityEventTools.AddPersistentListener(openButton.onClick, menu.Open);
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
