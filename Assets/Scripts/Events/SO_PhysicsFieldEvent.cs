using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Physics Field")]
public sealed class SO_PhysicsFieldEvent : ScriptableObject
{
    private event Action<Vector2, float, float> listeners;
    /// <summary>Registers a pooled projectile or debris endpoint.</summary>
    public void RegisterListener(Action<Vector2, float, float> listener) { listeners += listener; }
    /// <summary>Unregisters an endpoint.</summary>
    public void UnregisterListener(Action<Vector2, float, float> listener) { listeners -= listener; }
    /// <summary>Broadcasts a gravity well during the physics step.</summary>
    public void Raise(Vector2 center, float radius, float strength) { listeners?.Invoke(center, radius, strength); }
}
