using UnityEngine;

public sealed class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private SO_UICommandEvent onUICommandRequested;
    [SerializeField] private SO_IntEvent onStateEntered;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject settings;
    private bool paused;
    private void OnEnable() { onStateEntered?.RegisterListener(State); }
    private void OnDisable() { onStateEntered?.UnregisterListener(State); }
    private void State(int state) { paused = (GameState)state == GameState.Paused; panel.SetActive(paused); }
    private void Update() { if (Input.GetKeyDown(KeyCode.Escape)) onUICommandRequested?.Raise(paused ? UICommand.Resume : UICommand.Pause); }
    /// <summary>Resumes simulation.</summary>
    public void Resume() { onUICommandRequested?.Raise(UICommand.Resume); }
    /// <summary>Opens settings without advancing simulation.</summary>
    public void Settings() { settings.SetActive(true); }
    /// <summary>Saves the active session.</summary>
    public void Save() { onUICommandRequested?.Raise(UICommand.Save); }
    /// <summary>Loads the active slot.</summary>
    public void Load() { onUICommandRequested?.Raise(UICommand.Continue); }
    /// <summary>Returns to the title.</summary>
    public void Quit() { onUICommandRequested?.Raise(UICommand.MainMenu); }
}
