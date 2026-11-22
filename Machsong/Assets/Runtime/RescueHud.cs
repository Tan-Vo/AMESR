using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MachSong
{
    internal sealed class RescueHud
    {
        private static readonly Color Ink = Hex("#0B1824");
        private static readonly Color Panel = Hex("#102332F2");
        private static readonly Color Edge = Hex("#294855");
        private static readonly Color White = Hex("#F0F1DD");
        private static readonly Color Muted = Hex("#B3C6C7");
        private static readonly Color Mint = Hex("#A3F5CD");
        private static readonly Color Amber = Hex("#FFD3A2");
        private static readonly Vector2 TL = new(0,1), TR = Vector2.one, BL = Vector2.zero, BR = new(1,0),
            C = new(.5f,.5f);

        private readonly RectTransform canvas;
        private readonly Font body, display;
        private RectTransform root, target, braceTarget, power;
        private Text objective, detail, warning, targetName, settingsAudio, settingsMotion;
        private RescuePhase? shownPhase;
        private int shownCondition;

        internal RescueHud(Transform host)
        {
            body = Resources.Load<Font>("MachSong/Hud");
            display = Resources.Load<Font>("MachSong/Display");
            var canvasObject = new GameObject("Mission interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(host,false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvas = canvasObject.GetComponent<RectTransform>();
            var events = new GameObject("Mission input",typeof(EventSystem),typeof(StandaloneInputModule));
            events.transform.SetParent(host,false);
        }

        internal void Title(Action launch,Action settings,Action mute,bool muted)
        {
            Reset();
            var shade = Stretch(root,"Title gradient");
            var gradient = Resources.Load<Texture2D>("MachSong/menu_gradient");
            var tint = shade.gameObject.AddComponent<RawImage>();
            tint.texture = gradient;
            tint.raycastTarget = false;
            var content = Box(root,"Opening",new Vector2(0,.5f),new Vector2(72,0),new Vector2(510,536),new Vector2(0,.5f));
            Block(content,"Heading separator",TL,new Vector2(0,-65),new Vector2(56,3),Mint);
            Label(content,"COSMIC HORIZON",20,Mint,TL,new Vector2(0,-23),new Vector2(405,32));
            Label(content,"MẠCH SỐNG",108,White,TL,new Vector2(-2,-83),new Vector2(525,141),true);
            Label(content,"Cứu hộ trong lòng sinh vật khổng lồ",25,Mint,TL,new Vector2(0,-220),new Vector2(482,39));
            Label(content,"Một khoang cứu hộ còn người sống. Kai phải dùng chính nhịp đập đang nghiền nát con tàu để đưa họ ra ngoài.",24,Muted,
                TL,new Vector2(0,-274),new Vector2(441,91),false,TextAnchor.UpperLeft);
            Button(content,"Launch mission","Bắt đầu cứu hộ",launch,TL,new Vector2(0,-388),new Vector2(271,62),true);
            Button(content,"Open settings","Tùy chọn",settings,TL,new Vector2(0,-464),new Vector2(128,46));
            settingsAudio = Button(content,"Toggle audio","",mute,TL,new Vector2(140,-464),new Vector2(166,46));
            UpdateSettings(muted,false);
        }

        internal void Flight(Action pause)
        {
            Reset();
            var panel = Block(root,"Objective panel",TL,new Vector2(31,-27),new Vector2(572,132),Panel);
            Block(panel,"Objective stripe",TL,new Vector2(0,0),new Vector2(4,132),Mint);
            Label(panel,"NEBULA CANTINA  /  KÊNH CỨU HỘ",17,Mint,TL,new Vector2(19,-12),new Vector2(490,22));
            objective = Label(panel,"",36,White,TL,new Vector2(18,-34),new Vector2(533,48),true);
            detail = Label(panel,"",19,Muted,TL,new Vector2(19,-81),new Vector2(534,46),false,TextAnchor.UpperLeft);

            var gauge = Block(root,"Thruster panel",TR,new Vector2(-31,-27),new Vector2(234,79),Panel);
            Label(gauge,"LỰC ĐẨY",17,White,TL,new Vector2(15,-10),new Vector2(115,24));
            Button(gauge,"Pause game","Tạm dừng",pause,TR,new Vector2(-10,-8),new Vector2(86,27));
            Block(gauge,"Gauge track",TL,new Vector2(15,-51),new Vector2(204,8),Edge);
            power = Block(gauge,"Charge",TL,new Vector2(15,-51),new Vector2(204,8),Mint);

            var hints = Block(root,"Controls",BL,new Vector2(31,24),new Vector2(637,48),Panel);
            KeyHint(hints,12,"WASD / MŨI TÊN","Bay",127);
            KeyHint(hints,191,"SHIFT","Tăng tốc",70);
            KeyHint(hints,349,"E","Móc / tháo",34);
            KeyHint(hints,498,"Q","Đánh nhịp",34);
            warning = Label(root,"",21,Amber,BR,new Vector2(-36,23),new Vector2(454,52),false,TextAnchor.MiddleRight);
            target = Marker("Objective marker",Mint,out targetName);
            braceTarget = Marker("Brace marker",Amber,out _);
            shownPhase = null;
        }

        internal void Refresh(RescueRun run,Func<Vector2,Vector3> project)
        {
            int condition = (run.Clamped ? 1 : 0) | (run.Tethered ? 2 : 0) | (run.CanPulse ? 4 : 0) | (run.NearBrace ? 8 : 0) |
                (run.NearCapsule ? 16 : 0);
            if (run.Phase != shownPhase || condition != shownCondition)
            {
                shownPhase = run.Phase;
                shownCondition = condition;
                switch (run.Phase)
                {
                    case RescuePhase.FindSignal:
                        SetObjective("Tiếp cận máy thu","SARAH  /  Nhịp đập dẫn tới khoang mắc kẹt. Qua khe cầu trên.", "MÁY THU");
                        break;
                    case RescuePhase.Prepare:
                        SetObjective("Chuẩn bị đánh nhịp",run.Clamped
                            ? "BOLT-88  /  Giáp đã ghìm. Đánh nhịp để nhấc khoang lên và giữ cầu trên."
                            : "BOLT-88  /  Ghìm giáp để giữ cầu trên, hoặc móc khoang rồi đi theo nó xuống dưới.","KHOANG");
                        break;
                    case RescuePhase.Pulse:
                        SetObjective("Xác tàu đang chuyển động",run.Tethered
                            ? "Giữ dây để theo khoang. Bấm E nếu muốn thả dây ngay khi khoang đang trượt."
                            : "Tránh tấm giáp. Tiếp cận khoang và bấm E để móc dây.","KHOANG");
                        break;
                    case RescuePhase.Recover:
                        SetObjective("Đưa khoang về bến",run.Route == RescueRoute.UpperBridge
                            ? "GORDO  /  Cầu trên còn nguyên. Kéo khoang qua cửa trên rồi vào vòng sáng ở bến."
                            : "GORDO  /  Kéo khoang qua giữa ống dưới rồi bay lên bến.",
                            run.Tethered ? "BẾN CỨU HỘ" : "KHOANG");
                        break;
                }
            }
            power.sizeDelta = new Vector2(204*run.Power,8);
            warning.text = run.Strain > .12f ? "Va chạm tấm giáp" : run.Phase == RescuePhase.Pulse && run.PulseProgress <= 0
                ? "ĐỢI NHỊP CO" : run.NearBrace && !run.Tethered
                ? (run.Clamped ? "E   Nhả neo" : "E   Ghìm tấm giáp")
                : run.CanPulse && run.NearCapsule && !run.Tethered ? "E   Móc khoang      Q   Đánh nhịp"
                : run.CanPulse ? "Q   Đánh nhịp" : run.NearCapsule && !run.Tethered ? "E   Móc khoang" : "";

            Vector2 position = run.Phase == RescuePhase.FindSignal ? RescueRun.Receiver :
                run.Phase == RescuePhase.Recover && run.Tethered ? RescueRun.Dock : run.Capsule;
            MarkerAt(target,project(position),.088f);
            braceTarget.gameObject.SetActive(run.Phase == RescuePhase.Prepare);
            if (braceTarget.gameObject.activeSelf) MarkerAt(braceTarget,project(RescueRun.Brace),.076f);
        }

        internal void Pause(Action resume,Action restart,Action title,Action mute,Action motion,bool muted,bool gentle)
        {
            Reset();
            var panel = Modal("Tạm dừng",498);
            Button(panel,"Resume game","Tiếp tục",resume,TL,new Vector2(29,-114),new Vector2(442,58),true);
            SettingButtons(panel,mute,motion,muted,gentle,189);
            Button(panel,"Restart mission","Bắt đầu lại",restart,TL,new Vector2(29,-261),new Vector2(213,53));
            Button(panel,"Return to title","Về tiêu đề",title,TL,new Vector2(257,-261),new Vector2(214,53));
        }

        internal void Settings(Action back,Action mute,Action motion,bool muted,bool gentle)
        {
            Reset();
            var panel = Modal("Tùy chọn",498);
            SettingButtons(panel,mute,motion,muted,gentle,108);
            Label(panel,"Giảm rung camera, chuyển động hạt và cường độ vòng sóng.",22,Muted,TL,
                new Vector2(29,-176),new Vector2(440,55),false,TextAnchor.UpperLeft);
            Button(panel,"Close settings","Quay lại",back,TL,new Vector2(29,-261),new Vector2(442,53),true);
        }

        internal void Complete(RescueRun run,float seconds,Action restart,Action title)
        {
            Reset();
            var panel = Modal("Đã cứu được người",612);
            string route = run.Route == RescueRoute.UpperBridge ? "CẦU TRÊN GIỮ NGUYÊN" : "ĐƯỜNG ỐNG BẢO TRÌ MỞ";
            int rounded = Mathf.FloorToInt(seconds);
            Label(panel,$"{route}   /   {rounded/60:00}:{rounded%60:00}",22,Mint,
                TL,new Vector2(29,-105),new Vector2(553,35),true);
            string survivor = run.Route == RescueRoute.UpperBridge
                ? "“Hạm Đội nói nó đã chết. Tôi nghe nó đập suốt ca trực.”"
                : "“Tôi thấy họ khóa lối thoát trước khi nhịp đầu tiên vang lên.”";
            Label(panel,survivor,26,White,TL,new Vector2(29,-157),new Vector2(549,68),false,TextAnchor.UpperLeft);
            Button(panel,"Replay mission","Thử lối còn lại",restart,TL,new Vector2(29,-261),new Vector2(334,53),true);
            Button(panel,"Return to title","Về tiêu đề",title,TL,new Vector2(379,-261),new Vector2(204,53));
        }

        internal void UpdateSettings(bool muted,bool gentle)
        {
            if (settingsAudio != null) settingsAudio.text = muted ? "Âm thanh: Tắt" : "Âm thanh: Bật";
            if (settingsMotion != null) settingsMotion.text = gentle ? "Giảm rung: Bật" : "Giảm rung: Tắt";
        }

        private void SettingButtons(RectTransform parent, Action mute, Action motion, bool muted, bool gentle, float offset)
        {
            settingsAudio = Button(parent,"Toggle audio","",mute,TL,new Vector2(29,-offset),new Vector2(213,53));
            settingsMotion = Button(parent,"Toggle motion","",motion,TL,new Vector2(257,-offset),new Vector2(214,53));
            UpdateSettings(muted,gentle);
        }

        private RectTransform Modal(string title,float width)
        {
            var shade = Stretch(root,"Dimmed view").gameObject.AddComponent<Image>();
            shade.color = Hex("#07131ACD");
            var panel = Block(root,"Dialog",C,Vector2.zero,new Vector2(width,343),Panel,C);
            Block(panel,"Dialog edge",TL,Vector2.zero,new Vector2(width,3),Mint);
            Label(panel,title,55,White,TL,new Vector2(27,-19),new Vector2(width-54,76),true);
            return panel;
        }

        private void SetObjective(string action,string subtitle,string marker)
        {
            objective.text = action;
            detail.text = subtitle;
            targetName.text = marker;
        }

        private RectTransform Marker(string name,Color tint,out Text label)
        {
            var marker = Box(root,name,BL,Vector2.zero,new Vector2(143,26),C);
            var backing = Block(marker,"Marker backing",TL,Vector2.zero,new Vector2(143,24),Hex("#11242ED8"));
            Block(backing,"Marker line",TL,Vector2.zero,new Vector2(2,24),tint);
            label = Label(backing,name == "Brace marker" ? "NEO GIÁP" : "",16,tint,TL,new Vector2(1,0),new Vector2(141,24),false,TextAnchor.MiddleCenter);
            return marker;
        }

        private static void MarkerAt(RectTransform marker,Vector3 view,float lift)
        {
            marker.anchorMin = marker.anchorMax = new Vector2(Mathf.Clamp(view.x,.063f,.937f),Mathf.Clamp(view.y+lift,.133f,.755f));
        }

        private void KeyHint(RectTransform parent,float x,string key,string action,float width)
        {
            var tile = Block(parent,key,TL,new Vector2(x,-10),new Vector2(width,28),Edge);
            Label(tile,key,17,Mint,TL,Vector2.zero,new Vector2(width,28),false,TextAnchor.MiddleCenter);
            Label(parent,action,18,White,TL,new Vector2(x+width+8,-10),new Vector2(100,28));
        }

        private void Reset()
        {
            if (root != null)
            {
                root.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(root.gameObject);
            }
            root = Stretch(canvas,"Current screen");
            settingsAudio = settingsMotion = null;
        }

        private Text Button(RectTransform parent,string name,string text,Action click,Vector2 anchor,Vector2 pos,Vector2 size,bool primary=false)
        {
            var shape = Block(parent,name,anchor,pos,size,primary ? Mint : Edge);
            var image = shape.GetComponent<Image>();
            image.raycastTarget = true;
            var button = shape.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var palette = button.colors;
            palette.highlightedColor = new Color(1.14f,1.14f,1.14f);
            palette.selectedColor = new Color(1.19f,1.19f,1.19f);
            palette.pressedColor = new Color(.83f,.88f,.88f);
            palette.colorMultiplier = 1.15f;
            button.colors = palette;
            button.onClick.AddListener(()=>click());
            return Label(shape,text,size.y >= 50 ? 25 : size.y >= 40 ? 21 : 17,primary ? Ink : White,
                TL,Vector2.zero,size,false,TextAnchor.MiddleCenter);
        }

        private Text Label(RectTransform parent,string text,int size,Color tint,Vector2 anchor,Vector2 pos,Vector2 extent,
                           bool heading=false,TextAnchor alignment=TextAnchor.MiddleLeft)
        {
            var rect = Box(parent,text.Length == 0 ? "Dynamic text" : text,anchor,pos,extent);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = heading ? display : body;
            label.fontSize = size;
            label.text = text;
            label.color = tint;
            label.raycastTarget = false;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static RectTransform Block(RectTransform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color tint,Vector2? pivot=null)
        {
            var rect = Box(parent,name,anchor,pos,size,pivot);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = tint;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform Stretch(RectTransform parent,string name)
        {
            var rect = Box(parent,name,C,Vector2.zero,Vector2.zero,C);
            rect.anchorMin = BL;
            rect.anchorMax = TR;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform Box(RectTransform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Vector2? pivot=null)
        {
            var rect = (RectTransform)new GameObject(name,typeof(RectTransform)).transform;
            rect.SetParent(parent,false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot ?? anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        private static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html,out Color color);
            return color;
        }
    }
}
