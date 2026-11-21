using UnityEngine;

[RequireComponent(typeof(GameManager))]
public sealed class UIFlowRouter : MonoBehaviour
{
    [SerializeField] private SO_UICommandEvent onUICommandRequested;
    [SerializeField] private SO_GameEvent onSaveRequested;
    [SerializeField] private SO_GameEvent onLoadRequested;
    [SerializeField] private SO_GameEvent onTutorialStarted;
    private GameManager manager;
    private void Awake() { manager = GetComponent<GameManager>(); }
    private void OnEnable() { onUICommandRequested?.RegisterListener(Execute); }
    private void OnDisable() { onUICommandRequested?.UnregisterListener(Execute); }
    private void Execute(UICommand command)
    {
        switch (command)
        {
            case UICommand.NewGame: manager.NewGame(); break;
            case UICommand.Continue: onLoadRequested?.Raise(); break;
            case UICommand.Save: onSaveRequested?.Raise(); break;
            case UICommand.Pause: manager.ChangeState(GameState.Paused); break;
            case UICommand.Resume: manager.Resume(); break;
            case UICommand.Sleep: manager.GoToSleep(); break;
            case UICommand.Tutorial: onTutorialStarted?.Raise(); manager.Navigate(command); break;
            default: manager.Navigate(command); break;
        }
    }
}
