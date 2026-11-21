using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Ingredient Stock")]
public sealed class SO_IngredientStock : ScriptableObject
{
    [SerializeField] private SO_SaveSession session;
    /// <summary>Counts saved ingredient units.</summary>
    public int Count(SO_ItemData item)
    {
        if (session == null || item == null) return 0;
        long count = 0;
        foreach (var slot in session.Data.Inventory) if (slot.ItemId == item.name) count += slot.Quantity;
        return (int)System.Math.Min(int.MaxValue, count);
    }
    private int Required(SO_DishRecipe recipe, SO_ItemData item)
    {
        long count = 0;
        foreach (var ingredient in recipe.Ingredients) if (ingredient.Item == item) count += Mathf.Max(0, ingredient.Quantity);
        return (int)System.Math.Min(int.MaxValue, count);
    }
    /// <summary>Estimates servings without consuming ingredients, including repeated recipe entries.</summary>
    public int Servings(SO_DishRecipe recipe)
    {
        if (recipe == null || recipe.Ingredients == null || recipe.Ingredients.Length == 0) return 0;
        int batches = int.MaxValue;
        foreach (var ingredient in recipe.Ingredients)
        {
            if (ingredient.Item == null || ingredient.Quantity <= 0) return 0;
            batches = Mathf.Min(batches, Count(ingredient.Item) / Required(recipe, ingredient.Item));
        }
        return (int)System.Math.Min(int.MaxValue, (long)batches * Mathf.Max(1, recipe.ServingsPerCook));
    }
    /// <summary>Atomically consumes ingredients for one batch.</summary>
    public bool Consume(SO_DishRecipe recipe)
    {
        if (session == null || Servings(recipe) <= 0) return false;
        foreach (var ingredient in recipe.Ingredients)
        {
            int remaining = ingredient.Quantity;
            for (int i = session.Data.Inventory.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = session.Data.Inventory[i];
                if (slot.ItemId != ingredient.Item.name) continue;
                int take = Mathf.Min(remaining, slot.Quantity);
                remaining -= take; slot.Quantity -= take;
                if (slot.Quantity == 0) session.Data.Inventory.RemoveAt(i);
            }
        }
        return true;
    }
}
