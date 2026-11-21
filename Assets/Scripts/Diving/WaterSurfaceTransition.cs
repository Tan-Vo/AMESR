using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public sealed class WaterSurfaceTransition : MonoBehaviour
{
    [SerializeField] private SO_GameEvent onSplash;
    [SerializeField] private SO_IntEvent onWaterStateChanged;
    private PlayerController controller;
    private int waterContacts;
    private void Awake() { controller = GetComponent<PlayerController>(); }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("WaterSurface")) return;
        waterContacts++;
        if (waterContacts == 1) { controller.SetUnderwater(true); onSplash?.Raise(); onWaterStateChanged?.Raise(1); }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("WaterSurface")) return;
        waterContacts = Mathf.Max(0, waterContacts - 1);
        if (waterContacts == 0) { controller.SetUnderwater(false); onSplash?.Raise(); onWaterStateChanged?.Raise(0); }
    }
}
