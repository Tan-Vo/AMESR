using UnityEngine;

public sealed class CustomerSpawner : MonoBehaviour
{
    [SerializeField] private CustomerPool pool;
    [SerializeField] private Sprite[] appearances;
    private CantinaManager cantina;
    private float timer;
    public int Spawned { get; private set; }
    /// <summary>Starts a fresh sequence of four customer waves.</summary>
    public void Begin(CantinaManager manager) { cantina = manager; Spawned = 0; timer = 0.5f; }
    /// <summary>Returns a departing customer to its fixed pool.</summary>
    public void Return(CustomerAI customer) { pool.Return(customer); }
    private void FixedUpdate()
    {
        if (cantina == null || !cantina.IsRunning || Spawned >= 15) return;
        timer -= Time.fixedDeltaTime;
        if (timer > 0f) return;
        var customer = pool.Get();
        if (customer == null) return;
        float roll = Random.value;
        var type = Spawned < 3 ? CustomerType.Normal : Spawned < 7 ? (roll < 0.8f ? CustomerType.Normal : CustomerType.VIP) :
            roll < 0.7f ? CustomerType.Normal : roll < 0.9f ? CustomerType.VIP : CustomerType.Critic;
        customer.Initialize(cantina, type, appearances[Random.Range(0, appearances.Length)]);
        cantina.RegisterCustomer(customer);
        Spawned++;
        timer = Spawned >= 12 ? 4f : Mathf.Max(4.5f, 8f - (cantina.Day - 1) * 0.2f);
    }
}
