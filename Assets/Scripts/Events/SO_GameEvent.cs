using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Game Event")]
public class SO_GameEvent : ScriptableObject
{
    private event System.Action raised;
    /// <summary>Broadcasts to all subscribed listeners.</summary>
    public void Raise() => raised?.Invoke();
    /// <summary>Registers a callback once.</summary>
    public void RegisterListener(System.Action listener) { raised -= listener; raised += listener; }
    /// <summary>Unregisters a callback.</summary>
    public void UnregisterListener(System.Action listener) { raised -= listener; }
    private void OnDisable() { raised = null; }
}
