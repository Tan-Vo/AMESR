using UnityEngine;
using UnityEngine.UI;

public enum CustomerState { Spawning, SeekingSeat, Thinking, WaitingForOrder, Eating, Paying, AngryLeaving }

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public sealed class CustomerAI : MonoBehaviour
{
    [SerializeField] private SpriteRenderer orderBubble;
    [SerializeField] private Image patienceMeter;
    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private CantinaManager cantina;
    private int seat = -1;
    private float timer;
    private float patience;
    private bool paid;
    public CustomerState State { get; private set; }
    public CustomerType Type { get; private set; }
    public SO_DishRecipe Order { get; private set; }
    public float PatienceFraction => patience / 20f;
    private void Awake() { body = GetComponent<Rigidbody2D>(); sprite = GetComponent<SpriteRenderer>(); }
    /// <summary>Resets a pooled customer at the entrance.</summary>
    public void Initialize(CantinaManager manager, CustomerType type, Sprite appearance)
    {
        cantina = manager; Type = type; sprite.sprite = appearance;
        body.position = manager.Entrance;
        seat = -1; paid = false; Order = null; patience = 20f;
        State = CustomerState.Spawning; timer = 0.5f;
    }
    private void FixedUpdate()
    {
        if (cantina == null || !cantina.IsRunning) return;
        float dt = Time.fixedDeltaTime;
        timer -= dt;
        switch (State)
        {
            case CustomerState.Spawning:
                if (timer <= 0f) { State = CustomerState.SeekingSeat; timer = 5f; }
                break;
            case CustomerState.SeekingSeat:
                if (seat < 0) seat = cantina.ReserveSeat(this);
                if (seat < 0) { if (timer <= 0f) LeaveAngry(); break; }
                if (Walk(cantina.SeatPosition(seat), 3f))
                { State = CustomerState.Thinking; timer = Random.Range(3f, 5f); }
                break;
            case CustomerState.Thinking:
                if (timer > 0f) break;
                Order = cantina.ChooseOrder();
                if (Order == null || !cantina.OrderDish(Order)) { LeaveAngry(); break; }
                State = CustomerState.WaitingForOrder;
                break;
            case CustomerState.WaitingForOrder:
                patience = ServiceMath.Patience(patience, Type, dt);
                if (patience <= 0f) LeaveAngry();
                break;
            case CustomerState.Eating:
                if (timer <= 0f) { State = CustomerState.Paying; cantina.Pay(this); paid = true; ReleaseSeat(); }
                break;
            case CustomerState.Paying:
            case CustomerState.AngryLeaving:
                if (Walk(cantina.Exit, State == CustomerState.AngryLeaving ? 5f : 3f)) cantina.ReturnCustomer(this);
                break;
        }
    }
    private bool Walk(Vector2 target, float speed)
    {
        body.MovePosition(Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime));
        return (body.position - target).sqrMagnitude < 0.04f;
    }
    /// <summary>Accepts the matching plate; a wrong dish is wasted and reduces patience by twenty percent.</summary>
    public bool Serve(SO_DishRecipe dish)
    {
        if (State != CustomerState.WaitingForOrder) return false;
        if (dish != Order) { patience = Mathf.Max(0f, patience - 4f); return false; }
        State = CustomerState.Eating; timer = 4f;
        return true;
    }
    /// <summary>Closes an unfinished visit when the night ends.</summary>
    public void Close()
    {
        if (!paid && State == CustomerState.Eating) { cantina.Pay(this); paid = true; }
        if (!paid && State != CustomerState.AngryLeaving) LeaveAngry();
        ReleaseSeat();
    }
    private void ReleaseSeat() { if (seat >= 0) { cantina.ReleaseSeat(seat, this); seat = -1; } }
    private void LeaveAngry()
    {
        if (State == CustomerState.AngryLeaving || paid) return;
        State = CustomerState.AngryLeaving;
        cantina.Angry(this);
        ReleaseSeat();
    }
    private void LateUpdate()
    {
        if (orderBubble != null) { orderBubble.enabled = State == CustomerState.WaitingForOrder; orderBubble.sprite = Order != null ? Order.DishIcon : null; }
        if (patienceMeter != null)
        {
            patienceMeter.enabled = State == CustomerState.WaitingForOrder;
            patienceMeter.fillAmount = PatienceFraction;
            patienceMeter.color = PatienceFraction > 0.5f ? Color.green : PatienceFraction > 0.2f ? Color.yellow : Color.red;
        }
        sprite.color = State == CustomerState.AngryLeaving ? new Color(1f, 0.4f, 0.4f) : Color.white;
    }
}
