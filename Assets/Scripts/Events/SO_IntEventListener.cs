using UnityEngine;
using UnityEngine.Events;

public class SO_IntEventListener : MonoBehaviour
{
    [SerializeField] private SO_IntEvent gameEvent;
    [SerializeField] private UnityEvent<int> response = new();
    private void OnEnable() { if (gameEvent != null) gameEvent.RegisterListener(OnEventRaised); }
    private void OnDisable() { if (gameEvent != null) gameEvent.UnregisterListener(OnEventRaised); }
    /// <summary>Invokes the serialized response.</summary>
    public void OnEventRaised(int value) => response.Invoke(value);
}
