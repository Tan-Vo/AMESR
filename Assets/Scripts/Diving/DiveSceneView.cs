using UnityEngine;

public sealed class DiveSceneView : MonoBehaviour
{
    [SerializeField] private Transform target;
    private void LateUpdate()
    {
        if (target != null) transform.position = new Vector3(target.position.x, target.position.y, -10f);
    }
}
