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
    /// <summary>Registers a component listener once.</summary>
    public void RegisterListener(SO_GameEventListener listener)
    {
        if (listener != null) RegisterListener(listener.OnEventRaised);
    }
    /// <summary>Unregisters a component listener.</summary>
    public void UnregisterListener(SO_GameEventListener listener)
    {
        if (listener != null) UnregisterListener(listener.OnEventRaised);
    }
    private void OnDisable() { raised = null; }
}
