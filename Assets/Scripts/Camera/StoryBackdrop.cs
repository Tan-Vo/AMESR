using System;
using UnityEngine;

[DefaultExecutionOrder(800)]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class StoryBackdrop : MonoBehaviour
{
    private Camera view;
    private SpriteRenderer backdrop;

    private void Awake() => view = GetComponentInParent<Camera>();
    private void LateUpdate() => Fit(view);

    public void Fit(Camera camera)
    {
        if (camera == null || !camera.orthographic) return;
        if (backdrop == null) backdrop = GetComponent<SpriteRenderer>();
        if (backdrop.sprite == null) return;

        var size = backdrop.sprite.bounds.size;
        float scale = CoverScale(size.x, size.y, camera.orthographicSize, camera.aspect);
        if (transform.localScale.x != scale) transform.localScale = Vector3.one * scale;
    }

    internal static float CoverScale(float spriteWidth, float spriteHeight, float orthographicSize, float aspect)
    {
        const float framingWidth = 35f;
        float viewHeight = 2f * orthographicSize;
        return Math.Max(Math.Max(framingWidth, viewHeight * aspect) / spriteWidth, viewHeight / spriteHeight);
    }
}
