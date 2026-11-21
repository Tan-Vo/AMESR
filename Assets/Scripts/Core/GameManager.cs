using UnityEngine;

public sealed class GameManager : Singleton<GameManager>
{
    [SerializeField] private SO_GameEvent onGameStateChanged;
    [SerializeField] private SO_IntEvent onStateEntered;
    [SerializeField] private SO_IntEvent onStateRequested;
    [SerializeField] private SO_GameEvent onDayEnded;
    [SerializeField] private SO_GameEvent onNightEnded;
    [SerializeField] private SO_GameEvent onSaveRequested;
    [SerializeField] private SO_GameEvent onCaptureSave;
    [SerializeField] private SO_GameEvent onSaveLoaded;
    [SerializeField] private SO_GameEvent onBossReturnRequested;
    [SerializeField] private SO_StringEvent onSceneRequested;
    [SerializeField] private SO_StringEvent onSceneLoaded;
    [SerializeField] private SO_StringEvent onSceneFailed;
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private float divingTimeLimitSeconds = 300f;
    [SerializeField] private float cantinaTimeLimitSeconds = 180f;
    private GameState currentState = GameState.MainMenu;
    private GameState previousState;
    private GameState pendingState;
    private GameState beforeLoading;
    private string pendingScene;
    private int currentDay = 1;
    private float dayTimer;
    public float DayTimer => dayTimer;

    private void OnEnable()
    {
        onStateRequested?.RegisterListener(RequestState);
        onCaptureSave?.RegisterListener(CaptureSave);
        onSaveLoaded?.RegisterListener(RestoreSave);
        onSceneLoaded?.RegisterListener(SceneReady);
        onSceneFailed?.RegisterListener(SceneFailed);
        onBossReturnRequested?.RegisterListener(EndDiving);
    }
    private void OnDisable()
    {
        onStateRequested?.UnregisterListener(RequestState);
        onCaptureSave?.UnregisterListener(CaptureSave);
        onSaveLoaded?.UnregisterListener(RestoreSave);
        onSceneLoaded?.UnregisterListener(SceneReady);
        onSceneFailed?.UnregisterListener(SceneFailed);
        onBossReturnRequested?.UnregisterListener(EndDiving);
        if (Instance == this) Time.timeScale = 1f;
    }
    private void Update()
    {
        if (currentState == GameState.Paused || currentState == GameState.Loading || currentState == GameState.MainMenu) return;
        if (session != null) session.Data.TotalPlayTime += Time.deltaTime;
        if (currentState != GameState.Diving && currentState != GameState.CantinaOpen) return;
        dayTimer = Mathf.Max(0f, dayTimer - Time.deltaTime);
        if (dayTimer > 0f) return;
        if (currentState == GameState.Diving) EndDiving();
        else EndCantina();
    }
    private void RequestState(int value)
    {
        if (System.Enum.IsDefined(typeof(GameState), value)) ChangeState((GameState)value);
    }
    /// <summary>Changes the loop state and broadcasts both notification and typed state channels.</summary>
    public void ChangeState(GameState newState)
    {
        if (currentState == newState || !System.Enum.IsDefined(typeof(GameState), newState)) return;
        if (currentState == GameState.Loading && pendingScene != null) return;
        if (newState == GameState.Paused)
        {
            if (currentState == GameState.MainMenu || currentState == GameState.Loading) return;
            previousState = currentState;
            currentState = newState;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
            previousState = currentState;
            currentState = newState;
        }
        onGameStateChanged?.Raise();
        if (newState == GameState.CantinaOpen && dayTimer <= 0f) dayTimer = cantinaTimeLimitSeconds;
        onStateEntered?.Raise((int)currentState);
    }
    /// <summary>Resets the session and enters the first morning.</summary>
    public void NewGame()
    {
        if (pendingScene != null) return;
        session?.Replace(new SaveData());
        currentDay = 1;
        Load("SC_Tutorial", GameState.MorningBrief);
    }
    /// <summary>Advances the calendar and enters a morning briefing.</summary>
    public void StartNewDay()
    {
        if (pendingScene != null) return;
        currentDay++;
        CaptureSave();
        Load("SC_Mothership", GameState.MorningBrief);
    }
    /// <summary>Loads the shallows and begins the dive clock.</summary>
    public void StartDiving()
    {
        if (currentState != GameState.DivingPrep || pendingScene != null) return;
        dayTimer = divingTimeLimitSeconds;
        Load("SC_DiveZone1_Shallows", GameState.Diving);
    }
    /// <summary>Ends the dive, notifies catch systems, and returns to cantina preparation.</summary>
    public void EndDiving()
    {
        if (currentState != GameState.Diving && currentState != GameState.BossFight) return;
        ChangeState(GameState.Surfacing);
        onDayEnded?.Raise();
        Load("SC_Mothership", GameState.CantinaPrep);
    }
    /// <summary>Opens the cantina and begins the night clock.</summary>
    public void StartCantina()
    {
        if (currentState != GameState.CantinaPrep || pendingScene != null) return;
        dayTimer = cantinaTimeLimitSeconds;
        Load("SC_NebulaCantina", GameState.CantinaOpen);
    }
    /// <summary>Closes restaurant service and requests a night report.</summary>
    public void EndCantina()
    {
        if (currentState != GameState.CantinaOpen) return;
        ChangeState(GameState.NightShop);
        onNightEnded?.Raise();
    }
    /// <summary>Advances the day and requests a save before loading the morning hub.</summary>
    public void GoToSleep()
    {
        if (currentState != GameState.NightShop || pendingScene != null) return;
        ChangeState(GameState.Sleeping);
        currentDay++;
        CaptureSave();
        onSaveRequested?.Raise();
        Load("SC_Mothership", GameState.MorningBrief);
    }
    /// <summary>Restores the state active before pausing.</summary>
    public void Resume() { if (currentState == GameState.Paused) ChangeState(previousState); }
    /// <summary>Returns the active game state.</summary>
    public GameState GetCurrentState() => currentState;
    /// <summary>Returns the one-based day number.</summary>
    public int GetCurrentDay() => currentDay;

    private void CaptureSave() { if (session != null) session.Data.CurrentDay = currentDay; }
    private void RestoreSave()
    {
        if (session == null || pendingScene != null) return;
        currentDay = session.Data.CurrentDay;
        Load("SC_Mothership", GameState.MorningBrief);
    }
    private void Load(string scene, GameState destination)
    {
        if (pendingScene != null || onSceneRequested == null) return;
        beforeLoading = currentState;
        ChangeState(GameState.Loading);
        pendingState = destination;
        pendingScene = scene;
        onSceneRequested.Raise(scene);
    }
    private void SceneReady(string scene)
    {
        if (scene != pendingScene) return;
        pendingScene = null;
        ChangeState(pendingState);
    }
    private void SceneFailed(string scene)
    {
        if (scene != pendingScene) return;
        pendingScene = null;
        ChangeState(beforeLoading);
    }
}
