using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueSystem : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Image portrait;
    [SerializeField] private Text speaker;
    [SerializeField] private Text body;
    [SerializeField] private SO_DialogueLine conversation;
    [SerializeField] private SO_IntEvent onDialogueCompleted;
    [SerializeField] private float lettersPerSecond = 35f;
    private int index;
    private float elapsed, finishedAt = -1f;
    private bool open;
    private string Line => conversation != null && conversation.Lines != null && index < conversation.Lines.Length ? conversation.Lines[index] ?? "" : "";
    /// <summary>Starts a configured portrait conversation.</summary>
    public void Begin()
    {
        if (conversation == null || conversation.Lines == null || conversation.Lines.Length == 0) return;
        index = 0; elapsed = 0; finishedAt = -1; open = true; panel.SetActive(true);
        portrait.sprite = conversation.Portrait; speaker.text = conversation.SpeakerName;
    }
    private void Update()
    {
        if (!open) return;
        elapsed += Time.unscaledDeltaTime;
        bool complete = elapsed * lettersPerSecond >= Line.Length;
        if (complete && finishedAt < 0) finishedAt = elapsed;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || complete && elapsed - finishedAt >= 3f)
        {
            if (!complete) { elapsed = Line.Length / Mathf.Max(1, lettersPerSecond); return; }
            if (++index >= conversation.Lines.Length) { open = false; panel.SetActive(false); onDialogueCompleted?.Raise((int)conversation.OnComplete); }
            else { elapsed = 0f; finishedAt = -1f; }
        }
    }
    private void LateUpdate() { if (open) body.text = Line.Substring(0, Mathf.Min(Line.Length, (int)(elapsed * lettersPerSecond))); }
}
