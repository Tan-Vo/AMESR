using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Quest Progress")]
public sealed class SO_QuestProgressEvent : ScriptableObject
{
    private event Action<ObjectiveType, string, int> listeners;
    /// <summary>Registers a quest progress endpoint.</summary>
    public void RegisterListener(Action<ObjectiveType, string, int> listener) { listeners += listener; }
    /// <summary>Unregisters an endpoint.</summary>
    public void UnregisterListener(Action<ObjectiveType, string, int> listener) { listeners -= listener; }
    /// <summary>Publishes a successful gameplay action, not an attempted one.</summary>
    public void Raise(ObjectiveType type, string target, int amount) { listeners?.Invoke(type, target, amount); }
}
