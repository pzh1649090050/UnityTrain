using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace UnityTrain.Editor
{
    [CustomEditor(typeof(FanTrainingController))]
    public class FanTrainingInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("风扇开关 → Animator；巡检按钮优先触发 Inspection Camera（Blend List），未绑定时使用 Timeline。", MessageType.Info);
            DrawDefaultInspector();
            var lesson = (FanTrainingController)target;
            foreach (string issue in FanTrainingTools.Check(lesson)) EditorGUILayout.HelpBox(issue, MessageType.Error);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("调试风扇开关")) lesson.ToggleFan();
                if (GUILayout.Button("调试巡检开始 暂停 继续")) lesson.ToggleInspection();
                if (GUILayout.Button("停止巡检")) lesson.StopInspection();
            }
        }
    }
    public class FanTrainingTools : EditorWindow
    {
        FanTrainingController lesson;
        [MenuItem("培训/风扇教学工具")]
        static void Open() { GetWindow<FanTrainingTools>("风扇教学工具"); }
        public static List<string> Check(FanTrainingController target)
        {
            var errors = new List<string>();
            if (!target) { errors.Add("当前场景没有 FanTrainingController"); return errors; }
            if (!target.fanAnimator || !target.fanAnimator.runtimeAnimatorController) errors.Add("风扇 Animator 或 Controller 未绑定");
            if (!target.UsesCameraSequence && (!target.inspectionDirector || !target.inspectionDirector.playableAsset)) errors.Add("Playable Director 或 Timeline 未绑定");
            if (!target.cameraBrain) errors.Add("Cinemachine Brain 未绑定");
            if (!target.UsesCameraSequence && (!target.inspectionProbe || !target.inspectionHighlight)) errors.Add("巡检探针或检查灯未绑定");
            if (target.UsesCameraSequence && (target.inspectionCamera.m_Instructions == null || target.inspectionCamera.m_Instructions.Length == 0)) errors.Add("巡检镜头序列为空");
            if (!target.fanButton || !target.inspectionButton || !target.statusText) errors.Add("uGUI 按钮或状态文字未绑定");
            return errors;
        }
        void OnGUI()
        {
            lesson = (FanTrainingController)EditorGUILayout.ObjectField("主讲控制器", lesson, typeof(FanTrainingController), true);
            if (!lesson && GUILayout.Button("查找当前场景")) lesson = FindObjectOfType<FanTrainingController>();
            foreach (string issue in Check(lesson)) EditorGUILayout.HelpBox(issue, MessageType.Warning);
            if (!lesson) return;
            if (GUILayout.Button("定位扇叶转轴") && lesson.fanAnimator)
            {
                Selection.activeTransform = lesson.fanAnimator.transform.Find("Rotor");
                if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.FrameSelected();
            }
            var controller = lesson.fanAnimator ? lesson.fanAnimator.runtimeAnimatorController as AnimatorController : null;
            var state = controller ? controller.layers[0].stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Running") : null;
            using (new EditorGUI.DisabledScope(Application.isPlaying || !state))
            {
                EditorGUILayout.HelpBox("修改的是 Fan.controller 的 Running 状态，影响共用该 Controller 的实例。Ctrl+Z 可撤销，保存按钮写入资源。", MessageType.Info);
                EditorGUI.BeginChangeCheck(); float speed = EditorGUILayout.Slider("状态播放速度", state ? state.speed : 1, .1f, 3);
                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(state, "调整风扇动画速度"); state.speed = speed; EditorUtility.SetDirty(state); }
                if (GUILayout.Button("保存资源")) AssetDatabase.SaveAssets();
            }
        }
    }
}
