using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Weapon HUD")]
public sealed class SO_WeaponHUDEvent : ScriptableObject
{
    private event Action<SO_WeaponData, int, bool> listeners;
    /// <summary>Registers a HUD endpoint.</summary>
    public void RegisterListener(Action<SO_WeaponData, int, bool> listener) { listeners += listener; }
    /// <summary>Unregisters an endpoint.</summary>
    public void UnregisterListener(Action<SO_WeaponData, int, bool> listener) { listeners -= listener; }
    /// <summary>Publishes weapon and magazine changes.</summary>
    public void Raise(SO_WeaponData weapon, int ammo, bool reloading) { listeners?.Invoke(weapon, ammo, reloading); }
}
