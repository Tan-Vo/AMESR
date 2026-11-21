using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Floating Text")]
public sealed class SO_FloatingTextEvent : ScriptableObject
{
    private event Action<Vector3, string, Color> listeners;
    /// <summary>Registers a pooled visual presenter.</summary>
    public void RegisterListener(Action<Vector3, string, Color> listener) { listeners += listener; }
    /// <summary>Unregisters a presenter.</summary>
    public void UnregisterListener(Action<Vector3, string, Color> listener) { listeners -= listener; }
    /// <summary>Requests text without allocating a runtime entity.</summary>
    public void Raise(Vector3 position, string value, Color color) { listeners?.Invoke(position, value, color); }
}
