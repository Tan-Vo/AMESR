using UnityEngine;

[RequireComponent(typeof(CreatureBase), typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class CaptureTarget : MonoBehaviour
{
    [SerializeField] private SO_DiveInteractionEvent onDiveInteraction;
    [SerializeField] private SO_ItemCollectionEvent onItemCollectionRequested;
    [SerializeField] private SO_CatchCatalog catalog;
    private CreatureBase creature;
    private Rigidbody2D body;
    public Collider2D Hitbox { get; private set; }
    public bool IsAvailable => creature != null && creature.IsAlive && !creature.IsHooked;
    public bool IsAlive => isActiveAndEnabled && creature != null && creature.IsAlive;
    public bool IsHooked => IsAlive && creature.IsHooked;
    public float Mass => creature.Data.Mass;
    public Vector2 Position => body.position;
    private void Awake() { creature = GetComponent<CreatureBase>(); body = GetComponent<Rigidbody2D>(); Hitbox = GetComponent<Collider2D>(); }
    private void OnEnable() { onDiveInteraction?.Register(this); }
    private void OnDisable() { onDiveInteraction?.Unregister(this); if (creature != null) creature.SetHooked(false); }
    /// <summary>Suspends or restores the creature's own steering.</summary>
    public bool SetHooked(bool hooked)
    {
        if (hooked && !IsAvailable) return false;
        creature.SetHooked(hooked);
        return true;
    }
    /// <summary>Repositions the hooked body from the harpoon physics step.</summary>
    public void Pull(Vector2 position) { if (IsAlive && creature.IsHooked) { body.linearVelocity = Vector2.zero; body.MovePosition(position); } }
    /// <summary>Atomically adds a species item, leaving the creature alive if cargo is full.</summary>
    public bool TryCapture()
    {
        if (!IsAlive || !creature.IsHooked || catalog == null || onItemCollectionRequested == null) return false;
        var item = catalog.Find(creature.Data);
        if (item == null || !onItemCollectionRequested.TryCollect(item, 1)) return false;
        creature.OnCaught();
        return true;
    }
}
