using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/CreatureEvent")]
public sealed class SO_CreatureEvent : ScriptableObject
{
    private event Action<CreatureBase> raised;
    /// <summary>Publishes a pooled entity notification.</summary>
    public void Raise(CreatureBase creature) => raised?.Invoke(creature);
    /// <summary>Registers a callback once.</summary>
    public void RegisterListener(Action<CreatureBase> listener) { raised -= listener; raised += listener; }
    /// <summary>Removes a callback.</summary>
    public void UnregisterListener(Action<CreatureBase> listener) { raised -= listener; }
    private void OnDisable() { raised = null; }
}
