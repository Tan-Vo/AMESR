using System;
using System.Globalization;
using System.IO;

public static class PresentationRules
{
    public static bool IsSheetAsset(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;
        string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        foreach (string marker in new[] { "matrix", "master", "spritesheet", "sheet", "grid" })
            if (name.Contains(marker)) return true;
        string normalized = path.Replace('\\', '/');
        return (normalized.StartsWith("Bosses/", StringComparison.OrdinalIgnoreCase) || normalized.IndexOf("/Bosses/", StringComparison.OrdinalIgnoreCase) >= 0) && name.Contains("_main");
    }

    public static bool IsDiveScene(string name) => name == "SC_Tutorial" || name == "SC_BossArena" ||
        (name != null && name.StartsWith("SC_DiveZone", StringComparison.Ordinal));

    public static bool StartStandaloneArena(int sceneCount, bool encounterActive) => sceneCount == 1 && !encounterActive;

    public static string RecipeName(string value) => string.IsNullOrWhiteSpace(value) ? "Unknown Dish" :
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Replace('_', ' ').Trim().ToLowerInvariant());

    public static string RecipeLabel(string name, int price, int servings, bool selected)
    {
        if (price < 0 || servings < 0) throw new ArgumentOutOfRangeException(price < 0 ? nameof(price) : nameof(servings));
        return (selected ? "✓ " : "") + RecipeName(name) + "\n₡" + price + " • " + servings + " servings";
    }
}
