using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MenuSetup : MonoBehaviour
{
    [SerializeField] private CantinaManager cantina;
    [SerializeField] private SO_IngredientStock stock;
    [SerializeField] private SO_DishRecipe[] recipes;
    [SerializeField] private GameObject prepPanel;
    [SerializeField] private Text status;
    [SerializeField] private Text[] recipeLabels;
    private readonly List<SO_DishRecipe> selected = new(5);
    /// <summary>Toggles a recipe in the five-dish pre-service menu.</summary>
    public void ToggleRecipe(int index)
    {
        if (cantina.IsRunning || index < 0 || index >= recipes.Length) return;
        var recipe = recipes[index];
        if (selected.Contains(recipe)) selected.Remove(recipe);
        else if (selected.Count < 5 && stock.Servings(recipe) > 0) selected.Add(recipe);
    }
    /// <summary>Opens service only if the selected recipes have ingredients.</summary>
    public void Open() { if (cantina.SetMenu(selected)) cantina.Open(); }
    private void LateUpdate()
    {
        prepPanel.SetActive(!cantina.IsRunning && !cantina.HasReport);
        for (int i = 0; i < recipes.Length; i++)
            recipeLabels[i].text = (selected.Contains(recipes[i]) ? "✓ " : "") + recipes[i].DishName + "  ₡" + recipes[i].BasePrice + "  (" + stock.Servings(recipes[i]) + " servings)";
        status.text = cantina.HasReport ? "Night report\nServed: " + cantina.Report.Served + "  Angry: " + cantina.Report.Angry +
            "\nRevenue: " + cantina.Report.Revenue + "  Tips: " + cantina.Report.Tips + "\nBest dish: " + cantina.Report.BestDish + "  Stars: " + cantina.Report.Stars :
            cantina.IsRunning ? "E at counter: pick up • E at table: serve • E: clear plates" : "Choose up to five dishes, then open. Ingredients come from your dive cargo.";
    }
}
