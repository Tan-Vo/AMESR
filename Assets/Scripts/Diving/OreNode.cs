using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class OreNode : MonoBehaviour
{
    [SerializeField] private SO_DiveInteractionEvent onDiveInteraction;
    [SerializeField] private SO_LootSpawnEvent onLootSpawnRequested;
    [SerializeField] private SO_ItemData item;
    [SerializeField] private SO_GameEvent onMineCompleted;
    [SerializeField] private int quantity = 3;
    [SerializeField] private float miningTime = 2.5f;
    private SpriteRenderer sprite;
    public bool Depleted { get; private set; }
    public float MiningTime => Mathf.Max(0.1f, miningTime);
    private void Awake() { sprite = GetComponent<SpriteRenderer>(); }
    private void OnEnable() { onDiveInteraction?.Register(this); }
    private void OnDisable() { onDiveInteraction?.Unregister(this); }
    /// <summary>Depletes once and requests pooled resource drops.</summary>
    public void Deplete()
    {
        if (Depleted || item == null) return;
        Depleted = true;
        onLootSpawnRequested?.Raise(item, Mathf.Max(1, quantity), transform.position + Vector3.up * 0.5f);
        onMineCompleted?.Raise();
    }
    private void LateUpdate() { if (Depleted && sprite.color.a > 0.3f) sprite.color = new Color(0.4f, 0.4f, 0.4f, 0.3f); }
}
