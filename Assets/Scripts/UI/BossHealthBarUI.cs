using UnityEngine;
using UnityEngine.UI;

public sealed class BossHealthBarUI : MonoBehaviour
{
    [SerializeField] private BossArenaDirector arena;
    [SerializeField] private Image fill;
    [SerializeField] private Text label;
    [SerializeField] private RectTransform panel;
    [SerializeField] private float displayedY = 440f;
    private float reveal;
    private void LateUpdate()
    {
        var boss = arena.CurrentBoss;
        if (boss == null) return;
        reveal = Mathf.MoveTowards(reveal, 1f, Time.unscaledDeltaTime * 2f);
        panel.anchoredPosition = new Vector2(0, Mathf.Lerp(650, displayedY, reveal));
        fill.fillAmount = boss.CurrentHP / (float)boss.MaxHP;
        fill.color = boss.CurrentPhase == 1 ? new Color(0.42f, 0.80f, 0.85f) :
            boss.CurrentPhase == 2 ? new Color(0.97f, 0.73f, 0.40f) : new Color(0.91f, 0.50f, 0.45f);
        label.text = boss.BossName.ToUpperInvariant() + "   ·   " + (boss.CurrentPhase == 1 ? "I" : boss.CurrentPhase == 2 ? "II" : "III");
    }
}
