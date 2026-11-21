using UnityEngine;
using UnityEngine.Events;

public class SO_FloatEventListener : MonoBehaviour
{
    [SerializeField] private SO_FloatEvent gameEvent;
    [SerializeField] private UnityEvent<float> response = new();
    private void OnEnable() { if (gameEvent != null) gameEvent.RegisterListener(OnEventRaised); }
    private void OnDisable() { if (gameEvent != null) gameEvent.UnregisterListener(OnEventRaised); }
    /// <summary>Invokes the serialized response.</summary>
    public void OnEventRaised(float value) => response.Invoke(value);
}
