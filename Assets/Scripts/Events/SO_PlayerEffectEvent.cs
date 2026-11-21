using System;
using UnityEngine;

public enum PlayerEffectKind { Pull, Slow, Teleport, Stun }

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Player Motion Effect")]
public sealed class SO_PlayerEffectEvent : ScriptableObject
{
    private event Action<PlayerEffectKind, Vector2, float> listeners;
    /// <summary>Registers a player-owned motion endpoint.</summary>
    public void RegisterListener(Action<PlayerEffectKind, Vector2, float> listener) { listeners += listener; }
    /// <summary>Unregisters a motion endpoint.</summary>
    public void UnregisterListener(Action<PlayerEffectKind, Vector2, float> listener) { listeners -= listener; }
    /// <summary>Requests pull velocity, temporary speed scaling, or a physics-step teleport.</summary>
    public void Raise(PlayerEffectKind kind, Vector2 value, float amount = 0f) { listeners?.Invoke(kind, value, amount); }
}
