using UnityEngine;
using UnityEngine.UI;

public sealed class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private SO_UICommandEvent onUICommandRequested;
    [SerializeField] private SO_IntEvent onStateEntered;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject settings;
    [SerializeField] private Button saveButton;
    private bool canSave;
    private bool paused;
    private void OnEnable() { onStateEntered?.RegisterListener(State); }
    private void OnDisable() { onStateEntered?.UnregisterListener(State); }
    private void State(int state)
    {
        paused = (GameState)state == GameState.Paused;
        if (!paused) canSave = CheckpointPolicy.CanSave((GameState)state);
        if (saveButton != null) { saveButton.interactable = canSave; saveButton.GetComponentInChildren<Text>().text = canSave ? "Save" : "Save (hub only)"; }
        panel.SetActive(paused);
    }
    private void Update() { if (Input.GetKeyDown(KeyCode.Escape)) onUICommandRequested?.Raise(paused ? UICommand.Resume : UICommand.Pause); }
    /// <summary>Resumes simulation.</summary>
    public void Resume() { onUICommandRequested?.Raise(UICommand.Resume); }
    /// <summary>Opens settings without advancing simulation.</summary>
    public void Settings() { settings.SetActive(true); }
    /// <summary>Saves the active session.</summary>
    public void Save() { if (canSave) onUICommandRequested?.Raise(UICommand.Save); }
    /// <summary>Loads the active slot.</summary>
    public void Load() { onUICommandRequested?.Raise(UICommand.Continue); }
    /// <summary>Returns to the title.</summary>
    public void Quit() { onUICommandRequested?.Raise(UICommand.MainMenu); }
}
