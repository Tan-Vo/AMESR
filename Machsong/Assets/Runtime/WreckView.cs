using System;
using UnityEngine;

namespace MachSong
{
    internal sealed class WreckView
    {
        private static readonly Color Cyan = new(.46f,.99f,.85f);
        private static readonly Color Amber = new(1f,.71f,.43f);
        private static readonly Color Closed = new(.87f,.39f,.33f);
        private static readonly Vector2 Hinge = new(25.7f,2.22f);
        private readonly Camera camera;
        private readonly Transform sea;
        private readonly Material material;
        private readonly Sprite pixel, wreckSprite, titleSprite, armor, receiver, pulseSprite;
        private readonly SpriteRenderer sky, pilot, capsule, plate, pulse, beacon, brace, dockBeacon;
        private readonly SpriteRenderer upperDoor, lowerDoor, upperLight, lowerLight;
        private readonly SpriteRenderer[] dust;
        private readonly Sprite[] idle, boost;
        private readonly Transform rib;
        private readonly LineRenderer tow, clamp;
        private Vector2 focus = new(1,0);
        private bool headingLeft;

        internal WreckView(Transform host)
        {
            var view = new GameObject("Wreck camera",typeof(Camera),typeof(AudioListener));
            view.transform.SetParent(host,false);
            camera = view.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f,.060f,.082f);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 60f;
            material = Resources.Load<Material>("MachSong/SceneSprite");
            if (material == null) throw new InvalidOperationException("Missing Mach Song sprite material. Run Mach Song > Open Rescue Scene.");

            var tex = new Texture2D(1,1);
            tex.SetPixel(0,0,Color.white);
            tex.Apply(false,true);
            pixel = Sprite.Create(tex,new Rect(0,0,1,1),new Vector2(.5f,.5f),1,0,SpriteMeshType.FullRect);
            wreckSprite = Asset("wreck");
            titleSprite = Asset("title");
            armor = Asset("armor");
            receiver = Asset("receiver");
            pulseSprite = Asset("pulse");
            idle = Atlas("pilot_idle", 280, 240);
            boost = Atlas("pilot_boost", 280, 240);
            sky = Piece(host,"Painted Leviathan",titleSprite,Vector2.zero,Vector2.one,-100,Color.white);
            sea = new GameObject("Wreck").transform;
            sea.SetParent(host,false);
            BuildScenery();

            float barrierX = (RescueRun.WallLeft+RescueRun.WallRight)*.5f;
            upperDoor = Door("Bridge shutter",barrierX,RescueRun.UpperMin,RescueRun.UpperMax);
            lowerDoor = Door("Maintenance hatch",barrierX,RescueRun.LowerMin,RescueRun.LowerMax);
            upperLight = Light("Bridge signal",new Vector2(barrierX,RescueRun.UpperMax-.07f),new Vector2(1.35f,.07f),Cyan,6);
            lowerLight = Light("Duct signal",new Vector2(barrierX,RescueRun.LowerMax-.07f),new Vector2(1.35f,.07f),Closed,6);

            rib = new GameObject("Living rib hinge").transform;
            rib.SetParent(sea,false);
            rib.position = Hinge;
            var beam = Piece(rib,"Rib pulls free",armor,Vector2.zero,new Vector2(6.65f,1.45f),1,new Color(.80f,.83f,.79f));
            beam.transform.localPosition = new Vector3(-2.80f,0,0);
            Light("Rib hinge pin",Hinge,new Vector2(.77f,.77f),Amber,3);

