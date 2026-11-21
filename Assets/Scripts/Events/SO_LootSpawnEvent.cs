using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/LootSpawnEvent")]
public sealed class SO_LootSpawnEvent : ScriptableObject
{
    private event Action<SO_ItemData, int, Vector3> raised;
    /// <summary>Publishes a pooled entity notification.</summary>
    public void Raise(SO_ItemData item, int quantity, Vector3 position) => raised?.Invoke(item, quantity, position);
    /// <summary>Registers a callback once.</summary>
    public void RegisterListener(Action<SO_ItemData, int, Vector3> listener) { raised -= listener; raised += listener; }
    /// <summary>Removes a callback.</summary>
    public void UnregisterListener(Action<SO_ItemData, int, Vector3> listener) { raised -= listener; }
    private void OnDisable() { raised = null; }
}
