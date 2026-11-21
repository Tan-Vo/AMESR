using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class CosmicHorizonProjectBuilder
{
    private static void BuildDivingData()
    {
        Ensure<SO_DiveInteractionEvent>("Events/OnDiveInteraction");
        Ensure<SO_GameEvent>("Events/OnCableSnapped");
        var catalog = Ensure<SO_CatchCatalog>("Data/SO_CatchCatalog");
        var creatures = CreatureAssets();
        var items = new SO_ItemData[creatures.Length];
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = Ensure<SO_ItemData>("Data/SO_Catch_" + creatures[i].name);
            Set(items[i], "itemName", creatures[i].CreatureName, "icon", creatures[i].Icon, "category", (int)ItemCategory.Ingredient,
                "baseSellPrice", creatures[i].SellPrice, "maxStack", 20);
        }
        SetArray(catalog, "creatures", creatures.Length, (p, i) => p.objectReferenceValue = creatures[i]);
        SetArray(catalog, "items", items.Length, (p, i) => p.objectReferenceValue = items[i]);
    }
    private static LineRenderer Cable(Transform parent, string name, Color color)
    {
        var obj = new GameObject(name, typeof(LineRenderer));
        obj.transform.SetParent(parent, false);
        var line = obj.GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = line.endWidth = 0.035f;
        line.startColor = line.endColor = color;
        line.sortingLayerName = "Weapons_FX";
        line.enabled = false;
        Folder("Assets/Materials");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Cable.mat");
        if (material == null) { material = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(material, "Assets/Materials/M_Cable.mat"); }
        line.sharedMaterial = material;
        return line;
    }
    private static Image DiveMeter(Transform canvas, string name, Vector2 position, bool radial)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(canvas, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.2f);
        rect.anchoredPosition = position;
        rect.sizeDelta = radial ? new Vector2(64, 64) : new Vector2(250, 36);
        var image = obj.GetComponent<Image>();
        image.sprite = SpriteAt("Items/crystal_quantum_biolum_gem_01.png");
        image.type = Image.Type.Filled;
        image.fillMethod = radial ? Image.FillMethod.Radial360 : Image.FillMethod.Horizontal;
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }
    private static void BuildDivingPlayer(GameObject player, Transform canvas)
    {
        Folder("Assets/Prefabs/Diving");
        var tip = new GameObject("HarpoonTip", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(HarpoonProjectile));
        ConfigureProjectileBody(tip);
        tip.tag = "Harpoon";
        tip.layer = 8;
        tip.GetComponent<SpriteRenderer>().sprite = SpriteAt("Weapons/harpoon_tip_basic_gravity.png");
        tip.GetComponent<SpriteRenderer>().sharedMaterial = SpriteLitMaterial();
        tip.GetComponent<SpriteRenderer>().sortingLayerName = "Weapons_FX";
        var prefab = PrefabUtility.SaveAsPrefabAsset(tip, "Assets/Prefabs/Diving/PF_Harpoon.prefab");
        var pool = Pool<HarpoonPool>(player.transform, "HarpoonPool", prefab.GetComponent<HarpoonProjectile>());
        Set(pool, "initialSize", 2);
        var firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(player.transform, false);
        firePoint.localPosition = new Vector3(0, 0.15f, 0);
        var harpoon = player.AddComponent<HarpoonSystem>();
        WireChannels(harpoon);
        Set(harpoon, "equippedHarpoon", Ensure<SO_WeaponData>("Data/SO_BasicHarpoon"), "firePoint", firePoint,
            "harpoonPool", pool, "targetMask", (1 << 7) | (1 << 9) | (1 << 10),
            "cableRenderer", Cable(player.transform, "HarpoonCable", Color.cyan), "tensionMeter", DiveMeter(canvas, "Tension", new Vector2(0, 0), false));
        player.AddComponent<DistanceJoint2D>().enabled = false;
        var grapple = player.AddComponent<GrappleSystem>();
        Set(grapple, "grappleLayer", (1 << 11) | (1 << 9), "grappleLine", Cable(player.transform, "GrappleCable", Color.yellow));
        var mining = player.AddComponent<MiningSystem>();
        WireChannels(mining);
        Set(mining, "progressBar", DiveMeter(canvas, "MiningProgress", new Vector2(0, 60), true));
        var label = new GameObject("DiveControls", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(canvas, false);
        var rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -12);
        rect.sizeDelta = new Vector2(1100, 70);
        var text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.alignment = TextAnchor.UpperCenter;
        text.raycastTarget = false;
        text.text = "LMB: Harpoon / Hold to reel • Rest to cool tension • Green = sweet spot\nRMB: Hold grapple / Release to swing free • E: Hold to mine";
    }
    private static void BuildDivingPrefabs()
    {
        Folder("Assets/Prefabs/Diving");
        var ore = new GameObject("OreNode", typeof(SpriteRenderer), typeof(OreNode));
        ore.layer = 11;
        ore.GetComponent<SpriteRenderer>().sprite = SpriteAt("Items/crystal_quantum_biolum_gem_01.png");
        ore.GetComponent<SpriteRenderer>().sharedMaterial = SpriteLitMaterial();
        ore.GetComponent<SpriteRenderer>().sortingLayerName = "Environment_Front";
        var node = ore.GetComponent<OreNode>();
        WireChannels(node);
        Set(node, "item", Ensure<SO_ItemData>("Data/SO_QuantumCrystal"));
        PrefabUtility.SaveAsPrefabAsset(ore, "Assets/Prefabs/Diving/PF_OreNode.prefab");
        var anchor = new GameObject("GrapplePoint", typeof(SpriteRenderer), typeof(CircleCollider2D));
        anchor.layer = 11;
        anchor.tag = "GrapplePoint";
        anchor.GetComponent<CircleCollider2D>().radius = 0.65f;
        anchor.GetComponent<CircleCollider2D>().isTrigger = true;
        anchor.GetComponent<SpriteRenderer>().sprite = SpriteAt("Tilesets/tile_008.png");
        anchor.GetComponent<SpriteRenderer>().sharedMaterial = SpriteLitMaterial();
        anchor.GetComponent<SpriteRenderer>().sortingLayerName = "Environment_Front";
        PrefabUtility.SaveAsPrefabAsset(anchor, "Assets/Prefabs/Diving/PF_GrapplePoint.prefab");
    }
    private static void BuildDivingScene(Scene scene, Transform player)
    {
        if (scene.GetRootGameObjects().Any(root => root.name == "DiveResources")) return;
        var root = new GameObject("DiveResources");
        for (int i = 0; i < 6; i++)
        {
            var ore = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Diving/PF_OreNode.prefab"), scene);
            ore.transform.SetParent(root.transform, true);
            ore.transform.position = player.position + new Vector3(3f + i * 5f, -3f - i * 2f, 0);
            var anchor = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Diving/PF_GrapplePoint.prefab"), scene);
            anchor.transform.SetParent(root.transform, true);
            anchor.transform.position = player.position + new Vector3(i * 5f, 4f - i, 0);
        }
    }
}
