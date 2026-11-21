using System;
using UnityEngine;

public enum SfxType { Jump, Land, Shoot, HarpoonFire, HarpoonHit, CableSnap, ReelIn, CreatureCaught, MineHit, MineComplete, TakeDamage, Die, Heal, JetpackLoop, JetpackOverheat, CustomerOrder, DishReady, DishServed, CustomerHappy, CustomerAngry, CashRegister, BossIntro, BossPhaseChange, BossDefeat, UIClick, UIHover, Splash, OxygenWarning }

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/Audio Cue")]
public sealed class SO_AudioCueEvent : ScriptableObject
{
    private event Action<SfxType> listeners;
    /// <summary>Registers an audio service.</summary>
    public void RegisterListener(Action<SfxType> listener) { listeners += listener; }
    /// <summary>Unregisters a service.</summary>
    public void UnregisterListener(Action<SfxType> listener) { listeners -= listener; }
    /// <summary>Requests an optional sound without requiring an imported clip.</summary>
    public void Raise(SfxType cue) { listeners?.Invoke(cue); }
}
