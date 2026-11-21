using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(400)]
public sealed class BossPhaseManager : MonoBehaviour
{
    [SerializeField] private BossArenaDirector arena;
    [SerializeField] private Text bossLabel;
    private Camera view;
    private float originalZoom;
    private float intro;
    private float shake;
    private int phase;
    private void Start() { view = Camera.main; if (view != null) originalZoom = view.orthographicSize; intro = 1.5f; }
    private void OnDisable() { if (view != null) view.orthographicSize = originalZoom; }
    private void LateUpdate()
    {
        var boss = arena.CurrentBoss;
        if (boss == null || view == null) return;
        view.orthographicSize = 9f;
        intro = Mathf.Max(0f, intro - Time.deltaTime);
        if (intro > 0f) view.transform.position = Vector3.Lerp(arena.Player.position, boss.transform.position, Mathf.Sin(intro / 1.5f * Mathf.PI) * 0.5f) + Vector3.back * 10f;
        if (phase != boss.CurrentPhase) { phase = boss.CurrentPhase; shake = 0.5f; }
        shake = Mathf.Max(boss.CurrentPhase == 3 && !arena.Finished ? 0.08f : 0f, shake - Time.deltaTime);
        view.transform.position += new Vector3(Mathf.PerlinNoise(Time.time * 20f, 0) - 0.5f, Mathf.PerlinNoise(0, Time.time * 20f) - 0.5f) * shake;
        if (bossLabel != null) bossLabel.text = arena.Finished ? boss.BossName + " — encounter ended" :
            boss.BossName + "  Phase " + boss.CurrentPhase + "  HP " + boss.CurrentHP + "/" + boss.MaxHP + (arena.HasShieldAdds(boss) ? "  SHIELDED: destroy both adds" : "");
    }
}
