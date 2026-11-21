using UnityEngine;
using UnityEngine.UI;

public sealed class BossHealthBarUI : MonoBehaviour
{
    [SerializeField] private BossArenaDirector arena;
    [SerializeField] private Image fill;
    [SerializeField] private Text label;
    [SerializeField] private RectTransform panel;
    private float reveal;
    private void LateUpdate()
    {
        var boss = arena.CurrentBoss;
        if (boss == null) return;
        reveal = Mathf.MoveTowards(reveal, 1f, Time.unscaledDeltaTime * 2f);
        panel.anchoredPosition = new Vector2(0, Mathf.Lerp(650, 440, reveal));
        fill.fillAmount = boss.CurrentHP / (float)boss.MaxHP;
        fill.color = boss.CurrentPhase == 1 ? Color.green : boss.CurrentPhase == 2 ? Color.yellow : Color.red;
        label.text = boss.BossName + "  " + (boss.CurrentPhase == 1 ? "I" : boss.CurrentPhase == 2 ? "II" : "III") + "  " + boss.CurrentHP + "/" + boss.MaxHP;
    }
}
