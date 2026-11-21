using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Data/Save Session")]
public sealed class SO_SaveSession : ScriptableObject
{
    private SaveData data;
    public SaveData Data => data ??= new SaveData();
    /// <summary>Replaces the shared session after loading or starting a game.</summary>
    public void Replace(SaveData value) { data = value ?? new SaveData(); }
    private void OnEnable() { data = new SaveData(); }
}
