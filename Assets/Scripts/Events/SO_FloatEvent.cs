using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Float Event")]
public class SO_FloatEvent : ScriptableObject
{
    private event System.Action<float> raised;
    /// <summary>Broadcasts to all subscribed listeners.</summary>
    public void Raise(float value) => raised?.Invoke(value);
    /// <summary>Registers a callback once.</summary>
    public void RegisterListener(System.Action<float> listener) { raised -= listener; raised += listener; }
    /// <summary>Unregisters a callback.</summary>
    public void UnregisterListener(System.Action<float> listener) { raised -= listener; }
    private void OnDisable() { raised = null; }
}
