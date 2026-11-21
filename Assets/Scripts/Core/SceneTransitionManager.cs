using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneTransitionManager : Singleton<SceneTransitionManager>
{
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private SO_StringEvent onSceneRequested;
    [SerializeField] private SO_StringEvent onSceneLoaded;
    [SerializeField] private SO_StringEvent onSceneFailed;
    public bool IsLoading { get; private set; }

    private void OnEnable() { if (onSceneRequested != null) onSceneRequested.RegisterListener(LoadScene); }
    private void OnDisable() { if (onSceneRequested != null) onSceneRequested.UnregisterListener(LoadScene); }
    /// <summary>Fades to a scene asynchronously; boss arenas are loaded additively.</summary>
    public void LoadScene(string sceneName)
    {
        if (IsLoading || string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        { onSceneFailed?.Raise(sceneName); return; }
        StartCoroutine(Transition(sceneName));
    }
    private IEnumerator Transition(string sceneName)
    {
        IsLoading = true;
        yield return FadeOut();
        AsyncOperation operation = null;
        try { operation = SceneManager.LoadSceneAsync(sceneName, sceneName == "SC_BossArena" ? LoadSceneMode.Additive : LoadSceneMode.Single); }
        catch (System.Exception error) { Debug.LogWarning(error.Message, this); }
        if (operation != null) yield return operation;
        yield return FadeIn();
        IsLoading = false;
        if (operation == null) onSceneFailed?.Raise(sceneName);
        else onSceneLoaded?.Raise(sceneName);
    }
    /// <summary>Fades the persistent overlay to transparent using unscaled time.</summary>
    public IEnumerator FadeIn() => Fade(0f);
    /// <summary>Fades the persistent overlay to opaque using unscaled time.</summary>
    public IEnumerator FadeOut() => Fade(1f);
    private IEnumerator Fade(float target)
    {
        if (fadeCanvasGroup == null) yield break;
        fadeCanvasGroup.blocksRaycasts = true;
        float start = fadeCanvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / Mathf.Max(0.001f, fadeDuration));
            yield return null;
        }
        fadeCanvasGroup.alpha = target;
        fadeCanvasGroup.blocksRaycasts = target > 0f;
    }
}
