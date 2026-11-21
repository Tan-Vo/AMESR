using UnityEngine;

[DefaultExecutionOrder(700)]
public sealed class ParallaxController : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private SpriteRenderer[] tiles;
    [SerializeField] private float[] speeds = { 0.05f, 0.15f, 0.3f, 0.6f, 0.9f };
    [SerializeField] private float automaticScroll;
    private float origin;
    private void Start() { origin = cameraTransform.position.x; }
    private void LateUpdate()
    {
        float cameraX = cameraTransform.position.x;
        for (int layer = 0; layer < 5; layer++)
        {
            float width = tiles[layer * 3].sprite.bounds.size.x * tiles[layer * 3].transform.localScale.x;
            float basis = origin + (cameraX - origin) * speeds[layer] + Time.unscaledTime * automaticScroll * speeds[layer];
            float nearest = Mathf.Round((cameraX - basis) / width);
            for (int copy = 0; copy < 3; copy++)
                tiles[layer * 3 + copy].transform.position = new Vector3(basis + (nearest + copy - 1) * width, cameraTransform.position.y, 10 + layer);
        }
    }
}
