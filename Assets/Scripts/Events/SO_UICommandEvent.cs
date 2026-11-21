using System;
using UnityEngine;

public enum UICommand { NewGame, Continue, Save, MainMenu, Pause, Resume, Hub, Tutorial, Shallows, Twilight, Abyssal, EventHorizon, Cantina, Sleep }

[CreateAssetMenu(menuName = "Cosmic Horizon/Events/UI Command")]
public sealed class SO_UICommandEvent : ScriptableObject
{
    private event Action<UICommand> listeners;
    /// <summary>Registers the core flow endpoint.</summary>
    public void RegisterListener(Action<UICommand> listener) { listeners += listener; }
    /// <summary>Unregisters the endpoint.</summary>
    public void UnregisterListener(Action<UICommand> listener) { listeners -= listener; }
    /// <summary>Requests a navigation or persistence action.</summary>
    public void Raise(UICommand command) { listeners?.Invoke(command); }
}
