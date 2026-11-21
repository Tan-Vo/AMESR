using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public sealed class LootPickup : MonoBehaviour
{
    [SerializeField] private SO_ItemCollectionEvent onItemCollectionRequested;
    [SerializeField] private SO_LootPickupEvent onLootReleased;
    [SerializeField] private float maxLifetime = 30f;
    private SpriteRenderer sprite;
    private SO_ItemData item;
    private int quantity;
    private float remaining;
    private bool collecting;
    private void Awake() { sprite = GetComponent<SpriteRenderer>(); }
    /// <summary>Resets a pooled pickup at the drop position.</summary>
    public void Initialize(SO_ItemData definition, int amount, Vector3 position)
    {
        item = definition;
        quantity = amount;
        transform.position = position;
        sprite.sprite = definition.Icon;
        remaining = maxLifetime;
        collecting = false;
    }
    private void Update()
    {
        remaining -= Time.deltaTime;
        if (remaining <= 0f && !collecting) { collecting = true; onLootReleased?.Raise(this); }
    }
    private void OnTriggerEnter2D(Collider2D other) { Collect(other); }
    private void OnTriggerStay2D(Collider2D other) { Collect(other); }
    private void Collect(Collider2D other)
    {
        if (collecting || item == null || !other.CompareTag("Player")) return;
        collecting = true;
        if (onItemCollectionRequested != null && onItemCollectionRequested.TryCollect(item, quantity)) onLootReleased?.Raise(this);
        else collecting = false;
    }
}
