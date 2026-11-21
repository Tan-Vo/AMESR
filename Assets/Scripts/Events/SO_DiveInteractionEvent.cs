using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Dive Interactions")]
public sealed class SO_DiveInteractionEvent : ScriptableObject
{
    private readonly List<CaptureTarget> creatures = new(96);
    private readonly List<OreNode> ores = new(64);
    /// <summary>Registers an active capture endpoint.</summary>
    public void Register(CaptureTarget target) { if (!creatures.Contains(target)) creatures.Add(target); }
    /// <summary>Unregisters a capture endpoint.</summary>
    public void Unregister(CaptureTarget target) { creatures.Remove(target); }
    /// <summary>Registers an ore endpoint.</summary>
    public void Register(OreNode ore) { if (!ores.Contains(ore)) ores.Add(ore); }
    /// <summary>Unregisters an ore endpoint.</summary>
    public void Unregister(OreNode ore) { ores.Remove(ore); }
    /// <summary>Resolves a physics hit without cross-system component lookups.</summary>
    public CaptureTarget Resolve(Collider2D collider)
    {
        for (int i = 0; i < creatures.Count; i++)
            if (creatures[i] != null && creatures[i].Hitbox == collider && creatures[i].IsAvailable) return creatures[i];
        return null;
    }
    /// <summary>Finds the nearest unmined ore within reach.</summary>
    public OreNode NearestOre(Vector2 position, float range)
    {
        OreNode found = null;
        float square = range * range;
        for (int i = 0; i < ores.Count; i++)
        {
            var ore = ores[i];
            if (ore == null || ore.Depleted) continue;
            float distance = ((Vector2)ore.transform.position - position).sqrMagnitude;
            if (distance <= square) { square = distance; found = ore; }
        }
        return found;
    }
    /// <summary>Requests exclusive control of a creature until release.</summary>
    public bool Hook(CaptureTarget target) => target != null && target.SetHooked(true);
    /// <summary>Returns control to creature AI.</summary>
    public void Release(CaptureTarget target) { if (target != null) target.SetHooked(false); }
    /// <summary>Moves a hooked creature during a physics step.</summary>
    public void Pull(CaptureTarget target, Vector2 position) { if (target != null) target.Pull(position); }
    /// <summary>Admits a live catch to inventory before marking it caught.</summary>
    public bool Catch(CaptureTarget target) => target != null && target.TryCapture();
    /// <summary>Requests ore depletion and loot emission.</summary>
    public void Mine(OreNode ore) { if (ore != null) ore.Deplete(); }
}
