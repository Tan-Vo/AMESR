using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(DistanceJoint2D), typeof(PlayerStateMachine))]
public sealed class GrappleSystem : MonoBehaviour
{
    [SerializeField] private float grappleSpeed = 20f;
    [SerializeField] private float maxGrappleDistance = 12f;
    [SerializeField] private LayerMask grappleLayer;
    [SerializeField] private LineRenderer grappleLine;
    private Rigidbody2D body;
    private DistanceJoint2D joint;
    private PlayerStateMachine states;
    private Vector2 aim;
    private Vector2 anchor;
    private float steering;
    private bool pressed;
    private bool held;
    private bool attached;
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>(); joint = GetComponent<DistanceJoint2D>(); states = GetComponent<PlayerStateMachine>();
        joint.enabled = false;
        joint.autoConfigureConnectedAnchor = false;
        joint.autoConfigureDistance = false;
        joint.maxDistanceOnly = true;
    }
    private void Update()
    {
        pressed |= Input.GetButtonDown("Fire2");
        held = Input.GetButton("Fire2");
        steering = Input.GetAxisRaw("Horizontal");
        var camera = Camera.main;
        if (camera != null) aim = ((Vector2)camera.ScreenToWorldPoint(Input.mousePosition) - body.position).normalized;
    }
    private void FixedUpdate()
    {
        if (states.MovementLocked || !held) Release();
        else if (pressed && !attached && states.IsLocomotion)
        {
            var hit = Physics2D.Raycast(body.position, aim, maxGrappleDistance, grappleLayer);
            if (hit.collider != null && hit.collider.CompareTag("GrapplePoint"))
            {
                anchor = hit.point;
                joint.connectedAnchor = anchor;
                joint.distance = Vector2.Distance(body.position, anchor);
                joint.enabled = attached = true;
                states.ChangeState(PlayerState.GrappleSwinging);
            }
        }
        pressed = false;
        if (!attached) return;
        Vector2 radial = (body.position - anchor).normalized;
        Vector2 tangent = new Vector2(-radial.y, radial.x);
        body.AddForce(tangent * steering * grappleSpeed);
        if (states.InWater) body.AddForce(Vector2.down * body.mass * 3f);
    }
    private void Release()
    {
        if (joint != null) joint.enabled = false;
        attached = false;
        if (states != null && states.CurrentState == PlayerState.GrappleSwinging)
            states.ChangeState(states.InWater ? PlayerState.Swimming : PlayerState.Falling);
    }
    private void OnDisable() { Release(); }
    private void LateUpdate()
    {
        if (grappleLine == null) return;
        grappleLine.enabled = attached;
        if (attached) { grappleLine.SetPosition(0, transform.position); grappleLine.SetPosition(1, anchor); }
    }
}