            plate = Piece(sea,"Loose armor",Asset("plate"),RescueRun.InitialPlate,new Vector2(5.8f,1.77f),5,Color.white);
            capsule = Piece(sea,"Rescue capsule",Asset("capsule"),RescueRun.InitialCapsule,new Vector2(2.75f,1.78f),7,Color.white);
            pilot = Piece(sea,"Kai",idle[0],Vector2.zero,new Vector2(2.34f,2.0f),10,Color.white);
            pulse = Piece(sea,"Visible pressure wave",pulseSprite,RescueRun.Receiver,Vector2.one,9,Cyan);
            beacon = Piece(sea,"Pulse receiver",receiver,RescueRun.Receiver,new Vector2(1.26f,1.26f),4,Color.white);
            brace = Piece(sea,"Brace switch",receiver,RescueRun.Brace,new Vector2(.98f,.98f),6,Amber);
            dockBeacon = Piece(sea,"Docking beacon",pulseSprite,RescueRun.Dock,new Vector2(2.22f,2.22f),2,Cyan);
            tow = Cable("Rescue tether",7,.039f,Cyan);
            clamp = Cable("Anchor tether",5,.054f,Cyan);
            dust = new SpriteRenderer[38];
            var random = new System.Random(1144);
            for (int i=0;i<dust.Length;i++)
            {
                float width = .016f+(float)random.NextDouble()*.035f;
                dust[i] = Piece(sea,"Drifting particles",pixel,Vector2.zero,new Vector2(width,width),-2,
                    new Color(.59f,.77f,.72f,.32f+(float)random.NextDouble()*.22f));
            }
            ShowTitle();
        }

        internal void ShowTitle()
        {
            sea.gameObject.SetActive(false);
            sky.sprite = titleSprite;
            focus = Vector2.zero;
            FrameCamera(Vector2.zero,true);
        }

        internal void Launch()
        {
            sea.gameObject.SetActive(true);
            sky.sprite = wreckSprite;
            focus = new Vector2(.65f,.10f);
            headingLeft = false;
        }

        internal Vector3 Project(Vector2 point) => camera.WorldToViewportPoint(point);

        internal void Tick(RescueRun run,float time,float dt,bool gentle)
        {
            if (!sea.gameObject.activeSelf)
            {
                FrameCamera(Vector2.zero,true);
                return;
            }

            if (Mathf.Abs(run.Velocity.x) > .12f) headingLeft = run.Velocity.x < 0;
            pilot.flipX = headingLeft;
            pilot.sprite = (run.Boosting ? boost : idle)[gentle ? 0 : (int)(time*(run.Boosting ? 19 : 10))%idle.Length];
            pilot.transform.position = run.Pilot;
            float tilt = gentle ? 0 : Mathf.Clamp(run.Velocity.y*1.8f,-14,14)*(headingLeft ? -1 : 1);
            pilot.transform.rotation = Quaternion.Euler(0,0,tilt);

            capsule.transform.position = run.Capsule;
            capsule.transform.rotation = Quaternion.Euler(0,0,gentle ? 0 : Mathf.Sin(time*1.8f)*3f);
            plate.transform.position = run.Plate;
            plate.transform.rotation = Quaternion.Euler(0,0,run.PlateAngle);
            rib.rotation = Quaternion.Euler(0,0,run.RibAngle);
            upperDoor.enabled = !run.UpperOpen;
            lowerDoor.enabled = !run.LowerOpen;
            upperLight.color = run.UpperOpen ? Cyan : Closed;
            lowerLight.color = run.LowerOpen ? Cyan : Closed;
            beacon.color = run.Phase == RescuePhase.FindSignal ? Color.Lerp(Cyan,Color.white,.45f+.38f*Mathf.Sin(time*4)) : Color.white;
            brace.color = run.Clamped ? Cyan : Amber;
            dockBeacon.color = Color.Lerp(new Color(.19f,.63f,.57f,.35f),Cyan,run.Phase == RescuePhase.Recover ? .80f : .25f);
            Vector2 towAttach = run.Capsule + Vector2.left*.67f;
            DrawCable(tow,run.Pilot,towAttach,run.Tethered,time);
            DrawCable(clamp,RescueRun.Anchor,run.Plate,run.Clamped,time);
            ShowPulse(run,gentle);
            for (int i=0;i<dust.Length;i++)
            {
                float x = Mathf.Repeat(i*4.137f+(gentle ? 0 : time*(.16f+(i%4)*.064f)),37)-6;
                float y = Mathf.Repeat(i*1.807f,11.2f)-5.6f;
                dust[i].transform.position = new Vector3(x,y+(gentle ? 0 : .18f*Mathf.Sin(time+i)),0);
            }

            float halfWidth = camera.orthographicSize*camera.aspect;
            float padding = Mathf.Min(halfWidth*.66f,7.5f);
            float lookAhead = Mathf.Min(headingLeft ? .95f : 1.7f,halfWidth*.4f);
            float centeredX = Mathf.Clamp(run.Pilot.x + (headingLeft ? -lookAhead : lookAhead),
                RescueRun.WorldLeft+padding,RescueRun.WorldRight-padding);
            Vector2 desired = new(centeredX,.10f);
            focus = Vector2.Lerp(focus,desired,1-Mathf.Exp(-dt*4.9f));
            focus.x = Mathf.Clamp(focus.x,run.Pilot.x-halfWidth*.78f,run.Pilot.x+halfWidth*.78f);
            Vector2 shake = Vector2.zero;
            if (!gentle && run.Phase == RescuePhase.Pulse)
            {
                float amplitude = .17f*Mathf.Sin(Mathf.PI*run.PulseProgress);
                shake = new Vector2(Mathf.Sin(time*59),Mathf.Cos(time*71))*amplitude;
            }
            FrameCamera(focus+shake,false);
        }

