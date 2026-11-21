using UnityEngine;

public sealed class FlockManager : MonoBehaviour
{
    [SerializeField] private int flockSize = 30;
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float alignmentRadius = 3f;
    [SerializeField] private float cohesionRadius = 5f;
    [SerializeField] private float separationWeight = 2f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;
    [SerializeField] private float playerAvoidWeight = 3f;
    [SerializeField] private float playerAvoidRadius = 4f;
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float maxSteerForce = 2f;
    [SerializeField] private Transform player;
    private CreatureAI_Schooling[] members;
    private Transform[] boidTransforms;
    private Vector2[] positions;
    private Vector2[] velocities;
    private Vector2[] accelerations;
    private int count;
    private void Awake()
    {
        int size = Mathf.Clamp(flockSize, 1, 30);
        members = new CreatureAI_Schooling[size];
        boidTransforms = new Transform[size];
        positions = new Vector2[size];
        velocities = new Vector2[size];
        accelerations = new Vector2[size];
    }
    /// <summary>Adds a pooled schooling creature; returns false if the flock is full.</summary>
    public bool Register(CreatureAI_Schooling member)
    {
        for (int i = 0; i < count; i++) if (members[i] == member) return true;
        if (member == null || count == members.Length) return false;
        members[count] = member;
        boidTransforms[count] = member.transform;
        velocities[count] = Random.insideUnitCircle.normalized * Mathf.Min(maxSpeed, member.Data.MoveSpeed);
        count++;
        return true;
    }
    /// <summary>Removes a creature in constant time after locating it.</summary>
    public void Unregister(CreatureAI_Schooling member)
    {
        for (int i = 0; i < count; i++)
        {
            if (members[i] != member) continue;
            count--;
            members[i] = members[count];
            boidTransforms[i] = boidTransforms[count];
            velocities[i] = velocities[count];
            members[count] = null;
            boidTransforms[count] = null;
            return;
        }
    }
    private void FixedUpdate()
    {
        for (int i = 0; i < count; i++) positions[i] = boidTransforms[i].position;
        for (int i = 0; i < count; i++)
        {
            if (!members[i].IsAlive) { accelerations[i] = Vector2.zero; continue; }
            Vector2 separation = Vector2.zero, alignment = Vector2.zero, center = Vector2.zero;
            int aligned = 0, cohesive = 0;
            for (int j = 0; j < count; j++)
            {
                if (i == j || !members[j].IsAlive || members[i].Data != members[j].Data) continue;
                Vector2 away = positions[i] - positions[j];
                float square = away.sqrMagnitude;
                if (square < separationRadius * separationRadius)
                    separation += square > 0.0001f ? away / square : new Vector2(i < j ? -1f : 1f, 0f);
                if (square < alignmentRadius * alignmentRadius) { alignment += velocities[j]; aligned++; }
                if (square < cohesionRadius * cohesionRadius) { center += positions[j]; cohesive++; }
            }
            Vector2 steer = separation * separationWeight;
            if (aligned > 0) steer += (alignment / aligned - velocities[i]) * alignmentWeight;
            if (cohesive > 0) steer += (center / cohesive - positions[i]) * cohesionWeight;
            if (player != null)
            {
                Vector2 away = positions[i] - (Vector2)player.position;
                if (away.sqrMagnitude < playerAvoidRadius * playerAvoidRadius) steer += away.normalized * playerAvoidWeight;
            }
            accelerations[i] = Vector2.ClampMagnitude(steer, maxSteerForce);
        }
        for (int i = 0; i < count; i++)
        {
            velocities[i] = Vector2.ClampMagnitude(velocities[i] + accelerations[i] * Time.fixedDeltaTime, Mathf.Min(maxSpeed, members[i].Data.MoveSpeed));
            members[i].ApplyFlockVelocity(velocities[i]);
        }
    }
}
