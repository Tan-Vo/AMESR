using UnityEngine;

namespace MachSong
{
    public enum RescuePhase { FindSignal, Prepare, Pulse, Recover, Complete }
    public enum RescueRoute { Undecided, UpperBridge, LowerDuct }

    // Authored geometry is shared by the pilot, capsule and wreck renderer.
    public sealed class RescueRun
    {
        public static readonly Vector2 Dock = new(-4.1f, 3.2f);
        public static readonly Vector2 Receiver = new(11.2f, 3.34f);
        public static readonly Vector2 Brace = new(15.4f, 3.68f);
        public static readonly Vector2 Anchor = new(18.7f, 5.42f);
        public static readonly Vector2 InitialPlate = new(19.2f, 3.35f);
        public static readonly Vector2 InitialCapsule = new(21.75f, -.38f);
        public const float WallLeft = 7.9f, WallRight = 9.45f;
        public const float WorldLeft = -6.7f, WorldRight = 30.2f, Floor = -5.8f, Ceiling = 6.05f;
        public const float UpperMin = 1.99f, UpperMax = 4.69f, LowerMin = -5.22f, LowerMax = -2.54f;
        private const float PilotRadius = .24f, CapsuleRadius = .44f;
        private const float Windup = .80f, PulseDuration = 1.85f;

        public RescuePhase Phase { get; private set; }
        public RescueRoute Route { get; private set; }
        public Vector2 Pilot { get; private set; } = new(-3.15f, 3.23f);
        public Vector2 Velocity { get; private set; }
        public Vector2 Capsule { get; private set; } = InitialCapsule;
        public Vector2 Plate { get; private set; } = InitialPlate;
        public float PlateAngle { get; private set; } = -9f;
        public float RibAngle { get; private set; }
        public bool Clamped { get; private set; }
        public bool Tethered { get; private set; }
        public bool RodeCapsule { get; private set; }
        public bool ReleasedInPulse { get; private set; }
        public bool IsClosed => Phase is RescuePhase.Recover or RescuePhase.Complete;
        public bool UpperOpen => !IsClosed || Route == RescueRoute.UpperBridge;
        public bool LowerOpen => IsClosed && Route == RescueRoute.LowerDuct;
        public float PulseProgress => Mathf.Clamp01((pulseTime-Windup)/PulseDuration);
        public float Warning => Phase == RescuePhase.Pulse ? Mathf.Clamp01(pulseTime/Windup) : 0;
        public bool CanPulse => Phase == RescuePhase.Prepare && Pilot.x > WallRight + .68f;
        public bool NearBrace => Phase == RescuePhase.Prepare && Vector2.Distance(Pilot,Brace) < 1.65f;
        public bool NearCapsule => Phase != RescuePhase.Complete && Vector2.Distance(Pilot,Capsule) < 1.53f;
        public float Power { get; private set; } = 1;
        public float Strain { get; private set; }
        public bool Boosting { get; private set; }
        private float pulseTime;
        private Vector2 rideOffset;

        public bool Interact()
        {
            if (Phase == RescuePhase.Complete) return false;
            if (Tethered)
            {
                Tethered = false;
                if (Phase == RescuePhase.Pulse && PulseProgress > 0)
                {
                    Velocity = Vector2.ClampMagnitude(Velocity,7f);
                    ReleasedInPulse = true;
                }
                return true;
            }
            if (NearBrace)
            {
                Clamped = !Clamped;
                return true;
            }
            if (NearCapsule && (Phase == RescuePhase.Prepare || Phase == RescuePhase.Pulse || Phase == RescuePhase.Recover))
            {
                Tethered = true;
                rideOffset = Vector2.ClampMagnitude(Pilot-Capsule,1.35f);
                if (rideOffset.sqrMagnitude < .28f) rideOffset = Vector2.up*.55f;
                return true;
            }
            return false;
        }

        public bool Trigger()
        {
            if (!CanPulse) return false;
            Route = Clamped ? RescueRoute.UpperBridge : RescueRoute.LowerDuct;
            Phase = RescuePhase.Pulse;
            return true;
        }

