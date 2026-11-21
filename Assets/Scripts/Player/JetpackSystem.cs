using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerStats), typeof(PlayerStateMachine))]
public sealed class JetpackSystem : MonoBehaviour
{
    [SerializeField] private float jetpackForce = 15f;
    [SerializeField] private float maxUpwardVelocity = 8f;
    [SerializeField] private float fuelBurnRate = 10f;
    [SerializeField] private float overheatThreshold = 4f;
    [SerializeField] private float cooldownDuration = 2f;
    [SerializeField] private ParticleSystem thrusterParticles;
    [SerializeField] private SO_GameEvent onJetpackOverheated;
    private Rigidbody2D rb;
    private PlayerStats stats;
    private PlayerStateMachine stateMachine;
    private bool held;
    private float continuousUseTimer;
    private float cooldown;
    public bool IsJetpacking { get; private set; }
    public bool IsOverheated => cooldown > 0f;
    private void Awake() { rb = GetComponent<Rigidbody2D>(); stats = GetComponent<PlayerStats>(); stateMachine = GetComponent<PlayerStateMachine>(); }
    private void Update() { held = Input.GetKey(KeyCode.LeftShift); }
    private void FixedUpdate()
    {
        if (cooldown > 0f) { cooldown = Mathf.Max(0f, cooldown - Time.fixedDeltaTime); continuousUseTimer = 0f; }
        IsJetpacking = held && !IsOverheated && stats.CurrentFuel > 0f && !stateMachine.MovementLocked && !stats.IsDead();
        if (!IsJetpacking) { if (!held) continuousUseTimer = 0f; return; }
        rb.linearVelocity = rb.linearVelocity.WithY(Mathf.Min(rb.linearVelocity.y + jetpackForce / Mathf.Max(0.01f, rb.mass) * Time.fixedDeltaTime, maxUpwardVelocity));
        stats.ConsumeFuel(fuelBurnRate * Time.fixedDeltaTime);
        continuousUseTimer += Time.fixedDeltaTime;
        if (continuousUseTimer >= overheatThreshold)
        {
            cooldown = cooldownDuration;
            IsJetpacking = false;
            onJetpackOverheated?.Raise();
        }
    }
    private void LateUpdate()
    {
        if (thrusterParticles == null) return;
        if (IsJetpacking && !thrusterParticles.isPlaying) thrusterParticles.Play();
        else if (!IsJetpacking && thrusterParticles.isPlaying) thrusterParticles.Stop();
    }
}
