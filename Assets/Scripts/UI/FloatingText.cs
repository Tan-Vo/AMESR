using UnityEngine;

[RequireComponent(typeof(TextMesh))]
public sealed class FloatingText : MonoBehaviour
{
    private TextMesh label;
    private FloatingTextPool owner;
    private Vector3 origin;
    private Color color;
    private float remaining;
    private void Awake() { label = GetComponent<TextMesh>(); }
    /// <summary>Initializes a pooled label with one second of upward motion.</summary>
    public void Show(FloatingTextPool pool, Vector3 position, string text, Color tint)
    {
        owner = pool;
        origin = position;
        transform.SetParent(null, true);
        transform.position = position;
        label.text = text;
        color = tint;
        remaining = 1f;
    }
    private void LateUpdate()
    {
        remaining -= Time.deltaTime;
        if (remaining <= 0f) { owner.Return(this); return; }
        transform.position = origin + Vector3.up * (1f - remaining);
        label.color = new Color(color.r, color.g, color.b, remaining);
    }
}
