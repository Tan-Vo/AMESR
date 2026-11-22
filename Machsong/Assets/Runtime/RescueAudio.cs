using UnityEngine;

namespace MachSong
{
    internal sealed class RescueAudio
    {
        private const int Rate = 24000;
        private readonly AudioSource drone, effects, heartbeat;
        private readonly AudioClip tick, signal, success;

        internal RescueAudio(GameObject host,bool muted)
        {
            drone = host.AddComponent<AudioSource>();
            effects = host.AddComponent<AudioSource>();
            heartbeat = host.AddComponent<AudioSource>();
            drone.loop = true;
            drone.volume = .50f;
            effects.volume = .72f;
            heartbeat.volume = effects.volume;
            drone.clip = Create("Wreck ambience",12,t =>
            {
                float breath = .70f+.24f*Wave(.25f,t);
                return breath*(.070f*Wave(42,t)+.033f*Wave(63,t)+.017f*Wave(126,t)) +
                       .019f*Wave(169,t)*Wave(.50f,t);
            });
            tick = Create("Tether lock",.28f,t => .31f*Wave(418,t)*Mathf.Exp(-23*t)+.16f*Wave(748,t)*Mathf.Exp(-29*t));
            signal = Create("Sonar",.87f,t => Bell(339,t)+Bell(502,t-.19f));
            heartbeat.clip = Create("Living pulse",2.85f,t => .51f*Thump(t-.81f)+.30f*Thump(t-1.13f) +
                .05f*Wave(55,t)*Mathf.Sin(Mathf.PI*t/2.85f));
            success = Create("Rescue",1.75f,t => Bell(293,t)+Bell(440,t-.18f)+Bell(587,t-.42f)+Bell(698,t-.59f));
            SetMuted(muted);
            drone.Play();
        }

        internal void SetMuted(bool muted) => drone.mute = effects.mute = heartbeat.mute = muted;
        internal void PausePulse(bool paused)
        {
            if (paused) heartbeat.Pause();
            else heartbeat.UnPause();
        }

        internal void ResetMission()
        {
            heartbeat.Stop();
            effects.Stop();
        }

        internal void Click() => effects.PlayOneShot(tick);
        internal void Signal() => effects.PlayOneShot(signal);
        internal void Heartbeat() => heartbeat.Play();
        internal void Success() => effects.PlayOneShot(success);

        private static AudioClip Create(string name,float duration,System.Func<float,float> sound)
        {
            var samples = new float[(int)(Rate*duration)];
            for (int i=0;i<samples.Length;i++)
            {
                float t = i/(float)Rate;
                float fade = Mathf.Clamp01(t*90)*Mathf.Clamp01((duration-t)*(duration < 3f ? 16 : 90));
                samples[i] = fade*sound(t);
            }
            var clip = AudioClip.Create(name,samples.Length,1,Rate,false);
            clip.SetData(samples,0);
            return clip;
        }

        private static float Wave(float frequency,float time) => Mathf.Sin(2*Mathf.PI*frequency*time);
        private static float Bell(float hz,float t) => t < 0 ? 0 :
            Mathf.Clamp01(t*190)*Mathf.Exp(-5.5f*t)*(.18f*Wave(hz,t)+.06f*Wave(hz*2,t));
        private static float Thump(float t) => t < 0 ? 0 :
            Mathf.Clamp01(t*37)*Mathf.Exp(-6.5f*t)*Mathf.Sin(2*Mathf.PI*(73*t-11*t*t));
    }
}
