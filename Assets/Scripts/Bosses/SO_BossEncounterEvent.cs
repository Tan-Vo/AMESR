using System;
using UnityEngine;

public enum BossKind { AbyssalMaw, ChronoJellyfish, VoidLeviathan }

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Boss Encounter")]
public sealed class SO_BossEncounterEvent : ScriptableObject
{
    private event Action requested;
    public bool InProgress { get; private set; }
    public BossKind Kind { get; private set; }
    public Transform Player { get; private set; }
    public Vector2 ReturnPosition { get; private set; }
    public Vector2 Center { get; private set; }
    /// <summary>Registers the dive scene's additive-arena loader.</summary>
    public void RegisterListener(Action listener) { requested += listener; }
    /// <summary>Unregisters a scene loader.</summary>
    public void UnregisterListener(Action listener) { requested -= listener; }
    /// <summary>Starts one encounter only when a loader is available.</summary>
    public bool Request(BossKind kind, Transform player)
    {
        if (InProgress || player == null || requested == null) return false;
        InProgress = true; Kind = kind; Player = player; ReturnPosition = player.position; Center = ReturnPosition + Vector2.right * 1000f;
        requested.Invoke();
        return true;
    }
    /// <summary>Clears the transient encounter context after unloading.</summary>
    public void Clear() { InProgress = false; Player = null; }
    /// <summary>Starts a directly opened arena without asking a dive loader to reload it.</summary>
    public bool BeginStandalone(BossKind kind, Transform player, Vector2 center)
    {
        if (InProgress || player == null || !Enum.IsDefined(typeof(BossKind), kind)) return false;
        InProgress = true; Kind = kind; Player = player; ReturnPosition = player.position; Center = center;
        return true;
    }
}
