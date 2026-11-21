using System.Collections.Generic;
using UnityEngine;

public enum PlayerState { Idle, Walking, Running, Jumping, Falling, Landing, Swimming, Diving, Jetpacking, Mining, HarpoonAiming, HarpoonReeling, Combat_Melee, Combat_Ranged, Hurt, Dead, Interacting, Waiter_Idle, Waiter_Carrying, Waiter_Serving, Climbing, GrappleSwinging }

public interface IPlayerState
{
    /// <summary>Enters the state.</summary>
    void Enter();
    /// <summary>Runs frame logic.</summary>
    void Execute();
    /// <summary>Runs physics-step logic.</summary>
    void FixedExecute();
    /// <summary>Leaves the state.</summary>
    void Exit();
}

public sealed class PlayerStateMachine : MonoBehaviour
{
    private sealed class State : IPlayerState
    {
        private readonly PlayerStateMachine owner;
        private readonly PlayerState value;
        private float remaining;
        internal State(PlayerStateMachine owner, PlayerState value) { this.owner = owner; this.value = value; }
        /// <inheritdoc />
        public void Enter() { remaining = value == PlayerState.Hurt ? 0.5f : value == PlayerState.Landing ? 0.08f : 0f; }
        /// <inheritdoc />
        public void Execute()
        {
            if (value == PlayerState.Hurt || value == PlayerState.Landing)
            {
                remaining -= Time.deltaTime;
                if (remaining <= 0f) owner.ChangeState(owner.InWater ? PlayerState.Swimming : PlayerState.Idle);
            }
        }
        /// <inheritdoc />
        public void FixedExecute() { }
        /// <inheritdoc />
        public void Exit() { }
    }
    private readonly Dictionary<PlayerState, IPlayerState> states = new();
    public PlayerState CurrentState { get; private set; }
    public bool InWater { get; set; }
    public bool MovementLocked => CurrentState == PlayerState.Dead || CurrentState == PlayerState.Hurt ||
        CurrentState == PlayerState.Mining || CurrentState == PlayerState.Interacting;
    public bool IsLocomotion => CurrentState <= PlayerState.Jetpacking;
    private void Awake()
    {
        foreach (PlayerState value in System.Enum.GetValues(typeof(PlayerState))) states[value] = new State(this, value);
        states[CurrentState].Enter();
    }
    private void Update() { states[CurrentState].Execute(); }
    private void FixedUpdate() { states[CurrentState].FixedExecute(); }
    /// <summary>Transitions to a registered state; death cannot be interrupted.</summary>
    public void ChangeState(PlayerState next)
    {
        if (next == CurrentState || CurrentState == PlayerState.Dead || !states.ContainsKey(next)) return;
        states[CurrentState].Exit();
        CurrentState = next;
        states[CurrentState].Enter();
    }
}
