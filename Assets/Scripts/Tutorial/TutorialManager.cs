using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialManager : Singleton<TutorialManager>
{
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_GameEvent onTutorialStarted, onCreatureCaught, onDishServed, onSaveLoaded;
    [SerializeField] private SO_FloatEvent onFuelChanged, onOxygenChanged;
    [SerializeField] private SO_IntEvent onWaterStateChanged, onStateEntered;
    [SerializeField] private SO_StringEvent onSceneLoaded;
    [SerializeField] private SO_UICommandEvent onUICommandRequested;
    [SerializeField] private GameObject panel;
    [SerializeField] private Text instructions;
    private bool active, moved, oxygenSeen, inTraining, inCantina, underwater;
    private float elapsed;
    private static readonly string[] Steps = {
        "WASD to move, Space to jump — move, then jump.",
        "Hold Shift to use Jetpack — watch the orange fuel gauge.",
        "Enter the water to start diving — leave the landing platform.",
        "Click to fire your harpoon at a fish. Hold to reel; keep tension in the green zone. Catch one!",
        "Watch your Oxygen! The blue gauge drains faster at depth and while boosting.",
        "Return to the surface before oxygen runs out — swim or jetpack upward.",
        "Welcome to the Nebula Cantina! Press E to meet the kitchen crew.",
        "Pick dishes from your catch, then click OPEN CANTINA.",
        "E at the kitchen counter to pick up a dish; E at the matching customer's table to serve.",
        "Great job! You're ready to explore the cosmos. Press E to finish." };
    private void OnEnable()
    {
        onTutorialStarted?.RegisterListener(Begin); onCreatureCaught?.RegisterListener(Caught); onDishServed?.RegisterListener(Served);
        onFuelChanged?.RegisterListener(Fuel); onOxygenChanged?.RegisterListener(Oxygen); onWaterStateChanged?.RegisterListener(Water);
        onStateEntered?.RegisterListener(State); onSceneLoaded?.RegisterListener(Scene); onSaveLoaded?.RegisterListener(Restore);
        onUICommandRequested?.RegisterListener(Navigation);
    }
    private void OnDisable()
    {
        onTutorialStarted?.UnregisterListener(Begin); onCreatureCaught?.UnregisterListener(Caught); onDishServed?.UnregisterListener(Served);
        onFuelChanged?.UnregisterListener(Fuel); onOxygenChanged?.UnregisterListener(Oxygen); onWaterStateChanged?.UnregisterListener(Water);
        onStateEntered?.UnregisterListener(State); onSceneLoaded?.UnregisterListener(Scene); onSaveLoaded?.UnregisterListener(Restore);
        onUICommandRequested?.UnregisterListener(Navigation);
    }
    private void Begin() { session.Data.TutorialStep = 0; active = true; moved = oxygenSeen = inCantina = false; elapsed = 0; }
    private void Restore() { active = session.Data.TutorialStep > 0 && session.Data.TutorialStep < 10; }
    private void Navigation(UICommand command) { if (command == UICommand.MainMenu || command == UICommand.NewGame) active = false; }
    private void Scene(string scene)
    {
        inTraining = scene is "SC_Tutorial" or "SC_DiveZone1_Shallows";
        inCantina = scene == "SC_NebulaCantina";
        underwater = false;
    }
    private bool At(int step) => active && session.Data.TutorialStep == step && (step < 6 ? inTraining : inCantina);
    private void Next() { session.Data.TutorialStep = Mathf.Min(10, session.Data.TutorialStep + 1); elapsed = 0; if (session.Data.TutorialStep == 10) active = false; }
    private void Caught() { if (At(3)) Next(); }
    private void Served() { if (At(8)) Next(); }
    private void Fuel(float value) { if (At(1) && value < 0.98f) Next(); }
    private void Oxygen(float value) { if (At(4) && value < 0.97f) oxygenSeen = true; }
    private void Water(int value)
    {
        underwater = value != 0;
        if (At(2) && value == 1) Next();
        else if (At(5) && value == 0) { Next(); onUICommandRequested?.Raise(UICommand.Cantina); }
    }
    private void State(int value) { if (At(7) && (GameState)value == GameState.CantinaOpen) Next(); }
    private void Update()
    {
        if (!At(session.Data.TutorialStep) || Time.timeScale == 0f) return;
        elapsed += Time.deltaTime;
        if (At(0)) { moved |= Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f; if (moved && Input.GetKeyDown(KeyCode.Space)) Next(); }
        else if (At(2) && underwater && elapsed > 1f) Next();
        else if (At(4) && oxygenSeen && elapsed >= 3f) Next();
        else if (At(5) && !underwater && elapsed > 1f) { Next(); onUICommandRequested?.Raise(UICommand.Cantina); }
        else if ((At(6) || At(9)) && Input.GetKeyDown(KeyCode.E)) Next();
    }
    private void LateUpdate()
    {
        int step = session.Data.TutorialStep;
        bool show = At(step) && Time.timeScale > 0f;
        panel.SetActive(show);
        if (show) instructions.text = "TUTORIAL " + (step + 1) + "/10\n" + Steps[Mathf.Clamp(step, 0, 9)];
    }
}
