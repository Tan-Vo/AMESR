using System;
using UnityEngine;

[RequireComponent(typeof(PlayerStateMachine))]
public sealed class ServingSystem : MonoBehaviour
{
    [SerializeField] private CantinaManager cantina;
    [SerializeField] private KitchenQueue kitchen;
    [SerializeField] private Transform counter;
    [SerializeField] private SpriteRenderer traySprite;
    [SerializeField] private SO_SaveSession session;
    private readonly SO_DishRecipe[] tray = new SO_DishRecipe[3];
    private int count;
    private bool interact;
    private PlayerStateMachine states;
    private Func<SO_DishRecipe, int> pendingOrders;
    private void Awake() { states = GetComponent<PlayerStateMachine>(); pendingOrders = cantina.PendingOrders; }
    private void Update() { interact |= Input.GetKeyDown(KeyCode.E); }
    private void FixedUpdate()
    {
        if (states.CurrentState == PlayerState.Dead || states.CurrentState == PlayerState.Hurt) { interact = false; return; }
        count = KitchenQueueMath.PruneTray(tray, count, pendingOrders, cantina.IsRunning);
        states.ChangeState(count > 0 ? PlayerState.Waiter_Carrying : PlayerState.Waiter_Idle);
        if (!interact || !cantina.IsRunning) { interact = false; return; }
        interact = false;
        session.Data.UpgradeLevels.TryGetValue("SO_WaiterTray", out int level);
        int capacity = Mathf.Clamp(1 + level, 1, 3);
        var customer = cantina.Nearby(transform.position);
        if (customer != null && count > 0)
        {
            int slot = 0;
            for (int i = 0; i < count; i++) if (tray[i] == customer.Order) { slot = i; break; }
            var dish = tray[slot]; tray[slot] = tray[--count]; tray[count] = null;
            cantina.Serve(customer, dish);
            states.ChangeState(PlayerState.Waiter_Serving);
        }
        else if (((Vector2)transform.position - (Vector2)counter.position).sqrMagnitude < 2.25f)
        {
            while (count < capacity) { var dish = kitchen.PickUp(); if (dish == null) break; tray[count++] = dish; }
        }
        else cantina.ClearPlate(transform.position);
    }
    private void LateUpdate() { if (traySprite != null) { traySprite.enabled = count > 0; if (count > 0) traySprite.sprite = tray[count - 1].DishSprite; } }
}
