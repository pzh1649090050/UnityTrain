using UnityEditor;
using UnityEngine;
namespace UnityTrain.Editor
{
    [CustomEditor(typeof(DeviceController))]
    public class DeviceInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("稳定 ID 连接场景、HTTP 设备和巡检记录。状态调试仅在播放时使用。", MessageType.Info);
            DrawDefaultInspector();
            var device = (DeviceController)target;
            if (string.IsNullOrWhiteSpace(device.deviceId)) EditorGUILayout.HelpBox("设备 ID 不能为空", MessageType.Error);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("启停")) device.ApplyCommand(device.running ? "stop" : "start");
                if (GUILayout.Button("柜门")) device.ApplyCommand(device.doorOpen ? "close" : "open");
                if (GUILayout.Button("告警")) device.ApplyCommand(device.alarm ? "clearAlarm" : "alarm");
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
