using System.Collections.Generic;
using UnityEngine;

public sealed class CantinaManager : MonoBehaviour
{
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_IngredientStock stock;
    [SerializeField] private SO_NightReport report;
    [SerializeField] private KitchenQueue kitchen;
    [SerializeField] private CustomerSpawner spawner;
    [SerializeField] private Transform[] seats;
    [SerializeField] private Transform entranceDoor;
    [SerializeField] private Transform exitDoor;
    [SerializeField] private SO_IntEvent onStateEntered;
    [SerializeField] private SO_IntEvent onStateRequested;
    [SerializeField] private SO_IntEvent onCreditsAdded;
    [SerializeField] private SO_IntEvent onReputationRequested;
    [SerializeField] private SO_GameEvent onNightEnded;
    [SerializeField] private SO_GameEvent onDishServed;
    [SerializeField] private SO_GameEvent onCustomerLeft;
    [SerializeField] private SO_FloatingTextEvent onFloatingTextRequested;
    private readonly List<SO_DishRecipe> menu = new(5);
    private readonly Dictionary<SO_DishRecipe, int> batchRemainders = new();
    private readonly Dictionary<SO_DishRecipe, int> dishSales = new();
    private readonly CustomerAI[] active = new CustomerAI[15];
    private CustomerAI[] occupants;
    private float[] dirty;
    private int activeCount, served, angry, revenue, tips;
    public bool IsRunning { get; private set; }
    public bool HasReport { get; private set; }
    public Vector2 Entrance => entranceDoor.position;
    public Vector2 Exit => exitDoor.position;
    public int Day => session != null ? session.Data.CurrentDay : 1;
    public SO_NightReport Report => report;
    private void Awake() { occupants = new CustomerAI[seats.Length]; dirty = new float[seats.Length]; }
    private void Start() { if (!IsRunning) onStateRequested?.Raise((int)GameState.CantinaPrep); }
    private void OnEnable() { onStateEntered?.RegisterListener(StateEntered); onNightEnded?.RegisterListener(EndNight); }
    private void OnDisable() { onStateEntered?.UnregisterListener(StateEntered); onNightEnded?.UnregisterListener(EndNight); }
    private void StateEntered(int state)
    {
        if ((GameState)state == GameState.CantinaOpen && !IsRunning)
        { if (menu.Count == 0) onStateRequested?.Raise((int)GameState.CantinaPrep); else BeginNight(); }
    }
    /// <summary>Validates and replaces the pre-service menu.</summary>
    public bool SetMenu(IReadOnlyList<SO_DishRecipe> selected)
    {
        if (IsRunning || selected == null || selected.Count == 0 || selected.Count > 5) return false;
        for (int i = 0; i < selected.Count; i++) if (stock.Servings(selected[i]) <= 0) return false;
        menu.Clear();
        for (int i = 0; i < selected.Count; i++) if (!menu.Contains(selected[i])) menu.Add(selected[i]);
        return true;
    }
    /// <summary>Opens service once menu selection is valid.</summary>
    public void Open() { if (!IsRunning && menu.Count > 0) { BeginNight(); onStateRequested?.Raise((int)GameState.CantinaOpen); } }
    private void BeginNight()
    {
        if (IsRunning) return;
        served = angry = revenue = tips = activeCount = 0;
        batchRemainders.Clear(); dishSales.Clear();
        kitchen.ResetQueue(); kitchen.Running = IsRunning = true; HasReport = false;
        spawner.Begin(this);
    }
    /// <summary>Tracks an active pooled visit.</summary>
    public void RegisterCustomer(CustomerAI customer) { if (activeCount < active.Length) active[activeCount++] = customer; }
    /// <summary>Reserves the nearest unlocked, clean, unoccupied seat.</summary>
    public int ReserveSeat(CustomerAI customer)
    {
        int found = -1; float distance = float.MaxValue;
        int capacity = Mathf.Min(seats.Length, ServiceMath.Seats(session.Data.ReputationPoints));
        session.Data.UpgradeLevels.TryGetValue("SO_CantinaSeats", out int extra);
        capacity = Mathf.Min(seats.Length, capacity + extra);
        for (int i = 0; i < capacity; i++)
        {
            float candidate = (seats[i].position - customer.transform.position).sqrMagnitude;
            if (occupants[i] == null && dirty[i] <= 0f && candidate < distance) { found = i; distance = candidate; }
        }
        if (found >= 0) occupants[found] = customer;
        return found;
    }
    /// <summary>Returns the reserved table position.</summary>
    public Vector2 SeatPosition(int seat) => seats[seat].position;
    /// <summary>Frees a seat and schedules automatic plate cleanup.</summary>
    public void ReleaseSeat(int seat, CustomerAI owner) { if (occupants[seat] == owner) { occupants[seat] = null; dirty[seat] = 3f; } }
    /// <summary>Picks a dish that still has ingredients or reserved batch servings.</summary>
    public SO_DishRecipe ChooseOrder()
    {
        if (menu.Count == 0) return null;
        int start = Random.Range(0, menu.Count);
        for (int i = 0; i < menu.Count; i++)
        {
            var recipe = menu[(start + i) % menu.Count];
            batchRemainders.TryGetValue(recipe, out int amount);
            if (amount > 0 || stock.Servings(recipe) > 0) return recipe;
        }
        return null;
    }
    /// <summary>Reserves stock atomically before queuing a plate.</summary>
    public bool OrderDish(SO_DishRecipe recipe)
    {
        batchRemainders.TryGetValue(recipe, out int amount);
        if (amount <= 0) { if (!stock.Consume(recipe)) return false; amount = Mathf.Max(1, recipe.ServingsPerCook); }
        batchRemainders[recipe] = amount - 1;
        kitchen.Enqueue(recipe);
        return true;
    }
    /// <summary>Finds a waiting customer within interaction range.</summary>
    public CustomerAI Nearby(Vector2 position)
    {
        CustomerAI found = null; float square = 2.25f;
        for (int i = 0; i < activeCount; i++)
        {
            float candidate = ((Vector2)active[i].transform.position - position).sqrMagnitude;
            if (active[i].State == CustomerState.WaitingForOrder && candidate < square) { square = candidate; found = active[i]; }
        }
        return found;
    }
    /// <summary>Delivers a plate and publishes successful service.</summary>
    public bool Serve(CustomerAI customer, SO_DishRecipe recipe)
    {
        bool accepted = customer != null && customer.Serve(recipe);
        if (accepted) onDishServed?.Raise();
        return accepted;
    }
    /// <summary>Records revenue, tips, and the customer-specific reputation award once.</summary>
    public void Pay(CustomerAI customer)
    {
        int price = Mathf.Max(0, customer.Order.BasePrice);
        int tip = ServiceMath.Tip(price, customer.PatienceFraction, customer.Order.TipMultiplierPercent);
        served++; revenue += price; tips += tip;
        dishSales.TryGetValue(customer.Order, out int sales); dishSales[customer.Order] = sales + price;
        onCreditsAdded?.Raise(price + tip);
        onReputationRequested?.Raise(ServiceMath.Reputation(customer.Type));
        onFloatingTextRequested?.Raise(customer.transform.position, "+" + (price + tip) + " Credits", Color.yellow);
    }
    /// <summary>Penalizes an angry departure using the reputation system's two-point rule.</summary>
    public void Angry(CustomerAI customer)
    {
        angry++; onReputationRequested?.Raise(-2);
        onFloatingTextRequested?.Raise(customer.transform.position, "-★", Color.red);
    }
    /// <summary>Returns a finished visit and closes service when all waves have departed.</summary>
    public void ReturnCustomer(CustomerAI customer)
    {
        for (int i = 0; i < activeCount; i++) if (active[i] == customer) { active[i] = active[--activeCount]; active[activeCount] = null; break; }
        spawner.Return(customer); onCustomerLeft?.Raise();
        if (IsRunning && spawner.Spawned >= 15 && activeCount == 0) { onStateRequested?.Raise((int)GameState.NightShop); onNightEnded?.Raise(); }
    }
    private void Update() { for (int i = 0; i < dirty.Length; i++) dirty[i] = Mathf.Max(0f, dirty[i] - Time.deltaTime); }
    private void EndNight()
    {
        if (!IsRunning) return;
        for (int i = activeCount - 1; i >= 0; i--) { active[i].Close(); spawner.Return(active[i]); active[i] = null; }
        activeCount = 0; kitchen.Running = IsRunning = false; HasReport = true;
        string best = "None"; int highest = 0;
        foreach (var sale in dishSales) if (sale.Value > highest) { best = sale.Key.DishName; highest = sale.Value; }
        report.Capture(served, angry, revenue, tips, session.Data.ReputationStars, best);
    }
    /// <summary>Clears a nearby dirty table early for a small credit bonus.</summary>
    public bool ClearPlate(Vector2 position)
    {
        for (int i = 0; i < seats.Length; i++)
            if (dirty[i] > 0f && ((Vector2)seats[i].position - position).sqrMagnitude < 2.25f)
            { dirty[i] = 0f; onCreditsAdded?.Raise(1); return true; }
        return false;
    }
}