        private void ShowPulse(RescueRun run,bool gentle)
        {
            pulse.enabled = run.Phase == RescuePhase.Pulse;
            if (!pulse.enabled) return;
            float p = run.PulseProgress;
            float width = p == 0 ? 1.1f+run.Warning*2.25f : 3.3f+p*28;
            pulse.transform.position = p == 0 ? RescueRun.Receiver : new Vector2(17.5f,1.27f);
            pulse.transform.localScale = new Vector3(width/pulseSprite.bounds.size.x,width*.78f/pulseSprite.bounds.size.y,1);
            pulse.color = p == 0 ? new Color(1,.77f,.53f,.62f) : new Color(.61f,1,.83f,(1-p)*(gentle ? .28f : .79f));
        }

        private void FrameCamera(Vector2 center,bool title)
        {
            camera.transform.position = new Vector3(center.x,center.y,-10);
            float h = camera.orthographicSize*2;
            Vector3 size = sky.sprite.bounds.size;
            float zoom = Mathf.Max(h/size.y,h*camera.aspect/size.x);
            sky.transform.localScale = Vector3.one * zoom * (title ? 1 : 1.15f);
            sky.transform.position = title ? Vector3.zero : new Vector3(center.x + Mathf.Sin(center.x*.11f)*1.02f,center.y,0);
        }

        private void BuildScenery()
        {
            for (int i=0;i<9;i++)
            {
                float x = -6+i*4.6f;
                var ceiling = Piece(sea,"Ceiling plating",armor,new Vector2(x,6.17f),new Vector2(5.2f,1.35f),-1,
                    new Color(.73f,.77f,.74f));
                ceiling.flipY = i%2 == 0;
                Piece(sea,"Floor plating",armor,new Vector2(x,-6.05f),new Vector2(5.2f,1.24f),-1,new Color(.69f,.73f,.74f));
                if (i%2 == 0)
                {
                    var strut = Piece(sea,"Distant wreck strut",armor,new Vector2(x+.45f,2.2f-(i%3)*2.2f),
                        new Vector2(5.6f,1.0f),-10,new Color(.25f,.41f,.46f,.73f));
                    strut.transform.rotation = Quaternion.Euler(0,0,-30+i*4.5f);
                }
            }

            float wallX = (RescueRun.WallLeft+RescueRun.WallRight)*.5f;
            Panel("Bulkhead floor", wallX,RescueRun.Floor-.38f, RescueRun.LowerMin);
            Panel("Bulkhead junction",wallX,RescueRun.LowerMax,RescueRun.UpperMin);
            Panel("Bulkhead overhead",wallX,RescueRun.UpperMax,RescueRun.Ceiling+.33f);
            for (int i=0;i<4;i++)
            {
                float y = 1.73f+i*.47f;
                Light("Junction weld",new Vector2(7.91f,y-.9f),new Vector2(.15f,.10f),Amber,1);
            }
            for (int i=0;i<7;i++)
            {
                float x=-6.1f+i*.76f;
                Light("Guidance rail",new Vector2(x,2.02f),new Vector2(.35f,.045f),Cyan,-1);
            }
            Piece(sea,"Transfer cradle",armor,new Vector2(-4.55f,1.72f),new Vector2(5.62f,1.30f),-1,Color.white);
            Piece(sea,"Capsule cradle",armor,new Vector2(21.52f,-1.48f),new Vector2(5.16f,1.10f),-1,new Color(.75f,.79f,.74f));
            Piece(sea,"Anchor eye",receiver,RescueRun.Anchor,new Vector2(.67f,.67f),2,Color.white);
            for (int i=0;i<4;i++)
                Light("Anchor fixture lamp",RescueRun.Anchor+new Vector2(-.27f-i*.35f,.40f),new Vector2(.13f,.065f),Amber,2);
        }

