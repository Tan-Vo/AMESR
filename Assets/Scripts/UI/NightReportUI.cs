using UnityEngine;
using UnityEngine.UI;

public sealed class NightReportUI : MonoBehaviour
{
    [SerializeField] private CantinaManager cantina;
    [SerializeField] private GameObject panel;
    [SerializeField] private Text summary;
    [SerializeField] private SO_UICommandEvent onUICommandRequested;
    private void LateUpdate()
    {
        panel.SetActive(cantina.HasReport);
        if (!cantina.HasReport) return;
        var report = cantina.Report;
        summary.text = "NIGHT REPORT\nCustomers served: " + report.Served + "/" + (report.Served + report.Angry) +
            "\nAngry: " + report.Angry + "\nRevenue: ₡" + report.Revenue + "\nTips: ₡" + report.Tips +
            "\nBest dish: " + report.BestDish + "\nReputation: " + new string('★', report.Stars);
    }
    /// <summary>Returns to the hub's night shop and bed.</summary>
    public void Continue() { onUICommandRequested?.Raise(UICommand.Hub); }
}
