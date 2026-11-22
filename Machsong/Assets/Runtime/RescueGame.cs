using UnityEngine;

namespace MachSong
{
    public sealed class RescueGame : MonoBehaviour
    {
        public RescueRun Run { get; private set; }
        private WreckView world;
        private RescueHud hud;
        private RescueAudio audioBed;
        private bool paused, gentle, muted;
        private bool complete;
        private float clock, duration;
        private RescuePhase lastPhase;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            gentle = PlayerPrefs.GetInt("machsong.gentle") == 1;
            muted = PlayerPrefs.GetInt("machsong.muted") == 1;
            world = new WreckView(transform);
            audioBed = new RescueAudio(gameObject,muted);
            hud = new RescueHud(transform);
            ShowTitle();
        }

        public void Launch()
        {
            audioBed.ResetMission();
            Run = new RescueRun();
            lastPhase = Run.Phase;
            paused = complete = false;
            duration = 0;
            world.Launch();
            world.Tick(Run,clock,0,gentle);
            hud.Flight(TogglePause);
            hud.Refresh(Run,world.Project);
            audioBed.Signal();
        }

        public void ShowTitle()
        {
            audioBed.ResetMission();
            Run = null;
            paused = complete = false;
            world.ShowTitle();
            hud.Title(Launch,Settings,ToggleMute,muted);
        }

        private void Settings() => hud.Settings(ShowTitle,ToggleMute,ToggleMotion,muted,gentle);

        public void TogglePause()
        {
            if (Run == null || complete) return;
            paused = !paused;
            audioBed.PausePulse(paused);
            if (paused)
                hud.Pause(TogglePause,Launch,ShowTitle,ToggleMute,ToggleMotion,muted,gentle);
            else
            {
                hud.Flight(TogglePause);
                hud.Refresh(Run,world.Project);
            }
        }

        public void ToggleMute()
        {
            muted = !muted;
            audioBed.SetMuted(muted);
            PlayerPrefs.SetInt("machsong.muted",muted ? 1 : 0);
            PlayerPrefs.Save();
            hud.UpdateSettings(muted,gentle);
        }

        public void ToggleMotion()
        {
            gentle = !gentle;
            PlayerPrefs.SetInt("machsong.gentle",gentle ? 1 : 0);
            PlayerPrefs.Save();
            hud.UpdateSettings(muted,gentle);
        }

        public void OperateTether()
        {
            if (Run != null && !paused && Run.Interact()) audioBed.Click();
        }

        public void SendPulse()
        {
            if (Run != null && !paused && Run.Trigger()) audioBed.Heartbeat();
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime,.05f);
            clock += dt;
            if (Run == null)
            {
                world.ShowTitle();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (paused || complete) return;
            if (Input.GetKeyDown(KeyCode.E)) OperateTether();
            if (Input.GetKeyDown(KeyCode.Q)) SendPulse();
            Vector2 direction = new(
                Control(KeyCode.D,KeyCode.RightArrow) - Control(KeyCode.A,KeyCode.LeftArrow),
                Control(KeyCode.W,KeyCode.UpArrow) - Control(KeyCode.S,KeyCode.DownArrow));
            bool boost = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.Space);
            Simulate(direction,boost,dt);
        }

        private static int Control(KeyCode key,KeyCode arrow) => Input.GetKey(key) || Input.GetKey(arrow) ? 1 : 0;

        public void Simulate(Vector2 direction,bool boost,float dt)
        {
            if (Run == null || paused || complete) return;
            Run.Advance(direction,boost,dt);
            duration += dt;
            world.Tick(Run,clock,dt,gentle);
            if (lastPhase != Run.Phase)
            {
                if (Run.Phase == RescuePhase.Prepare) audioBed.Signal();
                if (Run.Phase == RescuePhase.Recover) audioBed.Signal();
                if (Run.Phase == RescuePhase.Complete)
                {
                    complete = true;
                    audioBed.Success();
                    hud.Complete(Run,duration,Launch,ShowTitle);
                }
                lastPhase = Run.Phase;
            }
            if (!complete) hud.Refresh(Run,world.Project);
        }
    }
}
