using UnityEngine;

public sealed class FloatingTextSpawner : MonoBehaviour
{
    [SerializeField] private SO_FloatingTextEvent onFloatingTextRequested;
    [SerializeField] private FloatingTextPool pool;
    private void OnEnable() { onFloatingTextRequested?.RegisterListener(Spawn); }
    private void OnDisable() { onFloatingTextRequested?.UnregisterListener(Spawn); }
    private void Spawn(Vector3 position, string text, Color color)
    {
        var label = pool != null ? pool.Get() : null;
        if (label != null) label.Show(pool, position, text, color);
    }
}
