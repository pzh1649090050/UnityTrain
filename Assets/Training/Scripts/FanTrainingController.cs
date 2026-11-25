using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace UnityTrain
{
    // 本课主讲脚本：按钮只负责调用方法，动画交给 Animator，镜头顺序交给 Timeline。
    public class FanTrainingController : MonoBehaviour
    {
        public Animator fanAnimator;
        public PlayableDirector inspectionDirector;
        public CinemachineBrain cameraBrain;
        [Tooltip("可选：绑定场景中的“巡检”Blend List Camera 后，按钮优先播放它。")]
        public CinemachineBlendListCamera inspectionCamera;
        public Transform inspectionProbe;
        public GameObject inspectionHighlight;
        public UnityEngine.UI.Button fanButton, inspectionButton;
        public UnityEngine.UI.Text statusText;
        public bool IsRunning { get; private set; }
        public bool IsInspecting { get; private set; }
        public int CheckpointCount { get; private set; }
        public bool UsesCameraSequence => inspectionCamera != null;
        public bool HasInspectionSetup => cameraBrain && (UsesCameraSequence ?
            inspectionCamera.m_Instructions != null && inspectionCamera.m_Instructions.Length > 0 :
            inspectionDirector && inspectionDirector.playableAsset && inspectionProbe && inspectionHighlight);
        Vector3 initialProbePosition;
        int originalInspectionPriority;
        float sequenceTime;

        void Awake() { if (inspectionProbe) initialProbePosition = inspectionProbe.localPosition; }
        void OnEnable() { if (inspectionDirector) inspectionDirector.stopped += OnInspectionStopped; }
        void OnDisable() { if (inspectionDirector) inspectionDirector.stopped -= OnInspectionStopped; }
        void Start()
        {
            if (fanAnimator) fanAnimator.SetBool("Running", false);
            if (inspectionCamera)
            {
                originalInspectionPriority = inspectionCamera.Priority;
                inspectionCamera.enabled = false;
            }
            RefreshLabels();
        }

        // 对应风扇开关按钮。参数名必须与 Animator 的 Bool 参数完全一致。
        public void ToggleFan()
        {
            if (!fanAnimator) { Debug.LogWarning("请先绑定风扇 Animator", this); return; }
            IsRunning = !IsRunning;
            fanAnimator.SetBool("Running", IsRunning);
            RefreshLabels();
        }

        // Blend List 模式为开始/停止；未绑定它时使用 Timeline 开始/暂停/继续。
        public void ToggleInspection()
        {
            if (!HasInspectionSetup)
            {
                Debug.LogWarning("请完成 Cinemachine 与 Timeline 章节的巡检引用绑定", this);
                RefreshLabels(); return;
            }
            if (UsesCameraSequence)
            {
                if (IsInspecting) StopInspection();
                else
                {
                    if (inspectionDirector) inspectionDirector.Stop();
                    originalInspectionPriority = inspectionCamera.Priority;
                    inspectionCamera.PreviousStateIsValid = false;
                    inspectionCamera.Priority = 10000;
                    inspectionCamera.enabled = true;
                    sequenceTime = 0;
                    IsInspecting = true;
                    RefreshLabels();
                }
                return;
            }
            if (!IsInspecting)
            {
                CheckpointCount = 0; inspectionDirector.time = 0;
                IsInspecting = true; inspectionDirector.Play();
            }
            else if (inspectionDirector.state == PlayState.Playing) inspectionDirector.Pause();
            else inspectionDirector.Resume();
            RefreshLabels();
        }

        public void StopInspection()
        {
            if (inspectionCamera)
            {
                inspectionCamera.enabled = false;
                inspectionCamera.Priority = originalInspectionPriority;
                inspectionCamera.PreviousStateIsValid = false;
            }
            if (inspectionDirector) inspectionDirector.Stop();
            OnInspectionStopped(null);
        }

        // Signal Receiver 在时间轴的检查节点调用；不改风扇开关状态。
        public void OnInspectionCheckpoint() { CheckpointCount++; }

        void OnInspectionStopped(PlayableDirector director)
        {
            IsInspecting = false;
            if (inspectionProbe) inspectionProbe.localPosition = initialProbePosition;
            if (inspectionHighlight) inspectionHighlight.SetActive(false);
            RefreshLabels();
        }
        void RefreshLabels()
        {
            if (fanButton)
                fanButton.GetComponentInChildren<UnityEngine.UI.Text>().text =
                    IsRunning ? "关闭风扇" : "打开风扇";
            if (inspectionButton)
            {
                inspectionButton.interactable = HasInspectionSetup;
                string caption = !HasInspectionSetup ? "巡检待配置" : !IsInspecting ? "开始巡检" :
                    UsesCameraSequence ? "停止巡检" :
                    inspectionDirector.state == PlayState.Playing ? "暂停巡检" : "继续巡检";
                inspectionButton.GetComponentInChildren<UnityEngine.UI.Text>().text = caption;
            }
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) StopInspection();
            if (!statusText) return;
            string cameraName = !cameraBrain || cameraBrain.ActiveVirtualCamera == null ?
                "待配置" : cameraBrain.ActiveVirtualCamera.Name;
            if (UsesCameraSequence)
            {
                if (IsInspecting) sequenceTime += Time.deltaTime;
                string child = inspectionCamera.LiveChild == null ? "待机" : inspectionCamera.LiveChild.Name;
                statusText.text = "风扇：" + (IsRunning ? "运行" : "停止") +
                    "   巡检：" + (IsInspecting ? "播放中" : "待机") +
                    "   已运行：" + sequenceTime.ToString("F1") + " 秒\n巡检镜头：" + child;
                return;
            }
            double time = inspectionDirector ? inspectionDirector.time : 0;
            string patrol = !IsInspecting ? "待机" :
                inspectionDirector.state == PlayState.Playing ? "播放中" : "已暂停";
            statusText.text = "风扇：" + (IsRunning ? "运行" : "停止") + "   巡检：" + patrol +
                "   时间：" + time.ToString("F1") + "/12 秒   检查节点：" + CheckpointCount +
                "/2\n当前镜头：" + cameraName;
        }
    }
}