        private void Panel(string name,float x,float bottom,float top)
        {
            Vector2 center = new(x,(bottom+top)*.5f);
            var tile = Piece(sea,name,armor,center,new Vector2(top-bottom+.57f,1.77f),2,Color.white);
            tile.transform.rotation = Quaternion.Euler(0,0,90);
            Light(name+" spine",center,new Vector2(.048f,Mathf.Max(.16f,top-bottom-.10f)),new Color(.38f,.59f,.60f,.78f),3);
        }

        private SpriteRenderer Door(string name,float x,float bottom,float top)
        {
            var tile = Piece(sea,name,armor,new Vector2(x,(bottom+top)*.5f),new Vector2(top-bottom+.46f,1.82f),3,
                new Color(.88f,.58f,.49f));
            tile.transform.rotation = Quaternion.Euler(0,0,90);
            return tile;
        }

        private SpriteRenderer Light(string name,Vector2 position,Vector2 extent,Color tint,int order) =>
            Piece(sea,name,pixel,position,extent,order,tint);

        private LineRenderer Cable(string name,int order,float width,Color tint)
        {
            var node = new GameObject(name,typeof(LineRenderer));
            node.transform.SetParent(sea,false);
            var line = node.GetComponent<LineRenderer>();
            line.material = material;
            line.sortingOrder = order;
            line.positionCount = 9;
            line.startWidth = width;
            line.endWidth = width*.82f;
            line.startColor = Color.Lerp(tint,Color.white,.3f);
            line.endColor = tint;
            line.enabled = false;
            return line;
        }

        private static void DrawCable(LineRenderer line,Vector2 from,Vector2 to,bool visible,float time)
        {
            line.enabled = visible;
            if (!visible) return;
            for (int i=0;i<line.positionCount;i++)
            {
                float t = i/(float)(line.positionCount-1);
                Vector2 point = Vector2.Lerp(from,to,t)+Vector2.down*(Mathf.Sin(t*Mathf.PI)*(.08f+.035f*Mathf.Sin(time*5)));
                line.SetPosition(i,new Vector3(point.x,point.y,0));
            }
        }

        private SpriteRenderer Piece(Transform parent,string name,Sprite sprite,Vector2 position,Vector2 extent,int order,Color tint)
        {
            var node = new GameObject(name,typeof(SpriteRenderer));
            node.transform.SetParent(parent,false);
            node.transform.position = position;
            node.transform.localScale = new Vector3(extent.x/sprite.bounds.size.x,extent.y/sprite.bounds.size.y,1);
            var renderer = node.GetComponent<SpriteRenderer>();
            renderer.sharedMaterial = material;
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.color = tint;
            return renderer;
        }

        private static Sprite Asset(string name)
        {
            var asset = Texture(name);
            return Sprite.Create(asset,new Rect(0,0,asset.width,asset.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
        }

        private static Sprite[] Atlas(string name,int w,int h)
        {
            var asset = Texture(name);
            var frames = new Sprite[12];
            for (int i=0;i<frames.Length;i++)
            {
                var rect = new Rect(i%4*w,asset.height-(i/4+1)*h,w,h);
                frames[i] = Sprite.Create(asset,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            }
            return frames;
        }

        private static Texture2D Texture(string name)
        {
            var texture = Resources.Load<Texture2D>("MachSong/"+name);
            return texture != null ? texture : throw new InvalidOperationException($"Missing Mach Song art: {name}");
        }
    }
}
