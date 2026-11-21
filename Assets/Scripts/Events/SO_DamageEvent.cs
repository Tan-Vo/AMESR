using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Damage Request")]
public sealed class SO_DamageEvent : ScriptableObject
{
    private readonly List<DamageReceiver> receivers = new(128);
    /// <summary>Registers an entity-local damage endpoint.</summary>
    public void Register(DamageReceiver receiver) { if (!receivers.Contains(receiver)) receivers.Add(receiver); }
    /// <summary>Unregisters an endpoint when returned to a pool.</summary>
    public void Unregister(DamageReceiver receiver) { receivers.Remove(receiver); }
    /// <summary>Dispatches a hit to a matching collider, respecting target armor and tip effects.</summary>
    public bool Hit(Collider2D collider, int damage, float multiplier = 1f, HarpoonEffect effect = HarpoonEffect.Gravity)
    {
        for (int i = 0; i < receivers.Count; i++)
        {
            var receiver = receivers[i];
            if (receiver == null || receiver.Hitbox != collider || !receiver.IsAlive) continue;
            receiver.Receive(damage, multiplier, effect);
            if (effect == HarpoonEffect.ShockPulse)
            {
                Vector2 center = receiver.transform.position;
                for (int j = 0; j < receivers.Count; j++)
                    if (receivers[j] != null && ((Vector2)receivers[j].transform.position - center).sqrMagnitude <= 9f)
                        receivers[j].Stun(1f);
            }
            return true;
        }
        return false;
    }
    /// <summary>Damages each living enemy once inside a forward melee arc, blocked by terrain.</summary>
    public void Melee(Vector2 origin, Vector2 direction, float range, int damage)
    {
        for (int i = 0; i < receivers.Count; i++)
        {
            var receiver = receivers[i];
            if (receiver == null || !receiver.IsAlive || receiver.IsPlayer) continue;
            Vector2 delta = (Vector2)receiver.transform.position - origin;
            if (delta.sqrMagnitude > range * range || Vector2.Dot(direction.normalized, delta.normalized) < 0.5f) continue;
            if (Physics2D.Linecast(origin, receiver.transform.position, (1 << 9) | (1 << 10)).collider != null) continue;
            receiver.Receive(damage, 1f, HarpoonEffect.Gravity);
        }
    }
}
