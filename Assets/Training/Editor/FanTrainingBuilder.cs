using System;
using System.IO;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace UnityTrain.Editor
{
    public static class FanTrainingBuilder
    {
        const string Root = "Assets/Training/";
        const string ScenePath = Root + "Scenes/03_FanTraining.unity";
        static Font font;
        static Material housing, blade, steel, light;
        [MenuItem("培训/打开风扇主讲场景")]
        public static void OpenForTeaching()
        {
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            { Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath); Debug.Log("当前场景有未保存修改，已定位风扇场景资产，请保存后打开。"); return; }
            EditorSceneManager.OpenScene(ScenePath);
            Selection.activeGameObject = GameObject.Find("Fan Lesson Controller");
        }
        static GameObject Part(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        [MenuItem("培训/生成风扇主讲场景")]
        public static void Generate()
        {
            var original = SceneManager.GetActiveScene();
            if (SceneManager.GetSceneByPath(ScenePath).isLoaded) throw new Exception("请先关闭风扇教学场景再重新生成，避免覆盖课堂修改");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
            try
            {
                Directory.CreateDirectory(Root + "Timelines"); Directory.CreateDirectory(Root + "Meshes"); AssetDatabase.Refresh();
                housing = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/BlueSteel.mat");
                blade = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Mint.mat");
                steel = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Graphite.mat");
                light = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Amber.mat");
                font = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/NotoSansCJKsc-Regular.otf");
                var fan = BuildModel();
                PrefabUtility.SaveAsPrefabAsset(fan, Root + "Prefabs/TrainingFan.prefab"); UnityEngine.Object.DestroyImmediate(fan);
                fan = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/TrainingFan.prefab"));
                var floor = Part("Floor", null, PrimitiveType.Cube, new Vector3(0, -.15f, 2), new Vector3(16, .3f, 14), steel);
                for (int x = -7; x <= 7; x++) Part("FloorLine_" + x, floor.transform, PrimitiveType.Cube, new Vector3(x / 16f, .52f, 0), new Vector3(.001f, .01f, 1), housing);
                var sun = new GameObject("Key Light").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.5f; sun.transform.rotation = Quaternion.Euler(45, -30, 0);
                var fill = new GameObject("Fill Light").AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = .65f; fill.color = new Color(.5f, .85f, 1); fill.transform.rotation = Quaternion.Euler(25, 140, 0);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.38f, .45f, .55f);
                var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera"; camera.gameObject.AddComponent<AudioListener>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .055f, .08f); camera.nearClipPlane = .05f;
                var brain = camera.gameObject.AddComponent<CinemachineBrain>(); brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 1);
                var root = new GameObject("Fan Lesson Controller"); var lesson = root.AddComponent<FanTrainingController>();
                lesson.fanAnimator = fan.GetComponent<Animator>(); lesson.cameraBrain = brain;
                var director = root.AddComponent<PlayableDirector>(); director.playOnAwake = false; director.extrapolationMode = DirectorWrapMode.None; lesson.inspectionDirector = director;
                var look = new GameObject("FanLookAt").transform; look.SetParent(root.transform); look.position = new Vector3(0, 2.35f, 2);
                var probe = Part("InspectionProbe", root.transform, PrimitiveType.Sphere, new Vector3(-2.5f, 2.3f, -.5f), Vector3.one * .15f, light);
                UnityEngine.Object.DestroyImmediate(probe.GetComponent<Collider>()); var probeAnimator = probe.AddComponent<Animator>(); lesson.inspectionProbe = probe.transform;
                var wide = CinematicsLessonBuilder.Vcam("Fan_Overview", root.transform, new Vector3(0, 3.2f, -7), look); wide.Priority = 30;
                var close = CinematicsLessonBuilder.Vcam("Fan_BladeCloseup", root.transform, new Vector3(-.8f, 2.7f, -1.6f), look);
                var follow = CinematicsLessonBuilder.Vcam("Fan_InspectionFollow", root.transform, new Vector3(-2.5f, 3.1f, -4.5f), look);
                follow.Follow = probe.transform; var body = follow.AddCinemachineComponent<CinemachineTransposer>();
                body.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace; body.m_FollowOffset = new Vector3(0, .8f, -4);
                body.m_XDamping = body.m_YDamping = body.m_ZDamping = .6f;
                var indicator = Part("InspectionHighlight", root.transform, PrimitiveType.Sphere, new Vector3(0, 3.9f, 2), Vector3.one * .2f, light);
                UnityEngine.Object.DestroyImmediate(indicator.GetComponent<Collider>()); indicator.SetActive(false); lesson.inspectionHighlight = indicator;
                string path = Root + "Timelines/FanInspection.playable"; AssetDatabase.DeleteAsset(path);
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>(); AssetDatabase.CreateAsset(timeline, path);
                timeline.durationMode = TimelineAsset.DurationMode.FixedLength; timeline.fixedDuration = 12; director.playableAsset = timeline;
                var shots = timeline.CreateTrack<CinemachineTrack>(null, "01 镜头巡检 Cinemachine"); director.SetGenericBinding(shots, brain);
                CinematicsLessonBuilder.Shot(shots, director, wide, 0, 4, "整体外观");
                CinematicsLessonBuilder.Shot(shots, director, close, 3, 5, "扇叶特写");
                CinematicsLessonBuilder.Shot(shots, director, follow, 7, 5, "跟随巡检探针");
                var motion = CinematicsLessonBuilder.Animation("FanInspectionProbe", "", "m_LocalPosition.x", AnimationCurve.Linear(0, -2.5f, 12, 2.5f));
                CinematicsLessonBuilder.AnimationTrack(timeline, director, probeAnimator, motion, "02 巡检探针 Animation Track");
                var active = timeline.CreateTrack<ActivationTrack>(null, "03 检查灯 Activation Track"); active.postPlaybackState = ActivationTrack.PostPlaybackState.Inactive;
                var activeClip = active.CreateDefaultClip(); activeClip.start = 2; activeClip.duration = 8; director.SetGenericBinding(active, indicator);
                var signals = timeline.CreateTrack<SignalTrack>(null, "04 检查节点 Signal Track"); var receiver = root.AddComponent<SignalReceiver>(); director.SetGenericBinding(signals, receiver);
                CinematicsLessonBuilder.Signal(signals, receiver, "FanBladeChecked", 5.5, lesson.OnInspectionCheckpoint);
                CinematicsLessonBuilder.Signal(signals, receiver, "FanHousingChecked", 9.5, lesson.OnInspectionCheckpoint);
                BuildUI(lesson); EditorSceneManager.SaveScene(scene, ScenePath);
                var builds = EditorBuildSettings.scenes.ToList(); if (!builds.Any(s => s.path == ScenePath)) builds.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = builds.ToArray();
                PatchWelcome(); AssetDatabase.SaveAssets(); Debug.Log("FAN_TRAINING_GENERATED");
            }
            finally { EditorSceneManager.CloseScene(scene, true); if (original.isLoaded) SceneManager.SetActiveScene(original); }
        }
        static GameObject BuildModel()
        {
            var fan = new GameObject("TrainingFan"); fan.transform.position = new Vector3(0, 0, 2);
            Part("Base", fan.transform, PrimitiveType.Cylinder, new Vector3(0, .14f, 0), new Vector3(2.1f, .14f, 1.5f), housing);
            Part("Stand", fan.transform, PrimitiveType.Cylinder, new Vector3(0, 1.1f, .15f), new Vector3(.2f, 1, .2f), housing);
            var motor = Part("Motor", fan.transform, PrimitiveType.Cylinder, new Vector3(0, 2.5f, .15f), new Vector3(.58f, .35f, .58f), housing); motor.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var rotor = new GameObject("Rotor").transform; rotor.SetParent(fan.transform, false); rotor.localPosition = new Vector3(0, 2.5f, -.25f);
            Part("Hub", rotor, PrimitiveType.Sphere, Vector3.zero, Vector3.one * .32f, blade);
            for (int i = 0; i < 4; i++)
            {
                var leaf = Part("Blade_" + i, rotor, PrimitiveType.Cube, Vector3.zero, new Vector3(.3f, .95f, .10f), blade);
                leaf.transform.localRotation = Quaternion.Euler(0, 0, i * 90 + 35); leaf.transform.localPosition = leaf.transform.localRotation * new Vector3(0, .59f, 0);
            }
            Mesh ring = RingMesh();
            foreach (float z in new[] { -.49f, .05f })
            {
                var cage = new GameObject("CageRing", typeof(MeshFilter), typeof(MeshRenderer)); cage.transform.SetParent(fan.transform, false);
                cage.transform.localPosition = new Vector3(0, 2.5f, z); cage.GetComponent<MeshFilter>().sharedMesh = ring; cage.GetComponent<MeshRenderer>().sharedMaterial = housing;
            }
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45; var bar = Part("CageSpoke_" + i, fan.transform, PrimitiveType.Cube, new Vector3(0, 2.5f, -.5f), new Vector3(.018f, 2.25f, .018f), housing);
                bar.transform.localRotation = Quaternion.Euler(0, 0, angle);
            }
            var animator = fan.AddComponent<Animator>(); animator.runtimeAnimatorController = FanAnimator(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; return fan;
        }
        static Mesh RingMesh()
        {
            string path = Root + "Meshes/FanCageRing.asset"; var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (saved) return saved;
            const int segments = 64, sides = 8; var vertices = new Vector3[segments * sides]; var triangles = new int[segments * sides * 6];
            for (int i = 0; i < segments; i++) for (int j = 0; j < sides; j++)
            {
                float a = i * Mathf.PI * 2 / segments, b = j * Mathf.PI * 2 / sides; float r = 1.15f + .035f * Mathf.Cos(b);
                int v = i * sides + j; vertices[v] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, .035f * Mathf.Sin(b));
                int n = ((i + 1) % segments) * sides + j, k = i * sides + (j + 1) % sides, nk = ((i + 1) % segments) * sides + (j + 1) % sides;
                int t = v * 6; triangles[t] = v; triangles[t + 1] = k; triangles[t + 2] = n; triangles[t + 3] = k; triangles[t + 4] = nk; triangles[t + 5] = n;
            }
            var mesh = new Mesh { name = "FanCageRing", vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static AnimatorController FanAnimator()
        {
            string path = Root + "Animations/Fan.controller"; var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path); if (controller) return controller;
            controller = AnimatorController.CreateAnimatorControllerAtPath(path); controller.AddParameter("Running", AnimatorControllerParameterType.Bool);
            var idle = CinematicsLessonBuilder.Animation("FanRotorIdle", "Rotor", "localEulerAnglesRaw.z", AnimationCurve.Constant(0, 1, 0));
            var spin = CinematicsLessonBuilder.Animation("FanRotorSpin", "Rotor", "localEulerAnglesRaw.z", AnimationCurve.Linear(0, 0, 1, 360));
            var settings = AnimationUtility.GetAnimationClipSettings(spin); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(spin, settings);
            var machine = controller.layers[0].stateMachine; var off = machine.AddState("Stopped"); off.motion = idle; machine.defaultState = off;
            var on = machine.AddState("Running"); on.motion = spin;
            var start = off.AddTransition(on); start.hasExitTime = false; start.duration = .15f; start.hasFixedDuration = true; start.AddCondition(AnimatorConditionMode.If, 0, "Running");
            var stop = on.AddTransition(off); stop.hasExitTime = false; stop.duration = .15f; stop.hasFixedDuration = true; stop.AddCondition(AnimatorConditionMode.IfNot, 0, "Running"); return controller;
        }
        static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h); return rt;
        }
        static UnityEngine.UI.Text Text(Transform parent, float x, float y, float w, float h, string value, int size)
        {
            var rt = Rect("Label", parent, x, y, w, h); var label = rt.gameObject.AddComponent<UnityEngine.UI.Text>(); label.font = font; label.fontSize = size; label.text = value; label.color = Color.white; label.raycastTarget = false; return label;
        }
        static UnityEngine.UI.Button Button(Transform parent, float x, float y, string label)
        {
            var rt = Rect(label, parent, x, y, 340, 54); var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(.04f, .42f, .35f);
            var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; Text(rt, 8, 8, 324, 38, label, 23).alignment = TextAnchor.MiddleCenter; return button;
        }
        static void BuildUI(FanTrainingController lesson)
        {
            var canvas = new GameObject("Fan Training Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(ChineseFont));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; canvas.GetComponent<ChineseFont>().bundledFont = font;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            var header = Rect("Header", canvas.transform, 0, 0, 1600, 96); header.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.025f, .06f, .09f, .96f);
            Text(header, 30, 12, 1540, 44, "风扇动画与巡检实训", 28);
            Text(header, 30, 58, 1540, 32, "Animation 制作旋转   /   Animator 控制开关   /   Cinemachine 镜头   /   Timeline 巡检编排", 17);
            var bottom = Rect("Controls", canvas.transform, 0, 710, 1600, 190); bottom.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.025f, .06f, .09f, .96f);
            lesson.fanButton = Button(bottom, 430, 18, "打开风扇"); lesson.inspectionButton = Button(bottom, 830, 18, "开始巡检");
            UnityEventTools.AddPersistentListener(lesson.fanButton.onClick, lesson.ToggleFan); UnityEventTools.AddPersistentListener(lesson.inspectionButton.onClick, lesson.ToggleInspection);
            lesson.statusText = Text(bottom, 30, 88, 1540, 62, "风扇停止，等待操作", 18);
            Text(bottom, 30, 155, 1540, 30, "巡检按钮：开始 → 暂停 → 继续；完成后可再次巡检。Esc 停止巡检。旋转与巡检可独立控制。", 16);
        }
        static void PatchWelcome()
        {
            var scene = SceneManager.GetSceneByPath(Root + "Scenes/00_Welcome.unity"); bool loaded = scene.isLoaded;
            if (!loaded) scene = EditorSceneManager.OpenScene(Root + "Scenes/00_Welcome.unity", OpenSceneMode.Additive);
            try
            {
                var welcome = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TrainingWelcome>()).Single();
                var canvas = scene.GetRootGameObjects().Select(g => g.GetComponent<Canvas>()).First(c => c);
                var prior = canvas.transform.Find("Cinemachine Timeline 实训"); if (prior) UnityEngine.Object.DestroyImmediate(prior.gameObject);
                if (!canvas.transform.Find("风扇动画与巡检实训"))
                {
                    var button = Button(canvas.transform, 620, 690, "风扇动画与巡检实训"); UnityEventTools.AddPersistentListener(button.onClick, welcome.EnterFanTraining);
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
