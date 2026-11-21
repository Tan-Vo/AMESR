using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIAudioFeedback : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private SO_AudioCueEvent onAudioCueRequested;
    /// <summary>Requests an optional hover sound.</summary>
    public void OnPointerEnter(PointerEventData data) { onAudioCueRequested?.Raise(SfxType.UIHover); }
    /// <summary>Requests an optional click sound.</summary>
    public void OnPointerClick(PointerEventData data) { onAudioCueRequested?.Raise(SfxType.UIClick); }
}
