using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Data/DialogueLine")]
public class SO_DialogueLine : ScriptableObject
{
    [SerializeField] private string speakerName;
    public string SpeakerName => speakerName;
    [SerializeField] private Sprite portrait;
    public Sprite Portrait => portrait;
    [SerializeField, TextArea(3, 5)] private string[] lines;
    public string[] Lines => lines;
    [SerializeField] private DialogueEvent onComplete;
    public DialogueEvent OnComplete => onComplete;
}
