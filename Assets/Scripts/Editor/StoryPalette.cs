using UnityEngine;

internal readonly struct StoryPalette
{
    internal readonly int Biome;
    internal readonly string Backdrop;
    internal readonly Color Sky, Scenery, Accent, Water, Grade;
    internal readonly float Ambient, WaterOpacity, Exposure, Vignette, Bloom;

    private StoryPalette(int biome, string backdrop, Color sky, Color scenery, Color accent, Color water, Color grade,
        float ambient, float waterOpacity, float exposure, float vignette, float bloom)
    {
        Biome = biome; Backdrop = backdrop;
        Sky = sky; Scenery = scenery; Accent = accent; Water = water; Grade = grade;
        Ambient = ambient; WaterOpacity = waterOpacity; Exposure = exposure; Vignette = vignette; Bloom = bloom;
    }

    internal static StoryPalette? ForScene(string scene) => scene switch
    {
        "SC_MainMenu" => new StoryPalette(0, "hangar_docking_bay_background.jpg",
            new Color(.025f, .04f, .09f), new Color(.70f, .80f, .92f), new Color(.4f, .8f, 1f), Color.clear, Color.white, 1f, 0f, 0f, .18f, .13f),
        "SC_Mothership" => new StoryPalette(-1, "hangar_docking_bay_background.jpg",
            new Color(.10f, .055f, .045f), new Color(.88f, .68f, .54f), new Color(1f, .64f, .30f), Color.clear, new Color(1f, .98f, .96f), .90f, 0f, 0f, .13f, .12f),
        "SC_NebulaCantina" => new StoryPalette(-1, "deep_space_asteroid_background.jpg",
            new Color(.13f, .063f, .05f), new Color(.65f, .44f, .40f), new Color(1f, .48f, .21f), Color.clear, new Color(1f, .97f, .94f), .90f, 0f, .03f, .12f, .16f),
        "SC_Tutorial" or "SC_DiveZone1_Shallows" => Dive(0),
        "SC_DiveZone2_Twilight" => Dive(1),
        "SC_DiveZone3_Abyssal" => Dive(2),
        "SC_DiveZone4_EventHorizon" => Dive(3),
        _ => null
    };

    internal static StoryPalette Dive(int biome) => biome switch
    {
        0 => new StoryPalette(0, null, new Color(.03f, .15f, .19f), new Color(.67f, .93f, 1f),
            new Color(.31f, .90f, 1f), new Color(.25f, .67f, .78f), new Color(.97f, 1f, 1f), .94f, .07f, 0f, .14f, .17f),
        1 => new StoryPalette(1, null, new Color(.027f, .06f, .15f), new Color(.56f, .70f, .89f),
            new Color(.40f, .64f, 1f), new Color(.29f, .45f, .78f), new Color(.96f, .98f, 1f), .90f, .09f, -.02f, .17f, .16f),
        2 => new StoryPalette(2, null, new Color(.015f, .067f, .069f), new Color(.43f, .62f, .64f),
            new Color(.26f, .85f, .73f), new Color(.27f, .64f, .56f), new Color(.95f, 1f, .99f), .86f, .11f, -.04f, .21f, .14f),
        3 => new StoryPalette(3, null, new Color(.039f, .019f, .068f), new Color(.45f, .37f, .59f),
            new Color(.76f, .47f, 1f), new Color(.62f, .37f, .78f), new Color(.99f, .96f, 1f), .82f, .13f, -.06f, .24f, .13f),
        _ => throw new System.ArgumentOutOfRangeException(nameof(biome))
    };
}
