using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class CosmicHorizonProjectBuilder
{
    private static Sprite UIFrame => SpriteAt("UI/hud_control_button_slider_01.png");
    private static void BuildUIData()
    {
        Ensure<SO_UICommandEvent>("Events/OnUICommandRequested");
        Ensure<SO_WeaponHUDEvent>("Events/OnWeaponHUDChanged");
        Ensure<SO_GameEvent>("Events/OnLoadRequested");
        Ensure<SO_IntEvent>("Events/OnHealRequested");
        Ensure<SO_IntEvent>("Events/OnDialogueCompleted");
        Ensure<SO_FloatEvent>("Events/OnPhaseTimeChanged");
        var inventory = Ensure<SO_InventoryAccess>("Data/SO_InventoryAccess"); WireChannels(inventory);
        var items = AssetDatabase.FindAssets("t:SO_ItemData", new[] { Root + "/Data" }).Select(guid => AssetDatabase.LoadAssetAtPath<SO_ItemData>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
        SetArray(inventory, "catalog", items.Length, (p, i) => p.objectReferenceValue = items[i]);
        string[] people = { "tan", "bancho", "lyra", "jax", "fizz" };
        foreach (string person in people)
        {
            var dialogue = Ensure<SO_DialogueLine>("Data/SO_Dialogue_" + person);
            var portrait = AssetDatabase.FindAssets("portrait_" + person + "_ t:Sprite", new[] { "Assets/Sprites/Characters" }).OrderBy(id => id).FirstOrDefault();
            Set(dialogue, "speakerName", person.ToUpperInvariant(), "portrait", portrait == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(portrait)));
            SetArray(dialogue, "lines", 2, (p, i) => p.stringValue = i == 0 ? "Welcome aboard Cosmic Horizon. The ocean below is full of discoveries." : "Catch ingredients, return safely, and serve the Nebula Cantina. Visit the shop before your next dive.");
        }
    }
    private static void BuildUIWidgets()
    {
        Folder("Assets/Prefabs/UI");
        var button = Frame(null, "Button", Vector2.zero, new Vector2(250, 52)); button.gameObject.AddComponent<Button>().targetGraphic = button;
        WireChannels(button.gameObject.AddComponent<UIAudioFeedback>());
        ServiceText(button.transform, "Label", Vector2.zero, new Vector2(240, 48), 20);
        PrefabUtility.SaveAsPrefabAsset(button.gameObject, "Assets/Prefabs/UI/PF_UIButton.prefab");
        var slot = Frame(null, "InventorySlot", Vector2.zero, new Vector2(84, 84)); var view = slot.gameObject.AddComponent<InventorySlotUI>();
        Set(view, "border", slot, "icon", UIIcon(slot.transform, "Icon", Vector2.zero, new Vector2(58, 58), null),
            "label", ServiceText(slot.transform, "Quantity", new Vector2(20, -25), new Vector2(40, 24), 16));
        PrefabUtility.SaveAsPrefabAsset(slot.gameObject, "Assets/Prefabs/UI/PF_InventorySlot.prefab");
    }
    private static Image Frame(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        if (parent != null) obj.transform.SetParent(parent, false);
        var image = obj.GetComponent<Image>(); image.sprite = UIFrame; image.type = Image.Type.Sliced;
        image.rectTransform.anchoredPosition = position; image.rectTransform.sizeDelta = size;
        return image;
    }
    private static Image UIIcon(Transform parent, string name, Vector2 position, Vector2 size, Sprite sprite)
    {
        var image = Frame(parent, name, position, size); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false; return image;
    }
    private static Button UIButton(Transform parent, string text, Vector2 position, UnityAction action)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PF_UIButton.prefab"));
        obj.name = text; obj.transform.SetParent(parent, false); obj.GetComponent<RectTransform>().anchoredPosition = position;
        obj.GetComponentInChildren<Text>().text = text;
        var button = obj.GetComponent<Button>(); if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action); return button;
    }
    private static void Navigation(Transform parent, string text, Vector2 position, UICommand command)
    {
        float depth = command == UICommand.Twilight ? 500 : command == UICommand.Abyssal ? 1500 : command == UICommand.EventHorizon ? 2500 : 0;
        if (depth > 500) text += " ≥" + depth + "m";
        var button = UIButton(parent, text, position, null); var endpoint = button.gameObject.AddComponent<UICommandButton>(); WireChannels(endpoint);
        Set(endpoint, "command", (int)command, "gear", Ensure<SO_GearProfile>("Data/SO_GearProfile"), "requiredDepth", depth); UnityEventTools.AddPersistentListener(button.onClick, endpoint.Click);
    }
    private static GameObject UISettings(Transform parent)
    {
        var panel = Frame(parent, "Settings", Vector2.zero, new Vector2(850, 650)); var settings = panel.gameObject.AddComponent<SettingsUI>();
        WireChannels(settings);
        var resources = new DefaultControls.Resources { standard = UIFrame, background = UIFrame, inputField = UIFrame, knob = UIFrame, checkmark = UIFrame, dropdown = UIFrame, mask = UIFrame };
        ServiceText(panel.transform, "Title", new Vector2(0, 260), new Vector2(700, 50), 30).text = "SETTINGS";
        ServiceText(panel.transform, "VolumeLabel", new Vector2(-240, 160), new Vector2(200, 40)).text = "Master Volume";
        var sliderObj = DefaultControls.CreateSlider(resources); sliderObj.transform.SetParent(panel.transform, false);
        sliderObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, 160);
        var slider = sliderObj.GetComponent<Slider>(); UnityEventTools.AddPersistentListener(slider.onValueChanged, settings.Volume);
        var musicObj = DefaultControls.CreateSlider(resources); musicObj.transform.SetParent(panel.transform, false);
        musicObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, 95);
        var music = musicObj.GetComponent<Slider>(); UnityEventTools.AddPersistentListener(music.onValueChanged, settings.BgmVolume);
        ServiceText(panel.transform, "MusicLabel", new Vector2(-240, 95), new Vector2(200, 40)).text = "Music Volume";
        var effectsObj = DefaultControls.CreateSlider(resources); effectsObj.transform.SetParent(panel.transform, false);
        effectsObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, 30);
        var effects = effectsObj.GetComponent<Slider>(); UnityEventTools.AddPersistentListener(effects.onValueChanged, settings.SfxVolume);
        ServiceText(panel.transform, "EffectsLabel", new Vector2(-240, 30), new Vector2(200, 40)).text = "Effects Volume";
        var dropdownObj = DefaultControls.CreateDropdown(resources); dropdownObj.transform.SetParent(panel.transform, false);
        dropdownObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -70); dropdownObj.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 45);
        var dropdown = dropdownObj.GetComponent<Dropdown>(); UnityEventTools.AddPersistentListener(dropdown.onValueChanged, settings.Resolution);
        var toggleObj = DefaultControls.CreateToggle(resources); toggleObj.transform.SetParent(panel.transform, false);
        toggleObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);
        toggleObj.GetComponentInChildren<Text>().text = "Fullscreen"; var toggle = toggleObj.GetComponent<Toggle>();
        UnityEventTools.AddPersistentListener(toggle.onValueChanged, settings.Fullscreen);
        Set(settings, "volume", slider, "bgm", music, "sfx", effects, "resolution", dropdown, "fullscreen", toggle);
        UIButton(panel.transform, "Close", new Vector2(0, -220), settings.Close); panel.gameObject.SetActive(false); return panel.gameObject;
    }
    private static void BuildSceneUI(Scene scene)
    {
        if (scene.GetRootGameObjects().Any(obj => obj.name == "CosmicUI")) return;
        var obj = new GameObject("CosmicUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        obj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; obj.GetComponent<Canvas>().sortingOrder = 300;
        obj.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; obj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        var root = obj.transform;
        if (scene.name == "SC_BossArena")
        {
            var legacy = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "BossHUD");
            if (legacy != null) legacy.SetActive(false);
            var arena = scene.GetRootGameObjects().Select(go => go.GetComponent<BossArenaDirector>()).First(value => value != null);
            var panel = Frame(root, "BossHealth", new Vector2(0, 440), new Vector2(1000, 70));
            var bossFrame = AssetDatabase.FindAssets("boss_health_bar_hud t:Sprite").FirstOrDefault();
            if (bossFrame != null) panel.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(bossFrame));
            var fill = Frame(panel.transform, "Fill", Vector2.zero, new Vector2(940, 30)); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            var bar = obj.AddComponent<BossHealthBarUI>();
            Set(bar, "arena", arena, "panel", panel.rectTransform, "fill", fill, "label", ServiceText(panel.transform, "Name", Vector2.zero, new Vector2(900, 60), 24));
            return;
        }
        if (!scene.GetRootGameObjects().Any(go => go.GetComponent<EventSystem>() != null)) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        foreach (var player in scene.GetRootGameObjects().Where(go => go.GetComponent<PlayerController>() != null))
        {
            var oldWeapon = player.transform.Find("VitalsCanvas/WeaponStatus"); if (oldWeapon != null) oldWeapon.gameObject.SetActive(false);
            var oldControls = player.transform.Find("VitalsCanvas/DiveControls"); if (oldControls != null) oldControls.gameObject.SetActive(false);
        }
        var settings = UISettings(root);
        if (scene.name == "SC_MainMenu")
        {
            ServiceText(root, "Title", new Vector2(0, 300), new Vector2(1400, 120), 70).text = "COSMIC HORIZON";
            var menu = obj.AddComponent<MainMenuUI>(); WireChannels(menu); Set(menu, "settings", settings);
            UIButton(root, "New Game", new Vector2(0, 100), menu.NewGame);
            Set(menu, "continueButton", UIButton(root, "Continue", new Vector2(0, 25), menu.Continue));
            UIButton(root, "Settings", new Vector2(0, -50), menu.Settings); UIButton(root, "Quit", new Vector2(0, -125), menu.Quit);
            settings.transform.SetAsLastSibling(); return;
        }
        if (scene.name == "SC_Mothership")
        {
            ServiceText(root, "HubTitle", new Vector2(0, 440), new Vector2(1400, 80), 42).text = "MOTHERSHIP • DIVE POD / CANTINA / WORKSHOP";
            var commands = new[] { UICommand.Tutorial, UICommand.Shallows, UICommand.Twilight, UICommand.Abyssal, UICommand.EventHorizon, UICommand.Cantina, UICommand.Sleep };
            for (int i = 0; i < commands.Length; i++) Navigation(root, commands[i].ToString(), new Vector2(-760, 300 - i * 75), commands[i]);
            BuildShop(root); BuildDialogue(root);
        }
        else if (scene.name == "SC_NebulaCantina") BuildCantinaUI(scene, root);
        else Navigation(root, "Return to hub", new Vector2(760, -450), UICommand.Hub);
        BuildHUD(root);
        BuildInventory(root, scene.name == "SC_Mothership" || scene.name == "SC_NebulaCantina");
        var pausePanel = Frame(root, "Pause", Vector2.zero, new Vector2(1920, 1080)); pausePanel.color = new Color(0.08f, 0.12f, 0.2f, 0.94f);
        var pause = obj.AddComponent<PauseMenuUI>(); WireChannels(pause);
        Set(pause, "panel", pausePanel.gameObject, "settings", settings);
        UnityAction[] actions = { pause.Resume, pause.Settings, pause.Save, pause.Load, pause.Quit };
        string[] labels = { "Resume", "Settings", "Save", "Load", "Quit to menu" };
        for (int i = 0; i < labels.Length; i++) UIButton(pausePanel.transform, labels[i], new Vector2(0, 200 - i * 85), actions[i]);
        pausePanel.gameObject.SetActive(false); settings.transform.SetAsLastSibling();
    }
    private static void BuildHUD(Transform root)
    {
        var hud = root.gameObject.AddComponent<HUDManager>(); WireChannels(hud); Set(hud, "inventory", Ensure<SO_InventoryAccess>("Data/SO_InventoryAccess"));
        SetArray(hud, "biomes", 4, (p, i) => p.objectReferenceValue = BiomeAssets()[i]);
        var depthGauge = Frame(root, "DepthGauge", new Vector2(920, 330), new Vector2(22, 230)); depthGauge.raycastTarget = false;
        depthGauge.type = Image.Type.Filled; depthGauge.fillMethod = Image.FillMethod.Vertical; Set(hud, "depthFill", depthGauge);
        for (int i = 0; i < 5; i++)
        {
            var button = UIButton(root, "", new Vector2(-180 + i * 90, -475), null); button.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 64);
            var quick = button.gameObject.AddComponent<QuickSlotUI>();
            Set(quick, "inventory", Ensure<SO_InventoryAccess>("Data/SO_InventoryAccess"), "index", i,
                "icon", UIIcon(button.transform, "Consumable", new Vector2(0, 8), new Vector2(40, 40), null),
                "label", button.GetComponentInChildren<Text>());
            button.GetComponentInChildren<Text>().rectTransform.anchoredPosition = new Vector2(0, -20);
            UnityEventTools.AddPersistentListener(button.onClick, quick.Use);
        }
        string[] names = { "health", "oxygen", "fuel" };
        Color[] colors = { Color.red, Color.cyan, new Color(1f, 0.6f, 0.1f) };
        for (int i = 0; i < 3; i++)
        {
            var frame = Frame(root, names[i], new Vector2(-720, 460 - i * 55), new Vector2(320, 44)); frame.raycastTarget = false;
            var fill = Frame(frame.transform, "Fill", Vector2.zero, new Vector2(300, 30)); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.color = colors[i]; fill.raycastTarget = false;
            Set(hud, names[i] + "Fill", fill, names[i] + "Label", ServiceText(frame.transform, "Label", Vector2.zero, new Vector2(290, 38), 18));
        }
        UIIcon(root, "OxygenIcon", new Vector2(-915, 405), new Vector2(42, 42), SpriteAt("UI_Icons/hud_oxygen_tank.png"));
        UIIcon(root, "FuelIcon", new Vector2(-915, 350), new Vector2(42, 42), SpriteAt("UI_Icons/hud_jet_thruster_battery.png"));
        UIIcon(root, "CreditsIcon", new Vector2(680, -420), new Vector2(42, 42), SpriteAt("UI_Icons/hud_gold_coin.png"));
        Set(hud, "depthLabel", ServiceText(root, "Depth", new Vector2(700, 430), new Vector2(450, 110), 23),
            "creditsLabel", ServiceText(root, "Credits", new Vector2(790, -420), new Vector2(250, 50), 24),
            "weaponLabel", ServiceText(root, "Weapon", new Vector2(-680, -420), new Vector2(430, 60), 22),
            "weaponIcon", UIIcon(root, "WeaponIcon", new Vector2(-915, -420), new Vector2(64, 64), null),
            "quickLabel", ServiceText(root, "QuickSlots", new Vector2(0, -510), new Vector2(1800, 55), 15));
    }
    private static void BuildInventory(Transform root, bool sell)
    {
        var ui = root.gameObject.AddComponent<InventoryUI>();
        var panel = Frame(root, "Inventory", Vector2.zero, new Vector2(950, 700));
        ServiceText(panel.transform, "Title", new Vector2(0, 300), new Vector2(800, 50), 26).text = "CARGO • drag to stack/swap • right-click actions";
        var slots = new InventorySlotUI[20];
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PF_InventorySlot.prefab");
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = (GameObject)PrefabUtility.InstantiatePrefab(prefab); slot.transform.SetParent(panel.transform, false);
            slot.GetComponent<RectTransform>().anchoredPosition = new Vector2(-300 + i % 5 * 150, 200 - i / 5 * 110);
            slots[i] = slot.GetComponent<InventorySlotUI>(); Set(slots[i], "owner", ui, "index", i);
        }
        var context = Frame(panel.transform, "Context", new Vector2(320, -230), new Vector2(230, 200));
        UIButton(context.transform, "Use", new Vector2(0, 60), ui.Use); UIButton(context.transform, "Sell", Vector2.zero, ui.Sell); UIButton(context.transform, "Info", new Vector2(0, -60), ui.Info);
        var ghost = UIIcon(root, "DragGhost", Vector2.zero, new Vector2(64, 64), null); ghost.enabled = false;
        Set(ui, "inventory", Ensure<SO_InventoryAccess>("Data/SO_InventoryAccess"), "panel", panel.gameObject, "context", context.gameObject, "dragIcon", ghost, "allowSelling", sell,
            "tooltip", ServiceText(panel.transform, "Tooltip", new Vector2(-120, -230), new Vector2(560, 130), 18));
        SetArray(ui, "slots", slots.Length, (p, i) => p.objectReferenceValue = slots[i]); context.gameObject.SetActive(false); panel.gameObject.SetActive(false);
    }
    private static void BuildShop(Transform root)
    {
        var panel = Frame(root, "Workshop", new Vector2(200, 0), new Vector2(1250, 700));
        var shop = panel.gameObject.AddComponent<ShopSystem>(); WireChannels(shop);
        var ui = panel.gameObject.AddComponent<ShopUI>(); WireChannels(ui);
        var upgrades = Enum.GetValues(typeof(UpgradeCategory)).Cast<UpgradeCategory>().Select(category => Ensure<SO_UpgradeData>("Data/SO_" + category)).ToArray();
        SetArray(shop, "availableUpgrades", upgrades.Length, (p, i) => p.objectReferenceValue = upgrades[i]);
        SetArray(ui, "upgrades", upgrades.Length, (p, i) => p.objectReferenceValue = upgrades[i]); Set(ui, "shop", shop);
        var buttons = new Button[upgrades.Length]; var cards = new Text[upgrades.Length];
        for (int i = 0; i < upgrades.Length; i++)
        {
            var button = UIButton(panel.transform, "Upgrade", new Vector2(-450 + i % 4 * 300, 190 - i / 4 * 300), null);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 240);
            var label = button.GetComponentInChildren<Text>(); label.rectTransform.sizeDelta = new Vector2(270, 140); label.rectTransform.anchoredPosition = new Vector2(0, -35); label.fontSize = 18;
            UIIcon(button.transform, "UpgradeIcon", new Vector2(0, 70), new Vector2(70, 70), upgrades[i].Icon);
            UnityEventTools.AddIntPersistentListener(button.onClick, ui.Purchase, i); buttons[i] = button; cards[i] = label;
        }
        SetArray(ui, "cards", cards.Length, (p, i) => p.objectReferenceValue = cards[i]); SetArray(ui, "buttons", buttons.Length, (p, i) => p.objectReferenceValue = buttons[i]);
    }
    private static void BuildDialogue(Transform root)
    {
        var dialogue = root.gameObject.AddComponent<DialogueSystem>(); WireChannels(dialogue);
        var panel = Frame(root, "Dialogue", new Vector2(0, -340), new Vector2(1500, 300));
        Set(dialogue, "panel", panel.gameObject, "portrait", UIIcon(panel.transform, "Portrait", new Vector2(-610, 0), new Vector2(230, 260), null),
            "speaker", ServiceText(panel.transform, "Speaker", new Vector2(50, 100), new Vector2(1000, 45), 26),
            "body", ServiceText(panel.transform, "Text", new Vector2(80, -10), new Vector2(1050, 160), 24),
            "conversation", Ensure<SO_DialogueLine>("Data/SO_Dialogue_tan"));
        panel.gameObject.SetActive(false);
        UIButton(root, "Talk to crew", new Vector2(-760, -260), dialogue.Begin);
    }
    private static void BuildCantinaUI(Scene scene, Transform root)
    {
        var manager = scene.GetRootGameObjects().Select(go => go.GetComponent<CantinaManager>()).First(value => value != null);
        var hud = root.gameObject.AddComponent<CantinaHUD>(); WireChannels(hud);
        Set(hud, "cantina", manager, "stock", Ensure<SO_IngredientStock>("Data/SO_IngredientStock"), "summary", ServiceText(root, "CantinaHUD", new Vector2(0, 420), new Vector2(1100, 120), 23));
        var report = root.gameObject.AddComponent<NightReportUI>(); WireChannels(report);
        var panel = Frame(root, "NightReport", Vector2.zero, new Vector2(1000, 800));
        Set(report, "cantina", manager, "panel", panel.gameObject, "summary", ServiceText(panel.transform, "Summary", new Vector2(0, 50), new Vector2(900, 600), 30));
        UIButton(panel.transform, "Continue to workshop", new Vector2(0, -320), report.Continue); panel.gameObject.SetActive(false);
    }
}
