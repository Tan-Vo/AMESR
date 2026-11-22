using UnityEngine;

[DefaultExecutionOrder(800)]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CosmicBackdrop : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private bool alignWater;
    [SerializeField] private Vector2 overscan = new Vector2(1.3f, 1.12f);
    private SpriteRenderer backdrop;

    private void Awake() => backdrop = GetComponent<SpriteRenderer>();

    private void LateUpdate()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null || backdrop.sprite == null) return;

        var size = backdrop.sprite.bounds.size;
        var view = worldCamera.transform.position;
        float halfHeight = worldCamera.orthographicSize;
        float halfWidth = halfHeight * worldCamera.aspect;
        float scale = 2f * Mathf.Max(overscan.x * halfWidth / size.x, overscan.y * halfHeight / size.y);
        float spareX = Mathf.Max(0f, size.x * scale * 0.5f - halfWidth);
        float spareY = Mathf.Max(0f, size.y * scale * 0.5f - halfHeight);
        float y = alignWater ? Mathf.Clamp(-0.15f * size.y * scale, view.y - spareY, view.y + spareY) : view.y;

        transform.position = new Vector3(view.x - Mathf.Clamp(view.x * 0.06f, -spareX, spareX), y, view.z + 30f);
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
