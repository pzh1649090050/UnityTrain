using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace UnityTrain
{
    public class CinematicsLesson : MonoBehaviour
    {
        public CinemachineBrain brain;
        public CinemachineVirtualCamera[] cameras;
        public PlayableDirector director;
        public DeviceController animatorDevice;
        public Transform timelineDoor, followTarget;
        public GameObject inspectionHighlight;
        public UnityEngine.UI.Text statusText;
        public int signalCount;
        Vector3 initialTarget;
        bool movingTarget, timelineOwnsCamera;
        public bool TimelineOwnsCamera => timelineOwnsCamera;
        void Awake() { initialTarget = followTarget.position; }
        void OnEnable() { director.stopped += OnStopped; }
        void OnDisable() { director.stopped -= OnStopped; }
        public void ChooseCamera(int index)
        {
            StopSequence();
            for (int i = 0; i < cameras.Length; i++) cameras[i].Priority = i == index ? 30 : 10;
        }
        public void PlaySequence()
        {
            StopSequence(); movingTarget = false; signalCount = 0;
            timelineOwnsCamera = true; director.time = 0; director.Play();
        }
        public void PauseSequence() { if (timelineOwnsCamera) director.Pause(); }
        public void ResumeSequence() { if (timelineOwnsCamera) director.Resume(); }
        public void StopSequence()
        {
            director.Stop(); timelineOwnsCamera = false;
            timelineDoor.localRotation = Quaternion.identity;
            followTarget.position = initialTarget;
            inspectionHighlight.SetActive(false);
            animatorDevice.ApplyCommand("clearAlarm");
        }
        void OnStopped(PlayableDirector source)
        {
            timelineOwnsCamera = false; timelineDoor.localRotation = Quaternion.identity;
            followTarget.position = initialTarget; inspectionHighlight.SetActive(false);
            animatorDevice.ApplyCommand("clearAlarm");
        }
        public void ToggleTargetMotion()
        {
            ChooseCamera(2); movingTarget = !movingTarget;
        }
        public void ToggleRunning() { animatorDevice.ApplyCommand(animatorDevice.running ? "stop" : "start"); }
        public void ToggleDoor() { animatorDevice.ApplyCommand(animatorDevice.doorOpen ? "close" : "open"); }
        public void ToggleAlarm() { animatorDevice.ApplyCommand(animatorDevice.alarm ? "clearAlarm" : "alarm"); }
        public void OnAlarmSignal() { signalCount++; animatorDevice.ApplyCommand("alarm"); }
        public void OnClearSignal() { signalCount++; animatorDevice.ApplyCommand("clearAlarm"); }
        public void Back() { SceneManager.LoadScene("00_Welcome"); }
        void Update()
        {
            if (movingTarget && !timelineOwnsCamera)
                followTarget.position = initialTarget + Vector3.right * Mathf.PingPong(Time.time * 1.5f, 8);
            if (!statusText) return;
            string shot = brain.ActiveVirtualCamera == null ? "等待镜头" : brain.ActiveVirtualCamera.Name;
            statusText.text = "当前镜头：" + shot + "   |   " + (timelineOwnsCamera ? "Timeline 控制" : "Priority 控制") +
                "\n时间轴：" + director.time.ToString("F1") + " / 12.0 秒   " + director.state + "   信号：" + signalCount +
                "\n风机 Animator：Running=" + animatorDevice.running + "  DoorOpen=" + animatorDevice.doorOpen + "  Alarm=" + animatorDevice.alarm;
        }
    }
}
