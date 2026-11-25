using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace UnityTrain.Editor
{
    // 所有场景、动画和预制体通过 Unity 原生 API 生成；结果可直接在编辑器中教学。
    public static class TrainingProjectBuilder
    {
        const string Root = "Assets/Training/";
        static Font font;
        static Material metal, dark, teal, floor, white, amber;
        static readonly Color Ink = new Color(.035f, .075f, .11f, .96f);
        static readonly Color Muted = new Color(.56f, .70f, .77f);
        static readonly Color Accent = new Color(.15f, .89f, .73f);
        [MenuItem("培训/重新生成演示资源（覆盖生成场景）")]
        static void RebuildMenu()
        {
            if (!EditorUtility.DisplayDialog("重新生成", "将覆盖演示场景及四种设备预制体。请先备份课堂修改。", "生成", "取消")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Generate();
        }
        public static void Generate()
        {
            foreach (string folder in new[] { "Scenes", "Prefabs", "Animations", "Materials", "Config", "Fonts" })
                Directory.CreateDirectory(Root + folder);
            AssetDatabase.Refresh();
            font = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/NotoSansCJKsc-Regular.otf");
            if (!font) throw new Exception("缺少随项目提供的 Noto Sans CJK SC 字体");
            AssetDatabase.DeleteAsset(Root + "Fonts/WindowsChinese.fontsettings");
            metal = Material("BlueSteel", new Color(.16f, .25f, .32f), .65f);
            dark = Material("Graphite", new Color(.035f, .065f, .09f), .4f);
            teal = Material("Mint", new Color(.06f, .67f, .57f), .5f);
            floor = Material("Floor", new Color(.10f, .15f, .20f), .35f);
            white = Material("Light", new Color(.74f, .91f, 1), .2f, true);
            amber = Material("Amber", new Color(1, .58f, .12f), .4f, true);
            var controller = CreateAnimator();
            var settings = AssetDatabase.LoadAssetAtPath<TrainingSettings>(Root + "Config/Network.asset");
            if (!settings) { settings = ScriptableObject.CreateInstance<TrainingSettings>(); AssetDatabase.CreateAsset(settings, Root + "Config/Network.asset"); }
            CreateRoom(controller, settings);
            CreateWelcome();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Root + "Scenes/00_Welcome.unity", true),
                new EditorBuildSettingsScene(Root + "Scenes/01_MachineRoom.unity", true) };
            PlayerSettings.companyName = "UnityTraining";
            PlayerSettings.productName = "UnityTrain";
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            AssetDatabase.SaveAssets();
            CinematicsLessonBuilder.Generate();
            FanTrainingBuilder.Generate();
            Debug.Log("TRAINING_GENERATION_OK");
        }
        static Material Material(string name, Color color, float metallic, bool emission = false)
        {
            string path = Root + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
            material.color = color; material.SetFloat("_Metallic", metallic); material.SetFloat("_Glossiness", .4f);
            if (emission) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * .8f); }
            return material;
        }
        static GameObject Shape(string name, Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        static AnimationClip Clip(string name, string path, string property, float from, float to, float duration, bool loop = false)
        {
            string asset = Root + "Animations/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(asset);
            if (!clip) { clip = new AnimationClip { name = name }; AssetDatabase.CreateAsset(clip, asset); }
            var curve = AnimationCurve.Linear(0, from, duration, to);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings); return clip;
        }
        static void Layer(AnimatorController controller, string name, string parameter, AnimationClip idle, AnimationClip active, bool first)
        {
            AnimatorStateMachine machine;
            if (first) machine = controller.layers[0].stateMachine;
            else
            {
                controller.AddLayer(name); var layers = controller.layers;
                layers[layers.Length - 1].defaultWeight = 1; controller.layers = layers;
                machine = layers[layers.Length - 1].stateMachine;
            }
            var off = machine.AddState("Idle"); off.motion = idle;
            var on = machine.AddState("Active"); on.motion = active; machine.defaultState = off;
            var enter = off.AddTransition(on); enter.hasExitTime = false; enter.duration = .25f;
            enter.AddCondition(AnimatorConditionMode.If, 0, parameter);
            var exit = on.AddTransition(off); exit.hasExitTime = false; exit.duration = .25f;
            exit.AddCondition(AnimatorConditionMode.IfNot, 0, parameter);
        }
        static AnimatorController CreateAnimator()
        {
            string path = Root + "Animations/Device.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path); if (existing) return existing;
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach (string p in new[] { "DoorOpen", "Running", "Alarm" }) controller.AddParameter(p, AnimatorControllerParameterType.Bool);
            Layer(controller, "Door", "DoorOpen", Clip("DoorClosed", "DoorPivot", "localEulerAnglesRaw.y", 0, 0, 1),
                Clip("DoorOpen", "DoorPivot", "localEulerAnglesRaw.y", -105, -105, 1), true);
            Layer(controller, "Rotor", "Running", Clip("RotorIdle", "Rotor", "localEulerAnglesRaw.z", 0, 0, 1),
                Clip("RotorSpin", "Rotor", "localEulerAnglesRaw.z", 0, 360, 1, true), false);
            var normal = new AnimationClip { name = "BeaconNormal" };
            var blink = new AnimationClip { name = "BeaconAlarm" };
            foreach (string axis in new[] { "x", "y", "z" })
            {
                AnimationUtility.SetEditorCurve(normal, EditorCurveBinding.FloatCurve("Beacon", typeof(Transform), "m_LocalScale." + axis), AnimationCurve.Constant(0, 1, .16f));
                AnimationUtility.SetEditorCurve(blink, EditorCurveBinding.FloatCurve("Beacon", typeof(Transform), "m_LocalScale." + axis),
                    new AnimationCurve(new Keyframe(0, .16f), new Keyframe(.4f, .24f), new Keyframe(.8f, .16f)));
            }
            var loop = AnimationUtility.GetAnimationClipSettings(blink); loop.loopTime = true; AnimationUtility.SetAnimationClipSettings(blink, loop);
            AssetDatabase.CreateAsset(normal, Root + "Animations/BeaconNormal.anim"); AssetDatabase.CreateAsset(blink, Root + "Animations/BeaconAlarm.anim");
            Layer(controller, "Alarm", "Alarm", normal, blink, false); return controller;
        }
        static DeviceController Device(int index, AnimatorController controller)
        {
            string[] ids = { "cabinet-01", "fan-01", "pump-01", "control-01" };
            string[] names = { "服务器机柜", "通风风机", "循环水泵", "控制柜" };
            var root = new GameObject(ids[index]);
            var device = root.AddComponent<DeviceController>(); device.deviceId = ids[index]; device.displayName = names[index];
            device.animator = root.AddComponent<Animator>(); device.animator.runtimeAnimatorController = controller;
            device.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Shape("Base", root.transform, PrimitiveType.Cube, new Vector3(0, .12f, 0), new Vector3(2, .24f, 1.6f), dark);
            Shape("Housing", root.transform, PrimitiveType.Cube, new Vector3(0, 1.35f, .1f), new Vector3(1.7f, 2.4f, 1.2f), metal);
            var door = new GameObject("DoorPivot"); door.transform.SetParent(root.transform, false); door.transform.localPosition = new Vector3(-.85f, 1.35f, -.56f);
            Shape("Door", door.transform, PrimitiveType.Cube, new Vector3(.84f, 0, 0), new Vector3(1.64f, 2.25f, .09f), dark);
            Shape("Handle", door.transform, PrimitiveType.Cube, new Vector3(1.5f, 0, -.09f), new Vector3(.06f, .45f, .07f), teal);
            var rotor = new GameObject("Rotor"); rotor.transform.SetParent(root.transform, false); rotor.transform.localPosition = new Vector3(0, 1.4f, -.71f);
            if (index == 1 || index == 2)
            {
                Shape("Hub", rotor.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.one * .3f, teal);
                for (int blade = 0; blade < 4; blade++)
                {
                    var shape = Shape("Blade_" + blade, rotor.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(.2f, .9f, .08f), teal);
                    shape.transform.localRotation = Quaternion.Euler(0, 0, 45 + 90 * blade);
                    shape.transform.localPosition = shape.transform.localRotation * new Vector3(0, .4f, 0);
                }
            }
            else for (int slot = 0; slot < 7; slot++)
            {
                Shape("Slot_" + slot, door.transform, PrimitiveType.Cube, new Vector3(.8f, -.8f + slot * .26f, -.08f), new Vector3(1.2f, .12f, .04f), metal);
                Shape("Indicator_" + slot, door.transform, PrimitiveType.Cube, new Vector3(.3f, -.8f + slot * .26f, -.11f), new Vector3(.07f, .035f, .02f), white);
            }
            if (index == 2)
            {
                Shape("PipeLeft", root.transform, PrimitiveType.Cylinder, new Vector3(-1.05f, .55f, 0), new Vector3(.3f, .55f, .3f), teal);
                Shape("PipeRight", root.transform, PrimitiveType.Cylinder, new Vector3(1.05f, .55f, 0), new Vector3(.3f, .55f, .3f), teal);
            }
            var beacon = Shape("Beacon", root.transform, PrimitiveType.Sphere, new Vector3(.6f, 2.68f, 0), Vector3.one * .16f, white);
            device.statusLamp = beacon.GetComponent<Renderer>();
            PrefabUtility.SaveAsPrefabAsset(root, Root + "Prefabs/" + ids[index] + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/" + ids[index] + ".prefab");
            return ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<DeviceController>();
        }
        static Camera Camera(Vector3 pos, Vector3 look)
        {
            var go = new GameObject("Main Camera"); go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>(); go.AddComponent<AudioListener>();
            go.transform.position = pos; go.transform.LookAt(look);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .05f, .08f);
            camera.fieldOfView = 58; camera.nearClipPlane = .05f; return camera;
        }
        static void Lighting()
        {
            RenderSettings.ambientLight = new Color(.45f, .55f, .65f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            var light = new GameObject("Key Light").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(48, -35, 0); light.shadows = LightShadows.Soft;
            var fill = new GameObject("Fill Light").AddComponent<Light>(); fill.type = LightType.Directional;
            fill.intensity = .6f; fill.color = new Color(.35f, .75f, 1); fill.transform.rotation = Quaternion.Euler(25, 145, 0);
        }
        static void CreateRoom(AnimatorController controller, TrainingSettings settings)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Lighting();
            var room = new GameObject("Environment").transform;
            Shape("Floor", room, PrimitiveType.Cube, new Vector3(0, -.15f, 1), new Vector3(19, .3f, 16), floor);
            for (int x = -9; x <= 9; x++) Shape("GridX_" + x, room, PrimitiveType.Cube, new Vector3(x, .006f, 1), new Vector3(.015f, .01f, 16), metal);
            for (int z = -7; z <= 9; z++) Shape("GridZ_" + z, room, PrimitiveType.Cube, new Vector3(0, .006f, z), new Vector3(19, .01f, .015f), metal);
            Shape("BackWall", room, PrimitiveType.Cube, new Vector3(0, 2.2f, 5), new Vector3(19, 4.4f, .25f), dark);
            for (int x = -8; x <= 8; x += 2)
                Shape("WallRib_" + x, room, PrimitiveType.Cube, new Vector3(x, 2, 4.8f), new Vector3(.05f, 4, .07f), metal);
            Shape("WallLight", room, PrimitiveType.Cube, new Vector3(0, 3.6f, 4.75f), new Vector3(17, .055f, .1f), teal);
            Shape("WalkwayLeft", room, PrimitiveType.Cube, new Vector3(-6.7f, .025f, -1), new Vector3(.07f, .02f, 9), teal);
            Shape("WalkwayRight", room, PrimitiveType.Cube, new Vector3(6.9f, .025f, -1), new Vector3(.07f, .02f, 9), teal);
            Shape("Pipe", room, PrimitiveType.Cube, new Vector3(0, 3.2f, 4.4f), new Vector3(16, .18f, .18f), metal);
            var devicesRoot = new GameObject("Devices").transform;
            var devices = new DeviceController[4];
            for (int i = 0; i < 4; i++)
            {
                devices[i] = Device(i, controller); devices[i].transform.SetParent(devicesRoot);
                devices[i].transform.position = new Vector3(-5.1f + i * 3.4f, 0, 2);
                var sign = new GameObject("DeviceLabel").AddComponent<TextMesh>(); sign.transform.SetParent(devices[i].transform, false);
                sign.transform.localPosition = new Vector3(0, 3.05f, -.6f); sign.anchor = TextAnchor.MiddleCenter;
                sign.font = font; sign.fontSize = 50; sign.characterSize = .045f; sign.color = new Color(.65f, .92f, .93f);
                sign.text = "0" + (i + 1) + " / " + devices[i].displayName;
                sign.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
            var camera = Camera(new Vector3(-.5f, 4.4f, -8), new Vector3(.3f, 1.2f, 2));
            camera.rect = new UnityEngine.Rect(0, 0, .74f, 1);
            // 子视口之外也要每帧清屏，避免切换场景后残留入口页像素。
            var background = new GameObject("Background Camera").AddComponent<Camera>();
            background.depth = -10; background.cullingMask = 0;
            background.clearFlags = CameraClearFlags.SolidColor; background.backgroundColor = new Color(.025f, .05f, .08f);
            var system = new GameObject("Training Systems");
            var patrol = system.AddComponent<PatrolController>(); patrol.cameraRig = camera.transform;
            patrol.points = new InspectionPoint[4];
            var routeRoot = new GameObject("Inspection Route").transform;
            for (int i = 0; i < 4; i++)
            {
                var point = new GameObject("Point_0" + (i + 1)).AddComponent<InspectionPoint>();
                point.transform.SetParent(routeRoot); point.transform.position = new Vector3(devices[i].transform.position.x - .5f, 1.85f, -1.8f);
                point.device = devices[i]; patrol.points[i] = point;
                Shape("PointMarker_" + i, routeRoot, PrimitiveType.Cylinder,
                    new Vector3(point.transform.position.x, .025f, point.transform.position.z), new Vector3(.4f, .015f, .4f), teal);
            }
            var fly = camera.gameObject.AddComponent<FlyCamera>(); fly.patrol = patrol;
            var api = system.AddComponent<TrainingApi>(); api.settings = settings;
            var dashboard = system.AddComponent<TrainingDashboard>(); dashboard.devices = devices; dashboard.api = api; dashboard.patrol = patrol;
            camera.gameObject.AddComponent<DevicePicker>().dashboard = dashboard;
            BuildDashboard(dashboard);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), Root + "Scenes/01_MachineRoom.unity");
        }
        static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false); rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h); return rt;
        }
        static Transform Canvas()
        {
            var go = new GameObject("Training Canvas", typeof(RectTransform), typeof(UnityEngine.Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(ChineseFont));
            go.GetComponent<UnityEngine.Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.GetComponent<ChineseFont>().bundledFont = font;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)); return go.transform;
        }
        static Transform Panel(string name, Transform parent, float x, float y, float w, float h, Color color)
        { var rt = Rect(name, parent, x, y, w, h); rt.gameObject.AddComponent<UnityEngine.UI.Image>().color = color; return rt; }
        static UnityEngine.UI.Text Text(string name, Transform parent, float x, float y, float w, float h, string value, int size = 20, Color? color = null)
        {
            var rt = Rect(name, parent, x, y, w, h); var label = rt.gameObject.AddComponent<UnityEngine.UI.Text>();
            label.font = font; label.fontSize = size; label.color = color ?? Color.white; label.text = value;
            label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; return label;
        }
        static UnityEngine.UI.Button Button(Transform parent, float x, float y, float w, string label, bool accent = false)
        {
            var rt = Rect(label, parent, x, y, w, 38);
            var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = accent ? new Color(.06f, .48f, .40f) : new Color(.10f, .18f, .23f);
            var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(.65f, 1, .92f); colors.pressedColor = new Color(.4f, .75f, .7f); button.colors = colors;
            Text("Label", rt, 6, 5, w - 12, 28, label, 17).alignment = TextAnchor.MiddleCenter; return button;
        }
        static UnityEngine.UI.Button Action(TrainingDashboard dashboard, Transform parent, float x, float y, float w, string label, string command, bool accent = false, int index = 0)
        {
            var button = Button(parent, x, y, w, label, accent);
            var action = button.gameObject.AddComponent<DashboardAction>(); action.dashboard = dashboard; action.action = command; action.deviceIndex = index;
            UnityEventTools.AddPersistentListener(button.onClick, action.Execute); return button;
        }
        static UnityEngine.UI.InputField Input(Transform parent, float x, float y, float w, float h, string placeholder)
        {
            var rt = Rect("Input", parent, x, y, w, h); rt.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.065f, .12f, .16f);
            var field = rt.gameObject.AddComponent<UnityEngine.UI.InputField>();
            field.textComponent = Text("Value", rt, 10, 7, w - 20, h - 14, "", 16);
            field.placeholder = Text("Placeholder", rt, 10, 7, w - 20, h - 14, placeholder, 16, Muted); field.characterLimit = 240; return field;
        }
        static void BuildDashboard(TrainingDashboard d)
        {
            var canvas = Canvas();
            var header = Panel("Header", canvas, 0, 0, 1600, 90, Ink);
            Panel("Accent", header, 24, 24, 5, 43, Accent);
            Text("Title", header, 44, 8, 1100, 46, "UNITY TRAIN / 设备机房巡检", 27);
            Text("Subtitle", header, 44, 55, 900, 24, "2022.3.55f1c1    /    基础操作 · 动画 · 脚本 · HTTP · Editor", 15, Muted);
            Action(d, header, 1450, 25, 125, "返回入口", "back");
            var list = Panel("DeviceList", canvas, 24, 115, 220, 236, Ink);
            Text("Heading", list, 16, 10, 190, 36, "设备目录 / 04", 20, Accent);
            for (int i = 0; i < 4; i++) Action(d, list, 12, 52 + i * 44, 196, "0" + (i + 1) + "  " + d.devices[i].displayName, "select", false, i);
            var side = Panel("Details", canvas, 1200, 115, 376, 590, Ink);
            Text("Heading", side, 20, 16, 330, 30, "设备交互 / DEVICE", 20, Accent);
            d.detailText = Text("DeviceDetails", side, 20, 60, 336, 136, "请选择设备", 18);
            Action(d, side, 20, 200, 158, "启动设备", "start", true); Action(d, side, 194, 200, 158, "停止设备", "stop");
            Action(d, side, 20, 246, 158, "打开柜门", "open"); Action(d, side, 194, 246, 158, "关闭柜门", "close");
            Action(d, side, 20, 292, 158, "模拟告警", "alarm"); Action(d, side, 194, 292, 158, "清除告警", "clearAlarm");
            Text("RecordHeading", side, 20, 346, 336, 36, "检查结果", 20, Accent);
            d.noteField = Input(side, 20, 390, 332, 66, "输入检查备注（可选）"); d.noteField.lineType = UnityEngine.UI.InputField.LineType.MultiLineNewline;
            Action(d, side, 20, 470, 158, "记录正常", "normal", true); Action(d, side, 194, 470, 158, "记录异常", "abnormal");
            Action(d, side, 20, 526, 332, "提交巡检记录", "submit", true);
            var route = Panel("Patrol", canvas, 24, 668, 1150, 96, Ink);
            d.routeText = Text("RouteStatus", route, 18, 3, 1100, 48, "巡检准备就绪", 15);
            string[] labels = { "开始巡检", "暂停", "继续", "手动漫游", "下一点", "重置" };
            string[] actions = { "begin", "pause", "resume", "manual", "next", "reset" };
            for (int i = 0; i < labels.Length; i++) Action(d, route, 18 + i * 183, 53, 171, labels[i], actions[i], i == 0);
            var network = Panel("Network", canvas, 24, 776, 1150, 100, Ink);
            var mode = Action(d, network, 12, 12, 250, "本地模式 · 切换 HTTP", "mode", true); d.modeText = mode.GetComponentInChildren<UnityEngine.UI.Text>();
            d.addressField = Input(network, 274, 12, 650, 38, "http://127.0.0.1:8080");
            UnityEventTools.AddPersistentListener(d.addressField.onEndEdit, d.SetAddress);
            var sync = Action(d, network, 936, 12, 200, "同步设备状态", "sync");
            d.networkButtons = new[] { mode, sync };
            d.networkText = Text("NetworkStatus", network, 14, 56, 1120, 40, "本地演示模式", 13, Muted);
            var recordPanel = Panel("Records", canvas, 1200, 717, 376, 159, Ink);
            var scroll = Rect("ScrollView", recordPanel, 16, 12, 344, 135).gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            var viewport = Rect("Viewport", scroll.transform, 0, 0, 344, 135);
            viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = Ink; viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            var content = Rect("Content", viewport, 0, 0, 340, 0);
            var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>(); fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            d.recordsText = Text("RecordsText", content, 0, 0, 340, 0, "检查记录（未提交）", 16, Muted);
            var textRt = d.recordsText.rectTransform; textRt.anchorMax = new Vector2(1, 1); textRt.sizeDelta = Vector2.zero;
            var textFit = textRt.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>(); textFit.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(); layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            // Text 的高度由父布局计算，避免同一轴上双重控制。
            UnityEngine.Object.DestroyImmediate(textFit);
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            Text("Controls", canvas, 280, 120, 890, 46, "WASD 移动   ·   按住右键观察   ·   Q / E 升降   ·   左键选择设备", 17, Muted);
        }
        static void CreateWelcome()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera(new Vector3(0, 0, -10), Vector3.zero);
            var controller = new GameObject("Welcome").AddComponent<TrainingWelcome>();
            var canvas = Canvas(); Panel("Background", canvas, 0, 0, 1600, 900, new Color(.025f, .05f, .075f));
            Panel("AccentLine", canvas, 100, 135, 5, 94, Accent);
            Text("Eyebrow", canvas, 130, 130, 1200, 36, "UNITY TRAINING / 2022.3.55f1c1", 20, Accent);
            Text("Title", canvas, 128, 183, 1300, 86, "从基础操作，到设备机房巡检", 48);
            Text("Subtitle", canvas, 130, 295, 1300, 70, "一个可以运行、拆解、修改的 Unity 综合培训项目。\n面向新手 · 五天课程 · 中文练习与参考答案 · 本地 HTTP 服务", 24, Muted);
            string[] titles = { "01 / 场景与组件", "02 / 动画与状态", "03 / 交互与巡检", "04 / HTTP 与 Editor" };
            string[] body = { "层级 · Transform · 材质\nPrefab · Camera · Light", "Animation Clip · Animator\n柜门 · 风机 · 告警", "射线选择 · 摄像机漫游\n巡检点 · 记录 · 状态机", "GET / POST · JSON · 超时\nInspector · Window · Handles" };
            for (int i = 0; i < 4; i++)
            {
                var card = Panel("Course_" + i, canvas, 130 + i * 345, 430, 320, 180, Ink);
                Text("Title", card, 20, 22, 280, 36, titles[i], 24, Accent);
                Text("Body", card, 20, 75, 280, 94, body[i], 18, Muted);
            }
            var enter = Button(canvas, 130, 690, 300, "进入机房演示  →", true); UnityEventTools.AddPersistentListener(enter.onClick, controller.Enter);
            var quit = Button(canvas, 450, 690, 140, "退出程序"); UnityEventTools.AddPersistentListener(quit.onClick, controller.Quit);
            Text("Footer", canvas, 130, 790, 1300, 60, "先从本地模式体验；学习网络章节时，运行 Server/启动服务.cmd 并切换 HTTP 模式。\n课程与练习：Docs/课程指南.md   /   源码：Assets/Training", 18, Muted);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), Root + "Scenes/00_Welcome.unity");
        }
        [MenuItem("培训/构建 Windows 演示")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = "Builds/Windows/UnityTrain.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows 构建失败：" + report.summary.result);
            Debug.Log("TRAINING_BUILD_OK " + report.summary.totalSize);
        }
    }
}
