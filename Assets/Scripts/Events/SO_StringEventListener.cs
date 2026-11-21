using UnityEngine;
using UnityEngine.Events;

public class SO_StringEventListener : MonoBehaviour
{
    [SerializeField] private SO_StringEvent gameEvent;
    [SerializeField] private UnityEvent<string> response = new();
    private void OnEnable() { if (gameEvent != null) gameEvent.RegisterListener(OnEventRaised); }
    private void OnDisable() { if (gameEvent != null) gameEvent.UnregisterListener(OnEventRaised); }
    /// <summary>Invokes the serialized response.</summary>
    public void OnEventRaised(string value) => response.Invoke(value);
}
