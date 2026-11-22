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
    private static Sprite UIFrame => ArtSprite("ui_panel");
    private static Sprite UIKey => ArtSprite("ui_button");
    private static Sprite UIFill => ArtSprite("ui_fill");
    private static Font UIFont(bool display = false)
        => AssetDatabase.LoadAssetAtPath<Font>("Assets/Sprites/Horizon/" + (display ? "BarlowCondensed-SemiBold.ttf" : "Barlow-Medium.ttf"));
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
            Set(dialogue, "speakerName", person.ToUpperInvariant(), "portrait", ArtSprite("crew_" + person));
            SetArray(dialogue, "lines", 2, (p, i) => p.stringValue = i == 0
                ? "Welcome aboard Cosmic Horizon. The Pelagic Moon is our first stop."
                : "Bring specimens back to Nebula Cantina. Upgrade your suit before surveying the outer destinations.");
        }
    }
    private static void BuildUIWidgets()
    {
        Folder("Assets/Prefabs/UI");
        var button = Frame(null, "Button", Vector2.zero, new Vector2(250, 52)); button.gameObject.AddComponent<Button>().targetGraphic = button;
        button.sprite = UIKey;
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
        var image = Frame(parent, name, position, size); image.sprite = sprite; image.type = Image.Type.Simple; image.preserveAspect = true; image.raycastTarget = false; return image;
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

    private static void StyleSceneUI(Scene scene)
    {
        var root = scene.GetRootGameObjects().First(go => go.name == "CosmicUI").transform;
        root.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        foreach (var label in root.GetComponentsInChildren<Text>(true))
        {
            label.font = UIFont(label.name == "Title" || label.name == "HubTitle" || label.fontSize >= 30);
            label.color = new Color(0.91f, 0.97f, 1f);
        }
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.type == Image.Type.Filled)
                image.sprite = UIFill;
            else if (image.type == Image.Type.Sliced && !image.preserveAspect)
                image.sprite = image.GetComponent<Button>() != null ? UIKey : UIFrame;
        }
        if (scene.name == "SC_MainMenu") StyleTitleScreen(root);
        if (scene.name == "SC_Mothership") StyleMissionBoard(root);
        if (root.GetComponent<HUDManager>() is HUDManager hud)
        {
            int destination = DestinationIndex(scene.name);
            bool expedition = destination >= 0 || scene.name == "SC_Tutorial";
            string name = destination >= 0 ? DestinationNames[destination] : scene.name switch
            {
                "SC_Tutorial" => "Training Dock",
                "SC_Mothership" => "Mothership",
                _ => "Nebula Cantina"
            };
            Set(hud, "locationName", name, "showRange", expedition, "showDepth", destination <= 0);
            StyleExpeditionHUD(root, expedition);
        }
        if (scene.name == "SC_BossArena") StyleBossHealth(root);
        else if (scene.name != "SC_MainMenu") StyleSceneVignette(root);
        foreach (var component in root.GetComponentsInChildren<Component>(true))
            if (component is Graphic or Button or RectTransform)
                RecordUIOverride(component);
    }

    private static void StyleSceneVignette(Transform root)
    {
        var vignette = PresentationImage(root, "SceneVignette", Vector2.zero, Vector2.zero);
        vignette.sprite = ArtSprite("ui_vignette");
        vignette.color = new Color(0.015f, 0.025f, 0.055f);
        var rect = vignette.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        vignette.transform.SetAsFirstSibling();
    }

    private static void StyleExpeditionHUD(Transform root, bool expedition)
    {
        var vitals = PresentationImage(root, "SuitReadout", new Vector2(-746, 438), new Vector2(332, 140));
        vitals.color = Ink;
        vitals.transform.SetAsFirstSibling();
        vitals.gameObject.SetActive(expedition);
        string[] bars = { "health", "oxygen", "fuel" };
        Color[] colors = { new Color(0.91f, 0.50f, 0.45f), HUDManager.OxygenColor, Accent };
        for (int i = 0; i < bars.Length; i++)
        {
            var track = root.Find(bars[i])?.GetComponent<Image>();
            if (track == null) continue;
            track.gameObject.SetActive(expedition);
            track.sprite = null;
            track.type = Image.Type.Simple;
            track.color = new Color(0.18f, 0.24f, 0.32f);
            track.rectTransform.anchoredPosition = new Vector2(-746, 463 - i * 40);
            track.rectTransform.sizeDelta = new Vector2(288, 4);
            var fill = track.transform.Find("Fill").GetComponent<Image>();
            fill.rectTransform.sizeDelta = track.rectTransform.sizeDelta;
            fill.color = colors[i];
            var label = track.transform.Find("Label").GetComponent<Text>();
            PositionLabel(label, new Vector2(0, 14), new Vector2(288, 21), 18, TextAnchor.MiddleLeft);
            label.color = Paper;
        }
        foreach (string name in new[] { "OxygenIcon", "FuelIcon", "CreditsIcon" })
            root.Find(name)?.gameObject.SetActive(false);
        var hints = root.Find("QuickSlots")?.GetComponent<Text>();
        if (hints != null)
        {
            PositionLabel(hints, new Vector2(-687, -506), new Vector2(450, 26), 18, TextAnchor.MiddleLeft);
            hints.color = Muted;
        }

        var location = PresentationImage(root, "LocationReadout", new Vector2(724, 457), new Vector2(380, 104));
        location.color = Ink;
        location.transform.SetAsFirstSibling();
        var depth = root.Find("Depth")?.GetComponent<Text>();
        if (depth != null)
        {
            depth.font = UIFont(true);
            PositionLabel(depth, new Vector2(724, 461), new Vector2(332, 91), 30, TextAnchor.MiddleRight);
        }
        var gauge = root.Find("DepthGauge")?.GetComponent<Image>();
        if (gauge != null)
        {
            gauge.fillMethod = Image.FillMethod.Horizontal;
            gauge.rectTransform.anchoredPosition = new Vector2(724, 411);
            gauge.rectTransform.sizeDelta = new Vector2(378, 3);
            gauge.gameObject.SetActive(expedition);
        }
        var credits = root.Find("Credits")?.GetComponent<Text>();
        if (credits != null)
        {
            var position = new Vector2(724, 373);
            var plate = PresentationImage(root, "CreditReadout", position, new Vector2(380, 48));
            plate.color = Ink;
            plate.transform.SetAsFirstSibling();
            PositionLabel(credits, position, new Vector2(332, 45), 24, TextAnchor.MiddleRight);
            credits.color = Accent;
        }
        var weapon = root.Find("Weapon")?.GetComponent<Text>();
        if (weapon != null)
        {
            weapon.gameObject.SetActive(expedition);
            PositionLabel(weapon, new Vector2(-680, -457), new Vector2(330, 42), 22, TextAnchor.MiddleLeft);
        }
        var weaponIcon = root.Find("WeaponIcon")?.GetComponent<RectTransform>();
        if (weaponIcon != null)
        {
            weaponIcon.gameObject.SetActive(expedition);
            weaponIcon.anchoredPosition = new Vector2(-884, -457);
            weaponIcon.sizeDelta = new Vector2(42, 42);
        }
        var slots = root.GetComponentsInChildren<QuickSlotUI>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            slot.gameObject.SetActive(expedition);
            var button = slot.GetComponent<Button>();
            StyleActionButton(button, new Vector2(-136 + 68 * i, -473), new Vector2(60, 60));
            PositionLabel(button.GetComponentInChildren<Text>(), new Vector2(0, -17), new Vector2(60, 20), 15);
            slot.transform.Find("Consumable").GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);
        }
        var back = root.Find("Return to hub")?.GetComponent<Button>();
        if (back != null) StyleActionButton(back, new Vector2(790, -473), new Vector2(248, 54));
    }

    private static void StyleBossHealth(Transform root)
    {
        var plate = root.Find("BossHealth")?.GetComponent<Image>();
        if (plate == null) return;
        plate.sprite = null;
        plate.type = Image.Type.Simple;
        plate.color = Ink;
        plate.raycastTarget = false;
        plate.rectTransform.anchoredPosition = new Vector2(0, 470);
        plate.rectTransform.sizeDelta = new Vector2(678, 64);
        Set(root.GetComponent<BossHealthBarUI>(), "displayedY", 470f);
        var fill = plate.transform.Find("Fill").GetComponent<Image>();
        fill.rectTransform.anchoredPosition = new Vector2(0, -21);
        fill.rectTransform.sizeDelta = new Vector2(642, 5);
        var label = plate.transform.Find("Name").GetComponent<Text>();
        PositionLabel(label, new Vector2(0, 7), new Vector2(642, 43), 26);
    }

    private static void PositionLabel(Text label, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        label.rectTransform.anchoredPosition = position;
        label.rectTransform.sizeDelta = size;
        label.fontSize = fontSize;
        label.alignment = alignment;
    }

    private static void StyleTitleScreen(Transform root)
    {
        var title = root.Find("Title")?.GetComponent<Text>();
        if (title == null) return;
        title.text = "COSMIC\nHORIZON";
        title.alignment = TextAnchor.MiddleLeft;
        title.fontSize = 158;
        title.lineSpacing = 0.84f;
        title.color = new Color(0.95f, 0.97f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(-500, 190);
        title.rectTransform.sizeDelta = new Vector2(680, 350);
        var oldPanel = root.Find("TitlePanel");
        if (oldPanel != null) UnityEngine.Object.DestroyImmediate(oldPanel.gameObject);
        var rule = PresentationImage(root, "TitleRule", new Vector2(-799, -1), new Vector2(82, 3));
        rule.color = Accent;
        string[] labels = { "New Game", "Continue", "Settings", "Quit" };
        for (int i = 0; i < labels.Length; i++)
        {
            var button = root.Find(labels[i])?.GetComponent<Button>();
            if (button == null) continue;
            StyleActionButton(button, new Vector2(-654, -58 - i * 70), new Vector2(372, 56), i == 0);
        }
        root.GetComponentsInChildren<SettingsUI>(true).FirstOrDefault()?.transform.SetAsLastSibling();
    }

    private static readonly Color Accent = new Color(0.97f, 0.73f, 0.40f);
    private static readonly Color Ink = new Color(0.043f, 0.073f, 0.12f, 0.94f);
    private static readonly Color Paper = new Color(0.90f, 0.94f, 0.99f);
    private static readonly Color Muted = new Color(0.61f, 0.72f, 0.82f);

    private static Image PresentationImage(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var image = parent.Find(name)?.GetComponent<Image>() ?? Frame(parent, name, position, size);
        image.type = Image.Type.Simple;
        image.sprite = null;
        image.raycastTarget = false;
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = size;
        return image;
    }

    private static void StyleActionButton(Button button, Vector2 position, Vector2 size, bool primary = false)
    {
        ResizeButton(button, position, size);
        var graphic = (Image)button.targetGraphic;
        graphic.sprite = null;
        graphic.type = Image.Type.Simple;
        graphic.color = Color.white;
        var colors = ColorBlock.defaultColorBlock;
        colors.normalColor = primary ? Accent : Ink;
        colors.highlightedColor = primary ? new Color(1f, 0.87f, 0.60f) : new Color(0.16f, 0.26f, 0.39f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.40f, 0.50f, 0.61f);
        colors.disabledColor = new Color(0.04f, 0.07f, 0.11f, 0.50f);
        button.colors = colors;
        var label = button.GetComponentInChildren<Text>();
        label.font = UIFont();
        label.alignment = TextAnchor.MiddleLeft;
        label.color = primary ? Ink : Paper;
        label.rectTransform.sizeDelta = size - new Vector2(40f, 4f);
        RecordUIOverride(button);
        RecordUIOverride(graphic);
        RecordUIOverride(button.transform);
        RecordUIOverride(label);
        RecordUIOverride(label.rectTransform);
    }

    private static void RecordUIOverride(Component component)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(component))
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }

    private static void StyleMissionBoard(Transform root)
    {
        var panel = PresentationImage(root, "DispatchPanel", new Vector2(-638, 119), new Vector2(552, 766));
        panel.color = Ink;
        panel.transform.SetAsFirstSibling();
        var rule = PresentationImage(root, "DispatchRule", new Vector2(-638, 378), new Vector2(504, 2));
        rule.color = new Color(0.23f, 0.33f, 0.43f);
        var title = root.Find("HubTitle")?.GetComponent<Text>();
        if (title != null)
        {
            title.text = "EXPEDITION DISPATCH";
            title.color = Paper;
            PositionLabel(title, new Vector2(-638, 437), new Vector2(504, 68), 52, TextAnchor.MiddleLeft);
        }
        string[] destinations = { "Shallows", "Twilight", "Abyssal ≥1500m", "EventHorizon ≥2500m" };
        string[] readings = { "OCEAN", "LUNAR SURFACE", "VACUUM · SUIT 1500 M", "VACUUM · SUIT 2500 M" };
        for (int i = 0; i < destinations.Length; i++)
        {
            var button = root.Find(destinations[i])?.GetComponent<Button>();
            if (button != null) StyleDestinationCard(button, new Vector2(-768 + (i % 2) * 260, 272 - (i / 2) * 190), i, readings[i]);
        }
        string[] actions = { "Tutorial", "Cantina", "Talk to crew", "Sleep", "Claim boss loot" };
        string[] labels = { "Training Dock", "Nebula Cantina", "Talk to crew", "End day", "Claim boss loot" };
        for (int i = 0; i < actions.Length; i++)
        {
            var button = root.Find(actions[i])?.GetComponent<Button>();
            if (button == null) continue;
            StyleActionButton(button, new Vector2(-768 + (i % 2) * 260, -68 - (i / 2) * 66), new Vector2(244, 54));
            button.GetComponentInChildren<Text>().text = labels[i];
        }
        var workshop = root.Find("Workshop");
        if (workshop == null) return;
        var open = root.Find("Suit workshop")?.GetComponent<Button>();
        if (open == null)
        {
            open = UIButton(root, "Suit workshop", Vector2.zero, null);
            UnityEventTools.AddBoolPersistentListener(open.onClick, workshop.gameObject.SetActive, true);
        }
        StyleActionButton(open, new Vector2(-508, -200), new Vector2(244, 54));
        open.transform.SetSiblingIndex(panel.transform.GetSiblingIndex() + 1);
        if (workshop.Find("Close workshop") == null)
        {
            var close = UIButton(workshop, "Close workshop", Vector2.zero, null);
            ResizeButton(close, new Vector2(0, -283), new Vector2(270, 52));
            UnityEventTools.AddBoolPersistentListener(close.onClick, workshop.gameObject.SetActive, false);
        }
        workshop.gameObject.SetActive(false);
    }

    private static void StyleDestinationCard(Button button, Vector2 position, int index, string reading)
    {
        var size = new Vector2(244, 168);
        StyleActionButton(button, position, size);
        var thumbnail = PresentationImage(button.transform, "Landscape", new Vector2(0, 25), new Vector2(232, 104));
        thumbnail.sprite = BackdropSprite(DestinationArt[index]);
        thumbnail.color = new Color(1f, 1f, 1f, 0.94f);
        thumbnail.transform.SetAsFirstSibling();
        var thumbnailTint = thumbnail.GetComponent<CanvasGroup>();
        if (thumbnailTint == null) thumbnailTint = thumbnail.gameObject.AddComponent<CanvasGroup>();
        Set(button.GetComponent<UICommandButton>(), "availabilityGraphic", thumbnailTint);
        var label = button.GetComponentInChildren<Text>();
        label.font = UIFont(true);
        label.text = DestinationNames[index];
        PositionLabel(label, new Vector2(0, -45), new Vector2(216, 37), 29, TextAnchor.MiddleLeft);
        var info = button.transform.Find("Terrain")?.GetComponent<Text>();
        if (info == null) info = ServiceText(button.transform, "Terrain", Vector2.zero, Vector2.zero);
        info.text = reading;
        info.color = Muted;
        PositionLabel(info, new Vector2(0, -68), new Vector2(216, 22), 16, TextAnchor.MiddleLeft);
    }

    private static void ResizeButton(Button button, Vector2 position, Vector2 size)
    {
        var rect = button.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var text = button.GetComponentInChildren<Text>();
        text.rectTransform.sizeDelta = size - new Vector2(24f, 4f);
        text.fontSize = 25;
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
    private static void EnsureBossLootButton(Transform root)
    {
        const string label = "Claim boss loot";
        if (root.Find(label) != null) return;

        var inventory = Ensure<SO_InventoryAccess>("Data/SO_InventoryAccess");
        var button = UIButton(root, label, new Vector2(-760, -200), inventory.ClaimRewards);
        // Match the fresh scene's draw order so HUD and modal panels stay above the button.
        var depthGauge = root.Find("DepthGauge");
        if (depthGauge != null) button.transform.SetSiblingIndex(depthGauge.GetSiblingIndex());
    }
    private static void BuildSceneUI(Scene scene)
    {
        var existingUI = scene.GetRootGameObjects().FirstOrDefault(obj => obj.name == "CosmicUI");
        if (existingUI != null)
        {
            if (scene.name == "SC_Mothership") EnsureBossLootButton(existingUI.transform);
            return;
        }
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
            EnsureBossLootButton(root);
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
        for (int i = 0; i < labels.Length; i++)
        {
            var button = UIButton(pausePanel.transform, labels[i], new Vector2(0, 200 - i * 85), actions[i]);
            if (i == 2) Set(pause, "saveButton", button);
        }
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
        UIIcon(root, "OxygenIcon", new Vector2(-915, 405), new Vector2(42, 42), ArtSprite("item_oxygen"));
        UIIcon(root, "FuelIcon", new Vector2(-915, 350), new Vector2(42, 42), ArtSprite("item_battery"));
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
