using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Healing Request")]
public sealed class SO_HealingRequestEvent : ScriptableObject
{
    private Func<int, bool> receiver;
    /// <summary>Requests healing and reports whether health was restored.</summary>
    public bool TryHeal(int amount) => receiver?.Invoke(amount) ?? false;
    /// <summary>Registers the active player's healing endpoint.</summary>
    public void Register(Func<int, bool> handler) { receiver = handler; }
    /// <summary>Unregisters an endpoint without clearing a newer player.</summary>
    public void Unregister(Func<int, bool> handler) { if (receiver == handler) receiver = null; }
    private void OnDisable() { receiver = null; }
}
