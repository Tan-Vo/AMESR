using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/String Event")]
public class SO_StringEvent : ScriptableObject
{
    private event System.Action<string> raised;
    /// <summary>Broadcasts to all subscribed listeners.</summary>
    public void Raise(string value) => raised?.Invoke(value);
    /// <summary>Registers a callback once.</summary>
    public void RegisterListener(System.Action<string> listener) { raised -= listener; raised += listener; }
    /// <summary>Unregisters a callback.</summary>
    public void UnregisterListener(System.Action<string> listener) { raised -= listener; }
    private void OnDisable() { raised = null; }
}
