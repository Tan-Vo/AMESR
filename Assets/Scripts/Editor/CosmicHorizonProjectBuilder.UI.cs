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
    private static Sprite Kit(string name) => SpriteAt("UI/Kit/" + name + ".png") ?? throw new InvalidOperationException("Import the UI kit sprite " + name + " before building.");
    private static void BuildUIData()
    {
        Ensure<SO_UICommandEvent>("Events/OnUICommandRequested");
        Ensure<SO_WeaponHUDEvent>("Events/OnWeaponHUDChanged");
        Ensure<SO_GameEvent>("Events/OnLoadRequested");
        Ensure<SO_IntEvent>("Events/OnHealRequested");
        Ensure<SO_HealingRequestEvent>("Events/OnHealingRequested");
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
        var button = Frame(null, "Button", Vector2.zero, new Vector2(380, 72), "ui_button");
        var selectable = button.gameObject.AddComponent<Button>(); selectable.targetGraphic = button; ButtonStyle(selectable);
        WireChannels(button.gameObject.AddComponent<UIAudioFeedback>());
        ServiceText(button.transform, "Label", Vector2.zero, new Vector2(340, 64), 28);
        PrefabUtility.SaveAsPrefabAsset(button.gameObject, "Assets/Prefabs/UI/PF_UIButton.prefab");
        var slot = Frame(null, "InventorySlot", Vector2.zero, new Vector2(110, 110), "ui_slot");
        var view = slot.gameObject.AddComponent<InventorySlotUI>();
        Set(view, "border", slot, "icon", UIIcon(slot.transform, "Icon", Vector2.zero, new Vector2(64, 64), null),
            "label", ServiceText(slot.transform, "Quantity", new Vector2(28, -32), new Vector2(50, 34), 26));
        PrefabUtility.SaveAsPrefabAsset(slot.gameObject, "Assets/Prefabs/UI/PF_InventorySlot.prefab");
    }
    private static void ButtonStyle(Button button)
    {
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState { highlightedSprite = Kit("ui_button_hover"), selectedSprite = Kit("ui_button_hover"), pressedSprite = Kit("ui_button_pressed"), disabledSprite = Kit("ui_button_pressed") };
    }
    private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position;
    }
    private static Canvas UICanvas(string name, int order)
    {
        var obj = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = obj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
        var scaler = obj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }
    private static Image Frame(Transform parent, string name, Vector2 position, Vector2 size, string sprite = "ui_panel")
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        if (parent != null) obj.transform.SetParent(parent, false);
        var image = obj.GetComponent<Image>(); image.sprite = Kit(sprite); image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 0.4f;
        int order = name == "Settings" ? 1000 : name == "Pause" ? 900 : name == "DragGhost" ? 850 : name == "Inventory" ? 800 : name == "Workshop" || name == "NightReport" ? 700 : name == "Dialogue" ? 650 : 0;
        if (order > 0) { var canvas = obj.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = order; obj.AddComponent<GraphicRaycaster>(); }
        image.rectTransform.anchoredPosition = position; image.rectTransform.sizeDelta = size;
        return image;
    }
    private static Image UIIcon(Transform parent, string name, Vector2 position, Vector2 size, Sprite sprite)
    {
        var image = Frame(parent, name, position, size, "ui_slot"); image.sprite = sprite; image.type = Image.Type.Simple;
        image.enabled = sprite != null; image.preserveAspect = true; image.raycastTarget = false; return image;
    }
    private static Image BarFill(Transform parent, string name, Vector2 size, Color color, bool vertical = false)
    {
        var fill = Frame(parent, name, Vector2.zero, size, "ui_bar_fill"); fill.type = Image.Type.Filled;
        fill.fillMethod = vertical ? Image.FillMethod.Vertical : Image.FillMethod.Horizontal; fill.fillOrigin = 0;
        fill.color = color; fill.raycastTarget = false; return fill;
    }
    private static Button UIButton(Transform parent, string text, Vector2 position, UnityAction action)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PF_UIButton.prefab"));
        obj.name = text; obj.transform.SetParent(parent, false); obj.GetComponent<RectTransform>().anchoredPosition = position;
        var label = obj.GetComponentInChildren<Text>(true); label.text = text;
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(24, 6); label.rectTransform.offsetMax = new Vector2(-24, -6);
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
        var panel = Frame(parent, "Settings", Vector2.zero, new Vector2(1000, 840)); var settings = panel.gameObject.AddComponent<SettingsUI>(); WireChannels(settings);
        var resources = new DefaultControls.Resources { standard = Kit("ui_button"), background = Kit("ui_panel_small"), inputField = Kit("ui_panel_small"),
            knob = Kit("ui_slider_knob"), checkmark = Kit("ui_toggle_check"), dropdown = Kit("ui_dropdown"), mask = Kit("ui_bar_fill") };
        ServiceText(panel.transform, "Title", new Vector2(0, 320), new Vector2(800, 70), 48).text = "SETTINGS";
        var master = SettingsSlider(panel.transform, resources, "Master Volume", 210); UnityEventTools.AddPersistentListener(master.onValueChanged, settings.Volume);
        var music = SettingsSlider(panel.transform, resources, "Music Volume", 120); UnityEventTools.AddPersistentListener(music.onValueChanged, settings.BgmVolume);
        var effects = SettingsSlider(panel.transform, resources, "Effects Volume", 30); UnityEventTools.AddPersistentListener(effects.onValueChanged, settings.SfxVolume);
        var dropdownObj = DefaultControls.CreateDropdown(resources); dropdownObj.name = "Resolution"; dropdownObj.transform.SetParent(panel.transform, false);
        var dropdownRect = dropdownObj.GetComponent<RectTransform>(); dropdownRect.anchoredPosition = new Vector2(0, -90); dropdownRect.sizeDelta = new Vector2(700, 64);
        var dropdown = dropdownObj.GetComponent<Dropdown>(); dropdown.GetComponent<Image>().sprite = Kit("ui_dropdown"); dropdown.GetComponent<Image>().pixelsPerUnitMultiplier = 0.4f;
        dropdown.transform.Find("Arrow").gameObject.SetActive(false);
        dropdown.captionText.rectTransform.offsetMin = new Vector2(24, 8); dropdown.captionText.rectTransform.offsetMax = new Vector2(-44, -8);
        dropdown.template.sizeDelta = new Vector2(0, 300);
        var item = dropdown.itemText.GetComponentInParent<Toggle>(); item.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 56);
        dropdown.template.Find("Viewport/Content").GetComponent<RectTransform>().sizeDelta = new Vector2(0, 60);
        foreach (var label in dropdown.GetComponentsInChildren<Text>(true)) { label.fontSize = 28; label.color = new Color(0.8f, 0.96f, 1); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        dropdown.itemText.rectTransform.offsetMin = new Vector2(28, 4); dropdown.itemText.rectTransform.offsetMax = new Vector2(-20, -4);
        foreach (var image in dropdown.GetComponentsInChildren<Image>(true)) image.pixelsPerUnitMultiplier = 0.4f;
        UnityEventTools.AddPersistentListener(dropdown.onValueChanged, settings.Resolution);
        var toggleObj = DefaultControls.CreateToggle(resources); toggleObj.name = "Fullscreen"; toggleObj.transform.SetParent(panel.transform, false);
        var toggleRect = toggleObj.GetComponent<RectTransform>(); toggleRect.anchoredPosition = new Vector2(0, -190); toggleRect.sizeDelta = new Vector2(360, 64);
        var toggle = toggleObj.GetComponent<Toggle>(); var box = toggle.transform.Find("Background").GetComponent<Image>();
        box.sprite = Kit("ui_toggle_box"); box.type = Image.Type.Simple; box.preserveAspect = true;
        Anchor(box.rectTransform, new Vector2(0, 0.5f), new Vector2(22, 0)); box.rectTransform.sizeDelta = new Vector2(44, 44);
        var check = toggle.graphic.GetComponent<RectTransform>(); check.sizeDelta = new Vector2(38, 38);
        var toggleLabel = toggle.GetComponentInChildren<Text>(); toggleLabel.text = "Fullscreen"; toggleLabel.fontSize = 28;
        toggleLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); toggleLabel.color = new Color(0.8f, 0.96f, 1); toggleLabel.alignment = TextAnchor.MiddleLeft;
        toggleLabel.rectTransform.offsetMin = new Vector2(62, 0); toggleLabel.rectTransform.offsetMax = Vector2.zero;
        UnityEventTools.AddPersistentListener(toggle.onValueChanged, settings.Fullscreen);
        Set(settings, "volume", master, "bgm", music, "sfx", effects, "resolution", dropdown, "fullscreen", toggle);
        UIButton(panel.transform, "Close", new Vector2(0, -310), settings.Close); panel.gameObject.SetActive(false); return panel.gameObject;
    }
    private static Slider SettingsSlider(Transform parent, DefaultControls.Resources resources, string label, float y)
    {
        ServiceText(parent, label, new Vector2(-280, y), new Vector2(300, 60), 28).text = label;
        var obj = DefaultControls.CreateSlider(resources); obj.name = label + "Slider"; obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(150, y); rect.sizeDelta = new Vector2(480, 56);
        var background = obj.transform.Find("Background").GetComponent<Image>(); background.sprite = Kit("ui_slider_track"); background.pixelsPerUnitMultiplier = 0.4f;
        background.rectTransform.sizeDelta = new Vector2(0, 18);
        var slider = obj.GetComponent<Slider>(); var fill = slider.fillRect.GetComponent<Image>(); fill.sprite = Kit("ui_bar_fill"); fill.color = new Color(0.1f, 0.65f, 0.8f);
        slider.handleRect.sizeDelta = new Vector2(34, 34); slider.handleRect.GetComponent<Image>().preserveAspect = true;
        return slider;
    }
    private static void EnsureBossLootButton(Transform root)
    {
        const string label = "Claim boss loot";
        if (root.Find(label) == null) UIButton(root, label, new Vector2(-700, -240), Ensure<SO_InventoryAccess>("Data/SO_InventoryAccess").ClaimRewards);
    }
    private static void BuildSceneUI(Scene scene)
    {
        var existingUI = scene.GetRootGameObjects().FirstOrDefault(obj => obj.name == "CosmicUI");
        if (existingUI != null) UnityEngine.Object.DestroyImmediate(existingUI);
        var obj = UICanvas("CosmicUI", 300).gameObject; var root = obj.transform;
        if (!scene.GetRootGameObjects().Any(go => go.GetComponent<EventSystem>() != null)) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        if (scene.name == "SC_BossArena")
        {
            var legacy = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "BossHUD"); if (legacy != null) legacy.SetActive(false);
            var arena = scene.GetRootGameObjects().Select(go => go.GetComponent<BossArenaDirector>()).First(value => value != null);
            var panel = Frame(root, "BossHealth", new Vector2(0, 440), new Vector2(1200, 100), "ui_bar_frame");
            var fill = BarFill(panel.transform, "Fill", new Vector2(1140, 58), new Color(0.2f, 0.55f, 0.35f));
            var bar = obj.AddComponent<BossHealthBarUI>();
            var label = ServiceText(panel.transform, "Name", Vector2.zero, new Vector2(1100, 70), 44); label.text = "ABYSSAL SIGNAL";
            Set(bar, "arena", arena, "panel", panel.rectTransform, "fill", fill, "label", label); return;
        }
        foreach (var player in scene.GetRootGameObjects().Where(go => go.GetComponent<PlayerController>() != null))
        {
            var oldWeapon = player.transform.Find("VitalsCanvas/WeaponStatus"); if (oldWeapon != null) oldWeapon.gameObject.SetActive(false);
            var oldControls = player.transform.Find("VitalsCanvas/DiveControls"); if (oldControls != null) oldControls.gameObject.SetActive(false);
        }
        var settings = UISettings(root);
        if (scene.name == "SC_MainMenu")
        {
            ServiceText(root, "Title", new Vector2(0, 300), new Vector2(1600, 120), 76).text = "COSMIC HORIZON";
            var menu = obj.AddComponent<MainMenuUI>(); WireChannels(menu); Set(menu, "settings", settings);
            UIButton(root, "New Game", new Vector2(0, 100), menu.NewGame);
            Set(menu, "continueButton", UIButton(root, "Continue", Vector2.zero, menu.Continue));
            UIButton(root, "Settings", new Vector2(0, -100), menu.Settings); UIButton(root, "Quit", new Vector2(0, -200), menu.Quit);
            settings.transform.SetAsLastSibling(); return;
        }
        if (scene.name == "SC_Mothership")
        {
            var title = ServiceText(root, "HubTitle", Vector2.zero, new Vector2(900, 80), 48); title.text = "MOTHERSHIP";
            Anchor(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -80));
            var commands = new[] { UICommand.Tutorial, UICommand.Shallows, UICommand.Twilight, UICommand.Abyssal, UICommand.EventHorizon, UICommand.Cantina, UICommand.Sleep };
            for (int i = 0; i < commands.Length; i++) Navigation(root, commands[i].ToString(), new Vector2(-700 + i / 4 * 440, 180 - i % 4 * 100), commands[i]);
            BuildShop(root); BuildDialogue(root); EnsureBossLootButton(root);
        }
        else if (scene.name == "SC_NebulaCantina") BuildCantinaUI(scene, root);
        else
        {
            Navigation(root, "Return to hub", Vector2.zero, UICommand.Hub);
            Anchor(root.Find("Return to hub").GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(-230, 210));
        }
        BuildHUD(root); BuildInventory(root, scene.name == "SC_Mothership" || scene.name == "SC_NebulaCantina");
        var pausePanel = Frame(root, "Pause", Vector2.zero, new Vector2(900, 850));
        ServiceText(pausePanel.transform, "Title", new Vector2(0, 320), new Vector2(700, 70), 48).text = "PAUSED";
        var pause = obj.AddComponent<PauseMenuUI>(); WireChannels(pause); Set(pause, "panel", pausePanel.gameObject, "settings", settings);
        UnityAction[] actions = { pause.Resume, pause.Settings, pause.Save, pause.Load, pause.Quit };
        string[] labels = { "Resume", "Settings", "Save", "Load", "Quit to menu" };
        for (int i = 0; i < labels.Length; i++) { var button = UIButton(pausePanel.transform, labels[i], new Vector2(0, 200 - i * 100), actions[i]); if (i == 2) Set(pause, "saveButton", button); }
        pausePanel.gameObject.SetActive(false); settings.transform.SetAsLastSibling();
    }
    private static void BuildHUD(Transform root)
    {
        var hud = root.gameObject.AddComponent<HUDManager>(); WireChannels(hud); Set(hud, "inventory", Ensure<SO_InventoryAccess>("Data/SO_InventoryAccess"));
        SetArray(hud, "biomes", 4, (p, i) => p.objectReferenceValue = BiomeAssets()[i]);
        var depth = Frame(root, "DepthGauge", Vector2.zero, new Vector2(28, 210), "ui_bar_frame"); depth.raycastTarget = false;
        Anchor(depth.rectTransform, new Vector2(1, 1), new Vector2(-36, -210));
        Set(hud, "depthFill", BarFill(depth.transform, "Fill", new Vector2(12, 180), Color.cyan, true));
        for (int i = 0; i < 5; i++)
        {
            var button = UIButton(root, "", Vector2.zero, null); var rect = button.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(88, 88);
            button.GetComponent<Image>().sprite = Kit("ui_slot"); button.transition = Selectable.Transition.ColorTint;
            Anchor(rect, new Vector2(0.5f, 0), new Vector2(-208 + i * 104, 116));
            var label = button.GetComponentInChildren<Text>(); label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0);
            label.rectTransform.sizeDelta = new Vector2(88, 32); label.rectTransform.anchoredPosition = new Vector2(0, 16); label.fontSize = 26;
            var quick = button.gameObject.AddComponent<QuickSlotUI>(); Set(quick, "inventory", Ensure<SO_InventoryAccess>("Data/SO_InventoryAccess"), "index", i,
                "icon", UIIcon(button.transform, "Consumable", new Vector2(0, 12), new Vector2(42, 42), null), "label", label);
            UnityEventTools.AddPersistentListener(button.onClick, quick.Use);
        }
        string[] names = { "health", "oxygen", "fuel" }; string[] text = { "HP 100/100", "O₂ 100%", "FUEL 100%" };
        Color[] colors = { new Color(0.55f, 0.12f, 0.2f), new Color(0.05f, 0.4f, 0.5f), new Color(0.5f, 0.28f, 0.06f) };
        for (int i = 0; i < 3; i++)
        {
            var frame = Frame(root, names[i], Vector2.zero, new Vector2(410, 54), "ui_bar_frame"); frame.raycastTarget = false;
            Anchor(frame.rectTransform, new Vector2(0, 1), new Vector2(250, -80 - i * 66));
            var fill = BarFill(frame.transform, "Fill", new Vector2(374, 28), colors[i]);
            var label = ServiceText(frame.transform, "Label", Vector2.zero, new Vector2(360, 44), 28); label.text = text[i];
            Set(hud, names[i] + "Fill", fill, names[i] + "Label", label);
        }
        var depthChip = Frame(root, "DepthChip", Vector2.zero, new Vector2(420, 120), "ui_panel_small"); depthChip.raycastTarget = false;
        Anchor(depthChip.rectTransform, new Vector2(1, 1), new Vector2(-260, -100));
        var depthLabel = ServiceText(depthChip.transform, "Depth", Vector2.zero, new Vector2(370, 90), 28); depthLabel.text = "SURFACE • DAY 1";
        var creditsChip = Frame(root, "CreditsChip", Vector2.zero, new Vector2(320, 84), "ui_panel_small"); creditsChip.raycastTarget = false;
        Anchor(creditsChip.rectTransform, new Vector2(1, 0), new Vector2(-200, 100));
        var credits = ServiceText(creditsChip.transform, "Credits", Vector2.zero, new Vector2(280, 60), 30); credits.text = "₡ 0";
        var weaponChip = Frame(root, "WeaponChip", Vector2.zero, new Vector2(470, 90), "ui_panel_small"); weaponChip.raycastTarget = false;
        Anchor(weaponChip.rectTransform, new Vector2(0, 0), new Vector2(280, 100));
        var weapon = ServiceText(weaponChip.transform, "Weapon", new Vector2(40, 0), new Vector2(330, 68), 26); weapon.text = "GRAVITY HARPOON";
        var hint = ServiceText(root, "QuickSlots", Vector2.zero, new Vector2(1600, 44), 26); hint.text = "I: cargo • F1–F5: consumables • Esc: pause";
        Anchor(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 34));
        Set(hud, "depthLabel", depthLabel, "creditsLabel", credits, "weaponLabel", weapon,
            "weaponIcon", UIIcon(weaponChip.transform, "WeaponIcon", new Vector2(-180, 0), new Vector2(54, 54), Ensure<SO_WeaponData>("Data/SO_BasicHarpoon").Icon), "quickLabel", hint);
    }
    private static void BuildInventory(Transform root, bool sell)
    {
        var ui = root.gameObject.AddComponent<InventoryUI>(); var panel = Frame(root, "Inventory", Vector2.zero, new Vector2(1100, 850));
        ServiceText(panel.transform, "Title", new Vector2(0, 330), new Vector2(900, 70), 48).text = "CARGO";
        var slots = new InventorySlotUI[20]; var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PF_InventorySlot.prefab");
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = (GameObject)PrefabUtility.InstantiatePrefab(prefab); slot.transform.SetParent(panel.transform, false);
            slot.GetComponent<RectTransform>().anchoredPosition = new Vector2(-320 + i % 5 * 160, 200 - i / 5 * 120);
            slots[i] = slot.GetComponent<InventorySlotUI>(); Set(slots[i], "owner", ui, "index", i);
        }
        var context = Frame(panel.transform, "Context", new Vector2(330, -230), new Vector2(300, 270), "ui_panel_small");
        UnityAction[] actions = { ui.Use, ui.Sell, ui.Info }; string[] labels = { "Use", "Sell", "Info" };
        for (int i = 0; i < 3; i++) UIButton(context.transform, labels[i], new Vector2(0, 80 - i * 80), actions[i]).GetComponent<RectTransform>().sizeDelta = new Vector2(240, 64);
        var ghost = UIIcon(root, "DragGhost", Vector2.zero, new Vector2(64, 64), null);
        Set(ui, "inventory", Ensure<SO_InventoryAccess>("Data/SO_InventoryAccess"), "panel", panel.gameObject, "context", context.gameObject, "dragIcon", ghost, "allowSelling", sell,
            "tooltip", ServiceText(panel.transform, "Tooltip", new Vector2(-100, -300), new Vector2(750, 110), 26));
        SetArray(ui, "slots", slots.Length, (p, i) => p.objectReferenceValue = slots[i]); context.gameObject.SetActive(false); panel.gameObject.SetActive(false);
    }
    private static void BuildShop(Transform root)
    {
        var panel = Frame(root, "Workshop", Vector2.zero, new Vector2(1560, 850));
        ServiceText(panel.transform, "Title", new Vector2(0, 330), new Vector2(1300, 70), 48).text = "WORKSHOP • UPGRADES";
        var shop = panel.gameObject.AddComponent<ShopSystem>(); WireChannels(shop); var ui = panel.gameObject.AddComponent<ShopUI>(); WireChannels(ui);
        var upgrades = Enum.GetValues(typeof(UpgradeCategory)).Cast<UpgradeCategory>().Select(category => Ensure<SO_UpgradeData>("Data/SO_" + category)).ToArray();
        SetArray(shop, "availableUpgrades", upgrades.Length, (p, i) => p.objectReferenceValue = upgrades[i]);
        SetArray(ui, "upgrades", upgrades.Length, (p, i) => p.objectReferenceValue = upgrades[i]); Set(ui, "shop", shop);
        var buttons = new Button[upgrades.Length]; var cards = new Text[upgrades.Length];
        for (int i = 0; i < upgrades.Length; i++)
        {
            var button = UIButton(panel.transform, "Upgrade", new Vector2(-540 + i % 4 * 360, 150 - i / 4 * 240), null);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 210);
            var label = button.GetComponentInChildren<Text>(); label.rectTransform.offsetMin = new Vector2(16, 10); label.rectTransform.offsetMax = new Vector2(-16, -76); label.fontSize = 26;
            UIIcon(button.transform, "UpgradeIcon", new Vector2(0, 62), new Vector2(52, 52), upgrades[i].Icon);
            UnityEventTools.AddIntPersistentListener(button.onClick, ui.Purchase, i); buttons[i] = button; cards[i] = label;
        }
        SetArray(ui, "cards", cards.Length, (p, i) => p.objectReferenceValue = cards[i]); SetArray(ui, "buttons", buttons.Length, (p, i) => p.objectReferenceValue = buttons[i]);
        var close = UIButton(panel.transform, "Close", new Vector2(0, -320), null); UnityEventTools.AddBoolPersistentListener(close.onClick, panel.gameObject.SetActive, false);
        var open = UIButton(root, "Open workshop", new Vector2(-700, -340), null); UnityEventTools.AddBoolPersistentListener(open.onClick, panel.gameObject.SetActive, true);
        panel.gameObject.SetActive(false);
    }
    private static void BuildDialogue(Transform root)
    {
        var dialogue = root.gameObject.AddComponent<DialogueSystem>(); WireChannels(dialogue);
        var panel = Frame(root, "Dialogue", new Vector2(0, -300), new Vector2(1500, 340));
        var conversation = Ensure<SO_DialogueLine>("Data/SO_Dialogue_tan");
        Set(dialogue, "panel", panel.gameObject, "portrait", UIIcon(panel.transform, "Portrait", new Vector2(-590, 0), new Vector2(200, 230), conversation.Portrait),
            "speaker", ServiceText(panel.transform, "Speaker", new Vector2(110, 90), new Vector2(1000, 60), 44),
            "body", ServiceText(panel.transform, "Text", new Vector2(110, -20), new Vector2(1000, 140), 28), "conversation", conversation);
        panel.gameObject.SetActive(false); UIButton(root, "Talk to crew", new Vector2(-260, -240), dialogue.Begin);
    }
    private static void BuildCantinaUI(Scene scene, Transform root)
    {
        var manager = scene.GetRootGameObjects().Select(go => go.GetComponent<CantinaManager>()).First(value => value != null);
        var hud = root.gameObject.AddComponent<CantinaHUD>(); WireChannels(hud);
        var summary = ServiceText(root, "CantinaHUD", Vector2.zero, new Vector2(950, 90), 28); summary.text = "NEBULA CANTINA • PREPARATION";
        Anchor(summary.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -70));
        Set(hud, "cantina", manager, "stock", Ensure<SO_IngredientStock>("Data/SO_IngredientStock"), "summary", summary);
        var report = root.gameObject.AddComponent<NightReportUI>(); WireChannels(report);
        var panel = Frame(root, "NightReport", Vector2.zero, new Vector2(1100, 850));
        ServiceText(panel.transform, "Title", new Vector2(0, 330), new Vector2(900, 70), 48).text = "NIGHT REPORT";
        Set(report, "cantina", manager, "panel", panel.gameObject, "summary", ServiceText(panel.transform, "Summary", Vector2.zero, new Vector2(900, 520), 30));
        UIButton(panel.transform, "Continue to workshop", new Vector2(0, -320), report.Continue); panel.gameObject.SetActive(false);
    }
}
