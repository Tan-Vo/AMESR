using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/LootPickupEvent")]
public sealed class SO_LootPickupEvent : ScriptableObject
{
    private event Action<LootPickup> raised;
    /// <summary>Publishes a pooled entity notification.</summary>
    public void Raise(LootPickup pickup) => raised?.Invoke(pickup);
    /// <summary>Registers a callback once.</summary>
    public void RegisterListener(Action<LootPickup> listener) { raised -= listener; raised += listener; }
    /// <summary>Removes a callback.</summary>
    public void UnregisterListener(Action<LootPickup> listener) { raised -= listener; }
    private void OnDisable() { raised = null; }
}
