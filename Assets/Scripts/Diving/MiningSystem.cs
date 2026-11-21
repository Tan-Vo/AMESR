using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerStateMachine))]
public sealed class MiningSystem : MonoBehaviour
{
    [SerializeField] private SO_DiveInteractionEvent onDiveInteraction;
    [SerializeField] private float reach = 2.5f;
    [SerializeField] private Image progressBar;
    private PlayerStateMachine states;
    private OreNode target;
    private float progress;
    private bool held;
    private void Awake() { states = GetComponent<PlayerStateMachine>(); }
    private void Update() { held = Input.GetKey(KeyCode.E); }
    private void FixedUpdate()
    {
        bool available = states.IsLocomotion || states.CurrentState == PlayerState.Mining;
        var candidate = held && available && onDiveInteraction != null ? onDiveInteraction.NearestOre(transform.position, reach) : null;
        if (candidate != target) { target = candidate; progress = 0f; }
        if (target == null)
        {
            if (states.CurrentState == PlayerState.Mining) states.ChangeState(states.InWater ? PlayerState.Swimming : PlayerState.Idle);
            progress = 0f;
            return;
        }
        states.ChangeState(PlayerState.Mining);
        progress += Time.fixedDeltaTime / target.MiningTime;
        if (progress >= 1f) { onDiveInteraction.Mine(target); target = null; progress = 0f; states.ChangeState(states.InWater ? PlayerState.Swimming : PlayerState.Idle); }
    }
    private void LateUpdate()
    {
        if (progressBar == null) return;
        progressBar.enabled = target != null;
        progressBar.fillAmount = progress;
    }
}
