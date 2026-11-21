using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Data/Night Report")]
public sealed class SO_NightReport : ScriptableObject
{
    public int Served { get; private set; }
    public int Angry { get; private set; }
    public int Revenue { get; private set; }
    public int Tips { get; private set; }
    public int Stars { get; private set; }
    public string BestDish { get; private set; }
    /// <summary>Captures the completed night's immutable display values.</summary>
    public void Capture(int served, int angry, int revenue, int tips, int stars, string bestDish)
    { Served = served; Angry = angry; Revenue = revenue; Tips = tips; Stars = stars; BestDish = bestDish; }
}
