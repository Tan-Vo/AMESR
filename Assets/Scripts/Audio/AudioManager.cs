using System.Collections;
using UnityEngine;

public sealed class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private AudioSource bgmSource, sfxSource;
    [SerializeField] private AudioClip[] bgmTracks, sfxClips, biomeTracks;
    [SerializeField] private float bgmVolume = 0.7f, sfxVolume = 1f;
    [SerializeField] private SO_AudioCueEvent onAudioCueRequested;
    [SerializeField] private SO_IntEvent onStateEntered, onBiomeChanged, onUpgradeChanged;
    [SerializeField] private SO_FloatEvent onBgmVolumeChanged, onSfxVolumeChanged;
    private Coroutine transition;
    private GameState state;
    private void OnEnable()
    {
        onAudioCueRequested?.RegisterListener(PlaySfx); onStateEntered?.RegisterListener(State);
        onBiomeChanged?.RegisterListener(Biome); onUpgradeChanged?.RegisterListener(Purchase);
        onBgmVolumeChanged?.RegisterListener(MusicVolume); onSfxVolumeChanged?.RegisterListener(EffectsVolume);
    }
    private void OnDisable()
    {
        onAudioCueRequested?.UnregisterListener(PlaySfx); onStateEntered?.UnregisterListener(State);
        onBiomeChanged?.UnregisterListener(Biome); onUpgradeChanged?.UnregisterListener(Purchase);
        onBgmVolumeChanged?.UnregisterListener(MusicVolume); onSfxVolumeChanged?.UnregisterListener(EffectsVolume);
    }
    private void Start()
    {
        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        MusicVolume(PlayerPrefs.GetFloat("BgmVolume", 0.7f)); EffectsVolume(PlayerPrefs.GetFloat("SfxVolume", 1f));
    }
    private void MusicVolume(float value) { bgmVolume = Mathf.Clamp01(value); if (bgmSource != null) bgmSource.volume = bgmVolume; }
    private void EffectsVolume(float value) { sfxVolume = Mathf.Clamp01(value); }
    private void State(int value)
    {
        if ((GameState)value == GameState.Paused) { bgmSource.Pause(); sfxSource.Pause(); return; }
        state = (GameState)value; bgmSource.UnPause(); sfxSource.UnPause();
        if (state == GameState.BossFight) PlaySfx(SfxType.BossIntro);
        if (bgmTracks != null && value >= 0 && value < bgmTracks.Length) ChangeTrack(bgmTracks[value]);
    }
    private void Biome(int value) { if (state == GameState.Diving && biomeTracks != null && value >= 0 && value < biomeTracks.Length) ChangeTrack(biomeTracks[value]); }
    private void Purchase(int category) { PlaySfx(SfxType.CashRegister); }
    /// <summary>Plays an assigned cue; missing clips are intentionally silent.</summary>
    public void PlaySfx(SfxType cue)
    { int index = (int)cue; if (sfxSource != null && sfxClips != null && index >= 0 && index < sfxClips.Length && sfxClips[index] != null) sfxSource.PlayOneShot(sfxClips[index], sfxVolume); }
    /// <summary>Fades out and into an optional music track over two half-second ramps.</summary>
    public void ChangeTrack(AudioClip clip)
    {
        if (bgmSource == null || bgmSource.clip == clip) return;
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(FadeTrack(clip));
    }
    private IEnumerator FadeTrack(AudioClip clip)
    {
        float initial = bgmSource.volume;
        for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime) { bgmSource.volume = Mathf.Lerp(initial, 0f, t * 2f); yield return null; }
        bgmSource.Stop(); bgmSource.clip = clip; bgmSource.loop = true;
        if (clip != null) bgmSource.Play();
        for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime) { bgmSource.volume = Mathf.Lerp(0f, bgmVolume, t * 2f); yield return null; }
        bgmSource.volume = bgmVolume; transition = null;
    }
}
