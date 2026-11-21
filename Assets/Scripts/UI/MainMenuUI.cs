using System.IO;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuUI : MonoBehaviour
{
    [SerializeField] private SO_UICommandEvent onUICommandRequested;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject settings;
    private void Start() { continueButton.interactable = File.Exists(Path.Combine(Application.persistentDataPath, "save_slot_0.json")); }
    /// <summary>Starts a fresh day-one session at the hub.</summary>
    public void NewGame() { onUICommandRequested?.Raise(UICommand.NewGame); }
    /// <summary>Loads the active save through the persistence endpoint.</summary>
    public void Continue() { onUICommandRequested?.Raise(UICommand.Continue); }
    /// <summary>Opens the shared settings panel.</summary>
    public void Settings() { settings.SetActive(true); }
    /// <summary>Quits a standalone player.</summary>
    public void Quit() { Application.Quit(); }
}
