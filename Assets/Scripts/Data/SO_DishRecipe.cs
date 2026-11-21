using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Data/DishRecipe")]
public class SO_DishRecipe : ScriptableObject
{
    [SerializeField] private string dishName;
    public string DishName => dishName;
    [SerializeField] private Sprite dishIcon;
    public Sprite DishIcon => dishIcon;
    [SerializeField] private Sprite dishSprite;
    public Sprite DishSprite => dishSprite;
    [SerializeField] private RecipeIngredient[] ingredients;
    public RecipeIngredient[] Ingredients => ingredients;
    [SerializeField] private float cookTime = 4f;
    public float CookTime => cookTime;
    [SerializeField] private int servingsPerCook = 1;
    public int ServingsPerCook => servingsPerCook;
    [SerializeField] private int basePrice = 50;
    public int BasePrice => basePrice;
    [SerializeField] private int tipMultiplierPercent = 100;
    public int TipMultiplierPercent => tipMultiplierPercent;
    [SerializeField] private DishQuality quality;
    public DishQuality Quality => quality;
    [SerializeField] private string flavorText;
    public string FlavorText => flavorText;
}
