using UnityEngine;
using UnityEngine.UI;

public sealed class CantinaHUD : MonoBehaviour
{
    [SerializeField] private CantinaManager cantina;
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_IngredientStock stock;
    [SerializeField] private SO_FloatEvent onPhaseTimeChanged;
    [SerializeField] private Text summary;
    private float remaining = 180f, displayRevenue;
    private void OnEnable() { onPhaseTimeChanged?.RegisterListener(TimeChanged); }
    private void OnDisable() { onPhaseTimeChanged?.UnregisterListener(TimeChanged); }
    private void TimeChanged(float value) { remaining = value; }
    private void LateUpdate()
    {
        summary.enabled = cantina.IsRunning;
        if (!summary.enabled) return;
        displayRevenue = Mathf.MoveTowards(displayRevenue, cantina.Revenue, Time.unscaledDeltaTime * 300f);
        summary.text = "REVENUE ₡" + ((int)displayRevenue) + "   " + Mathf.CeilToInt(remaining) + "s   " + new string('★', session.Data.ReputationStars) + "\n";
        foreach (var dish in cantina.Menu) summary.text += dish.DishName + " ×" + stock.Servings(dish) + "   ";
    }
}
