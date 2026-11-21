using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Item Collection")]
public sealed class SO_ItemCollectionEvent : ScriptableObject
{
    private Func<SO_ItemData, int, bool> collector;
    /// <summary>Requests atomic admission of an item stack into the active player's cargo.</summary>
    public bool TryCollect(SO_ItemData item, int amount) => collector?.Invoke(item, amount) == true;
    /// <summary>Registers the active scene's cargo receiver.</summary>
    public void RegisterListener(Func<SO_ItemData, int, bool> listener) { collector = listener; }
    /// <summary>Unregisters only the receiver that owns the registration.</summary>
    public void UnregisterListener(Func<SO_ItemData, int, bool> listener) { if (collector == listener) collector = null; }
    private void OnDisable() { collector = null; }
}
