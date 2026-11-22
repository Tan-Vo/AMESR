using UnityEngine;

internal readonly struct StoryPalette
{
    internal readonly int Biome;
    internal readonly string Backdrop;
    internal readonly Color Sky, Accent, Water, Grade;
    internal readonly float Ambient, WaterOpacity, Exposure, Vignette, Bloom;

    private StoryPalette(int biome, string backdrop, Color sky, Color accent, Color water, Color grade,
        float ambient, float waterOpacity, float exposure, float vignette, float bloom)
    {
        Biome = biome; Backdrop = backdrop;
        Sky = sky; Accent = accent; Water = water; Grade = grade;
        Ambient = ambient; WaterOpacity = waterOpacity; Exposure = exposure; Vignette = vignette; Bloom = bloom;
    }

    internal static StoryPalette? ForScene(string scene) => scene switch
    {
        "SC_MainMenu" => new StoryPalette(-1, "title",
            new Color(.025f, .04f, .09f), new Color(.4f, .8f, 1f), Color.clear, Color.white, 1f, 0f, 0f, .08f, .13f),
        "SC_Mothership" => new StoryPalette(-1, "mothership",
            new Color(.04f, .07f, .12f), new Color(1f, .64f, .30f), Color.clear, new Color(1f, .98f, .96f), .90f, 0f, 0f, .13f, .12f),
        "SC_NebulaCantina" => new StoryPalette(-1, "cantina",
            new Color(.13f, .063f, .05f), new Color(1f, .48f, .21f), Color.clear, new Color(1f, .97f, .94f), .90f, 0f, .03f, .12f, .16f),
        "SC_Tutorial" or "SC_DiveZone1_Shallows" => Dive(0),
        "SC_DiveZone2_Twilight" => Dive(1),
        "SC_DiveZone3_Abyssal" => Dive(2),
        "SC_DiveZone4_EventHorizon" => Dive(3),
        _ => null
    };

    internal static StoryPalette Dive(int biome) => biome switch
    {
        0 => new StoryPalette(0, "shallows", new Color(.03f, .15f, .19f),
            new Color(.31f, .90f, 1f), new Color(.25f, .67f, .78f), new Color(.97f, 1f, 1f), .94f, .07f, 0f, .14f, .17f),
        1 => new StoryPalette(1, "twilight", new Color(.071f, .037f, .12f),
            new Color(.94f, .72f, .56f), Color.clear, new Color(1f, .98f, .99f), .90f, 0f, -.02f, .17f, .16f),
        2 => new StoryPalette(2, "abyssal", new Color(.013f, .037f, .074f),
            new Color(.36f, .80f, 1f), Color.clear, new Color(.95f, .98f, 1f), .86f, 0f, -.04f, .21f, .14f),
        3 => new StoryPalette(3, "event", new Color(.072f, .022f, .029f),
            new Color(1f, .58f, .45f), Color.clear, new Color(1f, .95f, .95f), .82f, 0f, -.06f, .24f, .13f),
        _ => throw new System.ArgumentOutOfRangeException(nameof(biome))
    };
}
