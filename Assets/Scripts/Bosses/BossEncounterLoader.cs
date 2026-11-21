using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BossEncounterLoader : MonoBehaviour
{
    [SerializeField] private SO_BossEncounterEvent onBossEncounterRequested;
    [SerializeField] private SO_GameEvent onBossDefeated;
    [SerializeField] private SO_GameEvent onPlayerDied;
    [SerializeField] private SO_GameEvent onEmergencySurface;
    [SerializeField] private SO_GameEvent onBossReturnRequested;
    [SerializeField] private SO_IntEvent onStateRequested;
    [SerializeField] private SO_FloatingTextEvent onFloatingTextRequested;
    private bool loading;
    private bool ending;
    private void OnEnable()
    {
        onBossEncounterRequested?.RegisterListener(Begin);
        onBossDefeated?.RegisterListener(Victory);
        onPlayerDied?.RegisterListener(Failure);
        onEmergencySurface?.RegisterListener(Failure);
    }
    private void OnDisable()
    {
        onBossEncounterRequested?.UnregisterListener(Begin);
        onBossDefeated?.UnregisterListener(Victory);
        onPlayerDied?.UnregisterListener(Failure);
        onEmergencySurface?.UnregisterListener(Failure);
    }
    private void Begin() { if (!loading && !ending) StartCoroutine(Load()); }
    private IEnumerator Load()
    {
        loading = true;
        if (!Application.CanStreamedLevelBeLoaded("SC_BossArena"))
        {
            onFloatingTextRequested?.Raise(onBossEncounterRequested.ReturnPosition, "Boss arena is missing from Build Settings", Color.red);
            onBossEncounterRequested.Clear(); loading = false; yield break;
        }
        onStateRequested?.Raise((int)GameState.BossFight);
        var operation = SceneManager.LoadSceneAsync("SC_BossArena", LoadSceneMode.Additive);
        if (operation != null) yield return operation;
        loading = false;
        if (operation == null) { onBossEncounterRequested.Clear(); onStateRequested?.Raise((int)GameState.Diving); }
    }
    private void Victory() { if (onBossEncounterRequested.InProgress && !ending) StartCoroutine(Finish(12f)); }
    private void Failure() { if (onBossEncounterRequested.InProgress && !ending) StartCoroutine(Finish(1f)); }
    private IEnumerator Finish(float delay)
    {
        ending = true;
        yield return new WaitForSeconds(delay);
        while (loading) yield return null;
        var scene = SceneManager.GetSceneByName("SC_BossArena");
        if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        onBossEncounterRequested.Clear();
        onBossReturnRequested?.Raise();
        ending = false;
    }
}
