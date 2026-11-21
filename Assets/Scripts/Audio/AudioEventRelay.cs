using UnityEngine;

public sealed class AudioEventRelay : MonoBehaviour
{
    [SerializeField] private SO_GameEvent source;
    [SerializeField] private SO_AudioCueEvent onAudioCueRequested;
    [SerializeField] private SfxType cue;
    private void OnEnable() { source?.RegisterListener(Play); }
    private void OnDisable() { source?.UnregisterListener(Play); }
    private void Play() { onAudioCueRequested?.Raise(cue); }
}
