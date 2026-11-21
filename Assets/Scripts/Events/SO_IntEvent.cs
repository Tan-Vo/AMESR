using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Int Event")]
public class SO_IntEvent : ScriptableObject
{
    private event System.Action<int> raised;
    /// <summary>Broadcasts to all subscribed listeners.</summary>
    public void Raise(int value) => raised?.Invoke(value);
    /// <summary>Registers a callback once.</summary>
    public void RegisterListener(System.Action<int> listener) { raised -= listener; raised += listener; }
    /// <summary>Unregisters a callback.</summary>
    public void UnregisterListener(System.Action<int> listener) { raised -= listener; }
    private void OnDisable() { raised = null; }
}
