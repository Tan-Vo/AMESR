using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsUI : MonoBehaviour
{
    [SerializeField] private Slider volume;
    [SerializeField] private Slider bgm, sfx;
    [SerializeField] private SO_FloatEvent onBgmVolumeChanged, onSfxVolumeChanged;
    [SerializeField] private Dropdown resolution;
    [SerializeField] private Toggle fullscreen;
    private Resolution[] resolutions;
    private void Start()
    {
        resolutions = Screen.resolutions;
        var options = new List<string>();
        foreach (var entry in resolutions) options.Add(entry.width + " × " + entry.height + " @ " + entry.refreshRateRatio);
        if (options.Count == 0) options.Add(Screen.width + " × " + Screen.height);
        resolution.ClearOptions(); resolution.AddOptions(options);
        fullscreen.isOn = Screen.fullScreen;
        volume.value = PlayerPrefs.GetFloat("MasterVolume", 0.8f); AudioListener.volume = volume.value;
        bgm.value = PlayerPrefs.GetFloat("BgmVolume", 0.7f); sfx.value = PlayerPrefs.GetFloat("SfxVolume", 1f);
    }
    /// <summary>Applies and persists master volume.</summary>
    public void Volume(float value) { AudioListener.volume = value; PlayerPrefs.SetFloat("MasterVolume", value); }
    /// <summary>Applies and persists music volume.</summary>
    public void BgmVolume(float value) { PlayerPrefs.SetFloat("BgmVolume", value); onBgmVolumeChanged?.Raise(value); }
    /// <summary>Applies and persists effects volume.</summary>
    public void SfxVolume(float value) { PlayerPrefs.SetFloat("SfxVolume", value); onSfxVolumeChanged?.Raise(value); }
    /// <summary>Applies a supported resolution.</summary>
    public void Resolution(int index) { if (resolutions != null && index >= 0 && index < resolutions.Length) Screen.SetResolution(resolutions[index].width, resolutions[index].height, Screen.fullScreen); }
    /// <summary>Switches fullscreen mode.</summary>
    public void Fullscreen(bool value) { Screen.fullScreen = value; }
    /// <summary>Closes settings.</summary>
    public void Close() { gameObject.SetActive(false); }
}
