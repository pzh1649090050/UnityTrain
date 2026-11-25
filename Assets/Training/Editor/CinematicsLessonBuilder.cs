using System;
using System.IO;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace UnityTrain.Editor
{
    public static class CinematicsLessonBuilder
    {
        const string Root = "Assets/Training/";
        const string ScenePath = Root + "Scenes/02_CinematicsLab.unity";
        static Font font;
        [MenuItem("培训/生成镜头与时间轴实训场景")]
        public static void Generate()
        {
            Directory.CreateDirectory(Root + "Timelines"); AssetDatabase.Refresh();
            var original = SceneManager.GetActiveScene();
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
                if (!AssetDatabase.CopyAsset(Root + "Scenes/01_MachineRoom.unity", ScenePath)) throw new Exception("无法创建教学场景副本");
            Scene scene = SceneManager.GetSceneByPath(ScenePath); bool alreadyLoaded = scene.isLoaded;
            if (!alreadyLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                string[] retained = { "Environment", "Devices", "Main Camera", "Key Light", "Fill Light" };
                foreach (var go in scene.GetRootGameObjects()) if (!retained.Contains(go.name)) UnityEngine.Object.DestroyImmediate(go);
                var camera = scene.GetRootGameObjects().Single(g => g.name == "Main Camera").GetComponent<Camera>();
                foreach (var component in camera.GetComponents<MonoBehaviour>()) UnityEngine.Object.DestroyImmediate(component);
                camera.rect = new UnityEngine.Rect(0, 0, 1, 1);
                var brain = camera.gameObject.AddComponent<CinemachineBrain>();
                brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 1);
                var devices = scene.GetRootGameObjects().Single(g => g.name == "Devices").GetComponentsInChildren<DeviceController>();
                var cabinet = devices.Single(d => d.deviceId == "cabinet-01");
                var fan = devices.Single(d => d.deviceId == "fan-01");
                // 时间轴教学机柜由 Animation Track 单独控制，风机继续由 Animator 状态机控制。
                cabinet.enabled = false; cabinet.animator.runtimeAnimatorController = null;
                var system = new GameObject("Cinematic Lesson");
                var lesson = system.AddComponent<CinematicsLesson>(); lesson.brain = brain; lesson.animatorDevice = fan;
                lesson.timelineDoor = cabinet.transform.Find("DoorPivot");
                var director = system.AddComponent<PlayableDirector>(); director.playOnAwake = false; director.extrapolationMode = DirectorWrapMode.None;
                lesson.director = director;
                var look = new GameObject("OverviewLookAt").transform; look.SetParent(system.transform); look.position = new Vector3(0, 1.3f, 2);
                var cabinetLook = new GameObject("CabinetLookAt").transform; cabinetLook.SetParent(system.transform); cabinetLook.position = cabinet.transform.position + Vector3.up * 1.3f;
                var target = new GameObject("FollowTarget"); target.transform.SetParent(system.transform); target.transform.position = new Vector3(-4, 1.3f, 0);
                var targetAnimator = target.AddComponent<Animator>(); lesson.followTarget = target.transform;
                var overview = Vcam("VCam_Overview", system.transform, new Vector3(0, 5, -10), look);
                var closeup = Vcam("VCam_Cabinet", system.transform, new Vector3(-6.7f, 2.4f, -2.4f), cabinetLook);
                var follow = Vcam("VCam_Follow", system.transform, new Vector3(-4, 3.3f, -5), target.transform);
                follow.Follow = target.transform;
                var transposer = follow.AddCinemachineComponent<CinemachineTransposer>();
                transposer.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
                transposer.m_FollowOffset = new Vector3(0, 2, -5); transposer.m_XDamping = transposer.m_YDamping = transposer.m_ZDamping = .7f;
                lesson.cameras = new[] { overview, closeup, follow }; overview.Priority = 30;
                var highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere); highlight.name = "TimelineInspectionHighlight";
                highlight.transform.SetParent(system.transform); highlight.transform.position = cabinet.transform.position + Vector3.up * 3.3f;
                highlight.transform.localScale = Vector3.one * .35f;
                highlight.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Amber.mat");
                UnityEngine.Object.DestroyImmediate(highlight.GetComponent<Collider>()); highlight.SetActive(false); lesson.inspectionHighlight = highlight;
                string path = Root + "Timelines/MachineRoomSequence.playable";
                AssetDatabase.DeleteAsset(path);
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>(); AssetDatabase.CreateAsset(timeline, path);
                timeline.durationMode = TimelineAsset.DurationMode.FixedLength; timeline.fixedDuration = 12;
                director.playableAsset = timeline;
                var cameraTrack = timeline.CreateTrack<CinemachineTrack>(null, "01 Cinemachine 镜头编排");
                director.SetGenericBinding(cameraTrack, brain);
                Shot(cameraTrack, director, overview, 0, 5, "全景"); Shot(cameraTrack, director, closeup, 4, 5, "机柜特写"); Shot(cameraTrack, director, follow, 8, 4, "跟随巡检目标");
                var doorClip = Animation("CabinetDoorSequence", "DoorPivot", "localEulerAnglesRaw.y",
                    new AnimationCurve(new Keyframe(0, 0), new Keyframe(3, 0), new Keyframe(5, 105), new Keyframe(7, 105), new Keyframe(9, 0), new Keyframe(12, 0)));
                AnimationTrack(timeline, director, cabinet.animator, doorClip, "02 Animation 机柜门开合");
                var motion = Animation("FollowTargetMotion", "", "m_LocalPosition.x", AnimationCurve.Linear(0, -4, 12, 4));
                AnimationTrack(timeline, director, targetAnimator, motion, "03 Animation 跟随目标移动");
                var activeTrack = timeline.CreateTrack<ActivationTrack>(null, "04 Activation 检修指示");
                activeTrack.postPlaybackState = ActivationTrack.PostPlaybackState.Inactive;
                var activeClip = activeTrack.CreateDefaultClip(); activeClip.start = 3; activeClip.duration = 5;
                director.SetGenericBinding(activeTrack, highlight);
                var signalTrack = timeline.CreateTrack<SignalTrack>(null, "05 Signal 告警事件");
                var receiver = system.AddComponent<SignalReceiver>(); director.SetGenericBinding(signalTrack, receiver);
                Signal(signalTrack, receiver, "Alarm", 6, lesson.OnAlarmSignal); Signal(signalTrack, receiver, "ClearAlarm", 10, lesson.OnClearSignal);
                font = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/NotoSansCJKsc-Regular.otf"); BuildUI(lesson);
                EditorSceneManager.SaveScene(scene);
                var buildScenes = EditorBuildSettings.scenes.ToList();
                if (!buildScenes.Any(s => s.path == ScenePath)) buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
                PatchWelcome(); AssetDatabase.SaveAssets();
                Debug.Log("CINEMATICS_LESSON_GENERATED");
            }
            finally
            {
                if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true);
                if (original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }
        internal static CinemachineVirtualCamera Vcam(string name, Transform parent, Vector3 position, Transform lookAt)
        {
            var camera = new GameObject(name).AddComponent<CinemachineVirtualCamera>(); camera.transform.SetParent(parent);
            camera.transform.position = position; camera.LookAt = lookAt; camera.Priority = 10; camera.m_Lens.FieldOfView = 55;
            camera.AddCinemachineComponent<CinemachineComposer>(); return camera;
        }
        internal static void Shot(CinemachineTrack track, PlayableDirector director, CinemachineVirtualCamera camera, double start, double duration, string name)
        {
            var clip = track.CreateClip<CinemachineShot>(); clip.start = start; clip.duration = duration; clip.displayName = name;
            var shot = (CinemachineShot)clip.asset; shot.VirtualCamera.exposedName = new PropertyName(camera.name);
            director.SetReferenceValue(shot.VirtualCamera.exposedName, camera);
        }
        internal static AnimationClip Animation(string name, string path, string property, AnimationCurve curve)
        {
            string asset = Root + "Animations/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(asset);
            if (!clip) { clip = new AnimationClip { name = name }; AssetDatabase.CreateAsset(clip, asset); }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve); return clip;
        }
        internal static void AnimationTrack(TimelineAsset timeline, PlayableDirector director, Animator animator, AnimationClip motion, string name)
        {
            var track = timeline.CreateTrack<AnimationTrack>(null, name); track.trackOffset = TrackOffset.ApplySceneOffsets;
            var clip = track.CreateClip<AnimationPlayableAsset>(); clip.start = 0; clip.duration = 12;
            ((AnimationPlayableAsset)clip.asset).clip = motion; director.SetGenericBinding(track, animator);
        }
        internal static void Signal(SignalTrack track, SignalReceiver receiver, string name, double time, UnityAction action)
        {
            string path = Root + "Timelines/" + name + ".signal";
            var asset = AssetDatabase.LoadAssetAtPath<SignalAsset>(path);
            if (!asset) { asset = ScriptableObject.CreateInstance<SignalAsset>(); AssetDatabase.CreateAsset(asset, path); }
            var signal = track.CreateMarker<SignalEmitter>(time); signal.asset = asset; signal.emitOnce = true; signal.retroactive = false;
            var reaction = new UnityEvent(); UnityEventTools.AddPersistentListener(reaction, action); receiver.AddReaction(asset, reaction);
        }
        static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h); return rt;
        }
        static UnityEngine.UI.Text Text(Transform parent, float x, float y, float w, float h, string text, int size)
        {
            var rt = Rect("Label", parent, x, y, w, h); var label = rt.gameObject.AddComponent<UnityEngine.UI.Text>();
            label.font = font; label.text = text; label.fontSize = size; label.raycastTarget = false; label.color = Color.white; return label;
        }
        static UnityEngine.UI.Button Button(Transform parent, float x, float y, float w, string text)
        {
            var rt = Rect(text, parent, x, y, w, 42); var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(.06f, .32f, .3f);
            var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            Text(rt, 6, 6, w - 12, 30, text, 17).alignment = TextAnchor.MiddleCenter; return button;
        }
        static void Action(CinematicsLesson lesson, Transform parent, float x, float y, float w, string label, string action)
        {
            var button = Button(parent, x, y, w, label); var binding = button.gameObject.AddComponent<CinematicsAction>(); binding.lesson = lesson; binding.action = action;
            UnityEventTools.AddPersistentListener(button.onClick, binding.Execute);
        }
        static void BuildUI(CinematicsLesson lesson)
        {
            var root = new GameObject("Cinematics Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(ChineseFont));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; root.GetComponent<ChineseFont>().bundledFont = font;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            var header = Rect("Header", root.transform, 0, 0, 1600, 100); header.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.025f, .07f, .10f, .97f);
            Text(header, 28, 10, 1320, 44, "镜头与时间轴实训  /  Cinemachine 2.9.7 + Timeline 1.6.2", 25);
            Text(header, 28, 57, 1320, 32, "Animation 记录属性   →   Animator 切换状态   →   Cinemachine 驱动镜头   →   Timeline 编排演示", 17);
            Action(lesson, header, 1420, 28, 150, "返回入口", "back");
            var bottom = Rect("LessonControls", root.transform, 20, 648, 1560, 232); bottom.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.025f, .07f, .10f, .97f);
            lesson.statusText = Text(bottom, 18, 12, 1520, 84, "准备就绪", 17);
            string[] labels = { "全景镜头", "机柜特写", "跟随镜头", "目标移动开关", "风机启停", "风机柜门", "风机告警" };
            string[] actions = { "overview", "cabinet", "follow", "target", "running", "door", "alarm" };
            for (int i = 0; i < labels.Length; i++) Action(lesson, bottom, 18 + i * 218, 108, 204, labels[i], actions[i]);
            string[] playLabels = { "播放 12 秒时间轴", "暂停时间轴", "继续时间轴", "停止并复位" };
            string[] playActions = { "play", "pause", "resume", "stop" };
            for (int i = 0; i < playLabels.Length; i++) Action(lesson, bottom, 18 + i * 384, 172, 368, playLabels[i], playActions[i]);
        }
        static void PatchWelcome()
        {
            string path = Root + "Scenes/00_Welcome.unity";
            var scene = SceneManager.GetSceneByPath(path); bool loaded = scene.isLoaded;
            if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var welcome = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TrainingWelcome>()).Single();
                var canvas = scene.GetRootGameObjects().Select(g => g.GetComponent<Canvas>()).First(c => c);
                var existing = canvas.transform.Find("Cinemachine Timeline 实训");
                if (!existing)
                {
                    var button = Button(canvas.transform, 620, 690, 400, "Cinemachine Timeline 实训");
                    UnityEventTools.AddPersistentListener(button.onClick, welcome.EnterCinematics);
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
        }
        public static void Inspect()
        {
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i)).Select(s => new { s.path, s.isDirty }).ToArray();
            File.WriteAllText("Tools/editor-inspection.txt", string.Join("\n", scenes.Select(s => s.path + " dirty=" + s.isDirty)));
        }
    }
}
