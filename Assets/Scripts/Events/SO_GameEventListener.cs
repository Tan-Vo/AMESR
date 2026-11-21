using UnityEngine;
using UnityEngine.Events;

public class SO_GameEventListener : MonoBehaviour
{
    [SerializeField] private SO_GameEvent gameEvent;
    [SerializeField] private UnityEvent response = new();
    private void OnEnable() { if (gameEvent != null) gameEvent.RegisterListener(OnEventRaised); }
    private void OnDisable() { if (gameEvent != null) gameEvent.UnregisterListener(OnEventRaised); }
    /// <summary>Invokes the serialized response.</summary>
    public void OnEventRaised() => response.Invoke();
}
