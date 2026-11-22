using System;
using UnityEditor;
using UnityEngine;

public static partial class CosmicHorizonProjectBuilder
{
    private static Sprite ArtSprite(string name) => SpriteAt("Horizon/" + name + ".png");
    private static Sprite BackdropSprite(string name) => SpriteAt("Horizon/bg_" + name + ".jpg");

    private static readonly string[] DestinationNames = { "Pelagic Moon", "Twilight Crater", "Prism Hollow", "Event Horizon Wreck" };
    private static readonly string[] DestinationArt = { "shallows", "twilight", "abyssal", "event" };
    private static readonly string[] DestinationProps = { "reef", "spire", "quartz", "scrap" };
    private static readonly string[] WildlifeArt = { "angler", "gasbag", "crab", "worm", "serpent", "cuttlefish", "ray", "drone" };
    private static readonly string[] BossArt = { "maw", "prism", "warden" };
    private static readonly string[] GuestArt = { "guest_teal", "guest_amber", "guest_blue" };

    private static int DestinationIndex(string sceneName)
    {
        return sceneName.StartsWith("SC_DiveZone", StringComparison.Ordinal) ? sceneName[11] - '1' : -1;
    }

    private static void BuildBackdrop(Transform parent, Camera camera, string art, bool water = false, int order = 0)
    {
        const string name = "DestinationBackdrop";
        var backdrop = parent.Find(name)?.gameObject;
        if (backdrop == null)
        {
            backdrop = new GameObject(name, typeof(SpriteRenderer), typeof(CosmicBackdrop));
            backdrop.transform.SetParent(parent, false);
        }
        var renderer = backdrop.GetComponent<SpriteRenderer>();
        renderer.sprite = BackdropSprite(art);
        renderer.sortingLayerName = "BG_Skybox";
        renderer.sortingOrder = order;
        Set(backdrop.GetComponent<CosmicBackdrop>(), "worldCamera", camera, "alignWater", water,
            "overscan", art == "title" ? Vector2.one : new Vector2(1.3f, 1.12f));
    }
}
