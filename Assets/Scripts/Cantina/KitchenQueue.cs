using System.Collections.Generic;
using UnityEngine;

public sealed class KitchenQueue : MonoBehaviour
{
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SpriteRenderer readyDish;
    [SerializeField] private SO_GameEvent onDishReady;
    private readonly Queue<SO_DishRecipe> orders = new(32);
    private readonly Queue<SO_DishRecipe> ready = new(32);
    private readonly SO_DishRecipe[] cooking = new SO_DishRecipe[3];
    private readonly float[] remaining = new float[3];
    public bool Running { get; set; }
    /// <summary>Queues a reserved serving in FIFO order.</summary>
    public void Enqueue(SO_DishRecipe recipe) { if (recipe != null) orders.Enqueue(recipe); }
    /// <summary>Removes one completed dish for the waiter's tray.</summary>
    public SO_DishRecipe PickUp() => ready.Count > 0 ? ready.Dequeue() : null;
    /// <summary>Discards one abandoned order at whichever kitchen stage still owns a matching plate.</summary>
    public void Cancel(SO_DishRecipe recipe)
    {
        if (KitchenQueueMath.Cancel(orders, recipe)) return;
        for (int i = 0; i < cooking.Length; i++) if (cooking[i] == recipe) { cooking[i] = null; remaining[i] = 0f; return; }
        KitchenQueueMath.Cancel(ready, recipe);
    }
    /// <summary>Clears unfinished and ready orders between nights.</summary>
    public void ResetQueue() { orders.Clear(); ready.Clear(); System.Array.Clear(cooking, 0, cooking.Length); }
    private void Update()
    {
        if (!Running || session == null) return;
        int cooks = session.Data.ReputationPoints >= 100 ? 3 : session.Data.ReputationPoints >= 30 ? 2 : 1;
        session.Data.UpgradeLevels.TryGetValue("SO_KitchenSpeed", out int level);
        for (int i = 0; i < cooking.Length; i++)
        {
            if (cooking[i] == null && i < cooks && orders.Count > 0)
            { cooking[i] = orders.Dequeue(); remaining[i] = Mathf.Max(0.1f, cooking[i].CookTime / (1f + level * 0.15f)); }
            if (cooking[i] == null) continue;
            remaining[i] -= Time.deltaTime;
            if (remaining[i] <= 0f) { ready.Enqueue(cooking[i]); cooking[i] = null; onDishReady?.Raise(); }
        }
    }
    private void LateUpdate()
    {
        if (readyDish == null) return;
        readyDish.enabled = ready.Count > 0;
        if (ready.Count > 0) readyDish.sprite = ready.Peek().DishSprite;
    }
}
