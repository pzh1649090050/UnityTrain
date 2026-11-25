using UnityEngine;

namespace UnityTrain
{
    // 一个设备实例持有自己的状态；网络与本地演示共用 ApplyState。
    public class DeviceController : MonoBehaviour
    {
        public string deviceId;
        public string displayName;
        public Animator animator;
        public Renderer statusLamp;
        public bool running, doorOpen, alarm;
        MaterialPropertyBlock lampProperties;
        public DeviceData Snapshot() => new DeviceData { id = deviceId, name = displayName,
            running = running, doorOpen = doorOpen, alarm = alarm };
        void Start() { RefreshVisuals(); }
        public void ApplyState(DeviceData data)
        {
            if (data == null || data.id != deviceId) return;
            running = data.running; doorOpen = data.doorOpen; alarm = data.alarm;
            RefreshVisuals();
        }
        public bool ApplyCommand(string command)
        {
            switch (command)
            {
                case "start": running = true; break;
                case "stop": running = false; break;
                case "open": doorOpen = true; break;
                case "close": doorOpen = false; break;
                case "alarm": alarm = true; break;
                case "clearAlarm": alarm = false; break;
                default: return false;
            }
            RefreshVisuals(); return true;
        }
        public void RefreshVisuals()
        {
            if (animator)
            {
                animator.SetBool("Running", running);
                animator.SetBool("DoorOpen", doorOpen);
                animator.SetBool("Alarm", alarm);
            }
            if (!statusLamp) return;
            if (lampProperties == null) lampProperties = new MaterialPropertyBlock();
            Color color = alarm ? new Color(1, .18f, .13f) : running ? new Color(.12f, 1, .58f) : new Color(.3f, .4f, .48f);
            statusLamp.GetPropertyBlock(lampProperties);
            lampProperties.SetColor("_Color", color);
            lampProperties.SetColor("_EmissionColor", color * 1.8f);
            statusLamp.SetPropertyBlock(lampProperties);
        }
    }
}