        public void Advance(Vector2 direction,bool boost,float dt)
        {
            if (Phase == RescuePhase.Complete || dt <= 0) return;
            dt = Mathf.Min(dt,.05f);
            direction = Vector2.ClampMagnitude(direction,1);
            Strain = Mathf.MoveTowards(Strain,0,dt*.50f);
            if (Phase == RescuePhase.Pulse) MoveWreck(dt);

            bool towing = Tethered && Phase == RescuePhase.Recover;
            bool riding = Phase == RescuePhase.Pulse && Tethered && PulseProgress > 0;
            Boosting = !riding && boost && direction.sqrMagnitude > .02f && Power > .035f;
            Power = Mathf.Clamp01(Power + (Boosting ? -.39f : .37f)*dt);
            float speed = Boosting ? (towing ? 6.9f : 9.1f) : (towing ? 4.45f : 5.85f);
            speed *= 1f - Strain*.25f;
            if (riding)
            {
                RodeCapsule = true;
                rideOffset = Vector2.ClampMagnitude(rideOffset + direction*dt*.88f,1.32f);
                Vector2 previous = Pilot;
                Pilot = Move(Pilot,Vector2.ClampMagnitude(Capsule + rideOffset-Pilot,dt*22f),PilotRadius);
                Velocity = (Pilot-previous)/dt;
            }
            else
            {
                Velocity = Vector2.MoveTowards(Velocity,direction*speed,dt*(Boosting ? 29 : 23));
                Pilot = Move(Pilot,Velocity*dt,PilotRadius);
            }

            if (Phase == RescuePhase.Pulse && !Tethered && PulseProgress > .05f && PulseProgress < .98f &&
                TouchesPlate(Pilot))
            {
                Strain = 1;
                Velocity += (Pilot-Plate + Vector2.up*.28f).normalized*dt*15f;
            }

            if (Phase == RescuePhase.FindSignal && Vector2.Distance(Pilot,Receiver) < 1.40f)
                Phase = RescuePhase.Prepare;

            if (towing)
            {
                Vector2 rope = Pilot - Capsule;
                float distance = rope.magnitude;
                if (distance > .86f)
                    Capsule = Move(Capsule,rope.normalized*Mathf.Min(distance-.86f,dt*7.55f),CapsuleRadius);
            }

            if (Phase == RescuePhase.Recover && Tethered &&
                Vector2.Distance(Capsule,Dock) < 1.25f && Vector2.Distance(Pilot,Dock) < 2.12f)
            {
                Phase = RescuePhase.Complete;
                Boosting = false;
                Velocity = Vector2.zero;
            }
        }

        private void MoveWreck(float dt)
        {
            pulseTime += dt;
            float p = PulseProgress;
            float eased = p*p*(3-2*p);
            float arc = Mathf.Sin(Mathf.PI*p);
            bool safe = Route == RescueRoute.UpperBridge;
            Capsule = Vector2.Lerp(InitialCapsule,safe ? new Vector2(24.35f,2.38f) : new Vector2(24.8f,-2.86f),eased)
                      + Vector2.up*arc*.71f;
            Plate = Vector2.Lerp(InitialPlate,safe ? new Vector2(18.75f,3.62f) : new Vector2(8.66f,3.36f),eased)
                    + Vector2.up*arc*(safe ? .13f : -.91f);
            PlateAngle = Mathf.Lerp(-9,safe ? -17 : 71,eased);
            RibAngle = Mathf.Lerp(0,-72,eased);
            if (p < 1) return;
            Phase = RescuePhase.Recover;
            // A pilot in the moving bridge aperture ends on the nearest side.
            if (Blocked(Pilot,PilotRadius))
            {
                float side = (WallLeft+WallRight)*.5f;
                Pilot = new Vector2(Pilot.x < side ? WallLeft-PilotRadius-.02f : WallRight+PilotRadius+.02f,Pilot.y);
            }
        }

        private bool TouchesPlate(Vector2 point)
        {
            Vector2 offset = point - Plate;
            float angle = PlateAngle*Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            float x = offset.x*cos + offset.y*sin;
            float y = offset.y*cos - offset.x*sin;
            return Mathf.Abs(x) < 2.19f && Mathf.Abs(y) < .76f;
        }

        private Vector2 Move(Vector2 body,Vector2 step,float radius)
        {
            Vector2 horizontal = new(Mathf.Clamp(body.x+step.x,WorldLeft+radius,WorldRight-radius),body.y);
            if (!Blocked(horizontal,radius)) body = horizontal;
            Vector2 vertical = new(body.x,Mathf.Clamp(body.y+step.y,Floor+radius,Ceiling-radius));
            return Blocked(vertical,radius) ? body : vertical;
        }

        private bool Blocked(Vector2 body,float radius)
        {
            if (body.x+radius <= WallLeft || body.x-radius >= WallRight) return false;
            if (UpperOpen && body.y-radius > UpperMin && body.y+radius < UpperMax) return false;
            if (LowerOpen && body.y-radius > LowerMin && body.y+radius < LowerMax) return false;
            return true;
        }
    }
}
