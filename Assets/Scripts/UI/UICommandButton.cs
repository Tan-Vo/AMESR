using UnityEngine;

public sealed class UICommandButton : MonoBehaviour
{
    [SerializeField] private SO_UICommandEvent onUICommandRequested;
    [SerializeField] private UICommand command;
    /// <summary>Dispatches this inspector-configured navigation command.</summary>
    public void Click() { onUICommandRequested?.Raise(command); }
}
