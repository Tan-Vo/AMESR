using UnityEngine;
using UnityEngine.UI;

public sealed class UICommandButton : MonoBehaviour
{
    [SerializeField] private SO_UICommandEvent onUICommandRequested;
    [SerializeField] private UICommand command;
    [SerializeField] private SO_GearProfile gear;
    [SerializeField] private SO_IntEvent onUpgradeChanged;
    [SerializeField] private float requiredDepth;
    [SerializeField] private CanvasGroup availabilityGraphic;
    private Button button;
    private void Awake() { button = GetComponent<Button>(); }
    private void Start() { Refresh(0); }
    private void OnEnable() { onUpgradeChanged?.RegisterListener(Refresh); }
    private void OnDisable() { onUpgradeChanged?.UnregisterListener(Refresh); }
    private void Refresh(int category)
    {
        bool available = gear == null || gear.SafeDepth >= requiredDepth;
        if (button != null) button.interactable = available;
        if (availabilityGraphic != null) availabilityGraphic.alpha = available ? 1f : 0.48f;
    }
    /// <summary>Dispatches this inspector-configured navigation command.</summary>
    public void Click() { if (gear == null || gear.SafeDepth >= requiredDepth) onUICommandRequested?.Raise(command); }
}
