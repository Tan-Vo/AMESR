using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public sealed class LootPickup : MonoBehaviour
{
    [SerializeField] private SO_ItemCollectionEvent onItemCollectionRequested;
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_LootPickupEvent onLootReleased;
    [SerializeField] private float maxLifetime = 30f;
    private SpriteRenderer sprite;
    private SO_ItemData item;
    private int quantity;
    private float remaining;
    private bool collecting;
    private string receipt;
    private void Awake() { sprite = GetComponent<SpriteRenderer>(); }
    /// <summary>Resets a pooled pickup at the drop position.</summary>
    public void Initialize(SO_ItemData definition, int amount, Vector3 position, string rewardReceipt = null)
    {
        item = definition;
        quantity = amount;
        transform.position = position;
        sprite.sprite = definition.Icon;
        remaining = maxLifetime;
        collecting = false;
        receipt = rewardReceipt;
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
        if (receipt != null && (session == null || RewardLedger.Find(session.Data, receipt) == null)) { onLootReleased?.Raise(this); return; }
        if (onItemCollectionRequested != null && onItemCollectionRequested.TryCollect(item, quantity))
        {
            if (receipt != null) RewardLedger.Collected(session.Data, receipt);
            onLootReleased?.Raise(this);
        }
        else collecting = false;
    }
}
