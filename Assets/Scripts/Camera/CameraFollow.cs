using UnityEngine;

[DefaultExecutionOrder(200)]
public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 deadZone = new Vector2(1, 1);
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSpeed = 3f;
    [SerializeField] private Vector2 offset = new Vector2(0, 1);
    [SerializeField] private float explorationZoom = 7f;
    [SerializeField] private float swimmingZoom = 6f;
    [SerializeField] private SO_IntEvent onStateEntered;
    [SerializeField] private SO_IntEvent onWaterStateChanged;
    [SerializeField] private SO_BossEncounterEvent onBossEncounterRequested;
    private Camera view;
    private Vector3 position;
    private Vector2 previousTarget;
    private float ahead;
    private bool underwater;
    private GameState state;
    private void Awake() { view = GetComponent<Camera>(); position = transform.position; }
    private void Start() { if (target != null) { previousTarget = target.position; position = new Vector3(target.position.x, target.position.y + offset.y, -10); } }
    private void OnEnable() { onStateEntered?.RegisterListener(State); onWaterStateChanged?.RegisterListener(Water); }
    private void OnDisable() { onStateEntered?.UnregisterListener(State); onWaterStateChanged?.UnregisterListener(Water); }
    private void State(int value) { if ((GameState)value != GameState.Paused) state = (GameState)value; }
    private void Water(int value) { underwater = value != 0; }
    private void LateUpdate()
    {
        if (state == GameState.BossFight && onBossEncounterRequested != null && onBossEncounterRequested.InProgress)
            position = new Vector3(onBossEncounterRequested.Center.x, onBossEncounterRequested.Center.y, -10);
        else if (target != null)
        {
            Vector2 current = target.position;
            float movement = current.x - previousTarget.x;
            if (Mathf.Abs(movement) > 0.001f) ahead = Mathf.MoveTowards(ahead, Mathf.Sign(movement) * lookAheadDistance, lookAheadSpeed * Time.deltaTime);
            Vector2 desired = current + offset + Vector2.right * ahead;
            Vector2 error = desired - (Vector2)position;
            Vector2 correction = new Vector2(Mathf.Sign(error.x) * Mathf.Max(0f, Mathf.Abs(error.x) - deadZone.x), Mathf.Sign(error.y) * Mathf.Max(0f, Mathf.Abs(error.y) - deadZone.y));
            position += (Vector3)correction * (1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
            if (error.sqrMagnitude > 2500f) position = new Vector3(desired.x, desired.y, -10);
            previousTarget = current;
        }
        transform.SetPositionAndRotation(position, Quaternion.identity);
        float zoom = state == GameState.BossFight ? 9f : state == GameState.CantinaOpen || state == GameState.CantinaPrep || state == GameState.NightShop ? 5f : underwater ? swimmingZoom : explorationZoom;
        view.orthographicSize = Mathf.MoveTowards(view.orthographicSize, zoom, Time.unscaledDeltaTime * 5f);
    }
}
