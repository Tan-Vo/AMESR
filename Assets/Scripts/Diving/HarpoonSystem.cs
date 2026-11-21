using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerStateMachine), typeof(Rigidbody2D))]
public sealed class HarpoonSystem : MonoBehaviour
{
    [SerializeField] private SO_WeaponData equippedHarpoon;
    [SerializeField] private Transform firePoint;
    [SerializeField] private LineRenderer cableRenderer;
    [SerializeField] private float maxCableLength = 15f;
    [SerializeField] private float reelBaseSpeed = 3f;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private HarpoonPool harpoonPool;
    [SerializeField] private SO_DiveInteractionEvent onDiveInteraction;
    [SerializeField] private SO_GameEvent onCableSnapped;
    [SerializeField] private Image tensionMeter;
    private PlayerStateMachine states;
    private Rigidbody2D body;
    private HarpoonProjectile tip;
    private CaptureTarget hooked;
    private Action<HarpoonProjectile, Collider2D> hitCallback;
    private Vector2 aim = Vector2.right;
    private bool fire;
    private bool held;
    private bool tapped;
    private bool selected = true;
    private float tension;
    private float tugTimer;
    private float tugDuration;
    private float cooldown;
    public bool IsBusy => tip != null || hooked != null;
    private void Awake() { states = GetComponent<PlayerStateMachine>(); body = GetComponent<Rigidbody2D>(); hitCallback = OnHit; }
    /// <summary>Enables harpoon input when selected by the weapon loadout.</summary>
    public void SetSelected(bool value) { selected = value; if (!value) Cancel(); }
    private void Update()
    {
        held = Input.GetButton("Fire1");
        bool pressed = Input.GetButtonDown("Fire1");
        if (selected && pressed) { if (hooked != null) tapped = true; else fire = true; }
        var camera = Camera.main;
        if (camera != null) aim = ((Vector2)camera.ScreenToWorldPoint(Input.mousePosition) - (Vector2)firePoint.position).normalized;
    }
    private void FixedUpdate()
    {
        cooldown = Mathf.Max(0f, cooldown - Time.fixedDeltaTime);
        if (!selected || states.MovementLocked || states.CurrentState == PlayerState.GrappleSwinging)
        { fire = tapped = false; Cancel(); return; }
        if (fire && !IsBusy && cooldown <= 0f && harpoonPool != null && equippedHarpoon != null)
        {
            tip = harpoonPool.Get();
            if (tip != null)
            {
                tip.Launch(firePoint.position, aim, equippedHarpoon.ProjectileSpeed, maxCableLength, targetMask, equippedHarpoon.ProjectileSprite, hitCallback);
                states.ChangeState(PlayerState.HarpoonAiming);
                cooldown = 1f / Mathf.Max(0.1f, equippedHarpoon.FireRate);
            }
        }
        fire = false;
        if (tip != null && Vector2.Distance(body.position, tip.Position) > maxCableLength) { Snap(); return; }
        if (hooked == null) return;
        if (!hooked.IsHooked || Vector2.Distance(body.position, hooked.Position) > maxCableLength) { Snap(); return; }
        tugTimer -= Time.fixedDeltaTime;
        if (tugTimer <= 0f) { tugTimer = UnityEngine.Random.Range(0.8f, 1.8f); tugDuration = 0.25f; }
        bool tug = tugDuration > 0f;
        tugDuration -= Time.fixedDeltaTime;
        tension = TensionMath.Step(tension, held, tug, tapped, Time.fixedDeltaTime);
        if (tension >= 1f) { Snap(); return; }
        float speed = held ? TensionMath.ReelSpeed(reelBaseSpeed, body.mass, hooked.Mass, tension, tapped) : 0f;
        Vector2 away = (hooked.Position - body.position).normalized;
        onDiveInteraction.Pull(hooked, hooked.Position + away * ((tug ? 1.2f : 0f) - speed) * Time.fixedDeltaTime);
        tapped = false;
        if (Vector2.Distance(body.position, hooked.Position) <= 1f)
        {
            onDiveInteraction.Catch(hooked);
            Cancel();
        }
    }
    private void OnHit(HarpoonProjectile projectile, Collider2D collider)
    {
        if (projectile != tip) return;
        hooked = onDiveInteraction != null ? onDiveInteraction.Resolve(collider) : null;
        harpoonPool.Return(tip);
        tip = null;
        if (hooked != null && onDiveInteraction.Hook(hooked))
        { tension = 0.4f; tugTimer = 0.5f; tugDuration = 0f; states.ChangeState(PlayerState.HarpoonReeling); }
        else { hooked = null; RestoreState(); }
    }
    private void Snap() { onCableSnapped?.Raise(); Cancel(); }
    private void Cancel()
    {
        if (tip != null && harpoonPool != null) harpoonPool.Return(tip);
        tip = null;
        if (hooked != null) onDiveInteraction?.Release(hooked);
        hooked = null;
        tension = 0f;
        RestoreState();
    }
    private void RestoreState()
    {
        if (states != null && (states.CurrentState == PlayerState.HarpoonAiming || states.CurrentState == PlayerState.HarpoonReeling))
            states.ChangeState(states.InWater ? PlayerState.Swimming : PlayerState.Idle);
    }
    private void OnDisable() { Cancel(); }
    private void LateUpdate()
    {
        if (cableRenderer != null)
        {
            cableRenderer.enabled = IsBusy;
            if (IsBusy) { cableRenderer.SetPosition(0, firePoint.position); cableRenderer.SetPosition(1, hooked != null ? (Vector3)hooked.Position : (Vector3)tip.Position); }
        }
        if (tensionMeter != null)
        {
            tensionMeter.enabled = hooked != null;
            tensionMeter.fillAmount = tension;
            tensionMeter.color = tension >= 0.7f ? Color.red : tension >= 0.4f ? Color.green : Color.cyan;
        }
    }
}
