using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace UnityTrain.Editor
{
    public class TrainingValidator : EditorWindow
    {
        public struct Issue { public string message; public Object target; }
        readonly List<Issue> issues = new List<Issue>();
        Vector2 scroll;
        bool scanned;
        [MenuItem("培训/配置检查")]
        static void Open() { GetWindow<TrainingValidator>("配置检查"); }
        public static List<Issue> Scan()
        {
            var result = new List<Issue>();
            var devices = FindObjectsOfType<DeviceController>(true);
            foreach (var error in DeviceValidation.CheckIds(devices.Select(d => d.deviceId)))
                result.Add(new Issue { message = error, target = devices.FirstOrDefault(d => string.IsNullOrWhiteSpace(d.deviceId) || devices.Count(x => x.deviceId == d.deviceId) > 1) });
            foreach (var device in devices)
            {
                if (!device.animator || !device.statusLamp) result.Add(new Issue { message = device.name + " 缺少 Animator 或指示灯", target = device });
            }
            var routes = FindObjectsOfType<PatrolController>(true);
            if (routes.Length == 0) result.Add(new Issue { message = "当前场景没有巡检路线（请打开机房场景）" });
            foreach (var route in routes)
            {
                if (!route.cameraRig || route.points == null || route.points.Length == 0)
                    result.Add(new Issue { message = "路线摄像机缺失或路线为空", target = route });
                if (route.points != null) foreach (var point in route.points)
                    if (!point || !point.device) result.Add(new Issue { message = "巡检点为空或未绑定设备", target = point ? (Object)point : route });
            }
            foreach (var go in FindObjectsOfType<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go.gameObject) > 0)
                    result.Add(new Issue { message = go.name + " 存在丢失脚本", target = go });
            return result;
        }
        void OnGUI()
        {
            if (GUILayout.Button("检查当前场景")) { issues.Clear(); issues.AddRange(Scan()); scanned = true; }
            if (scanned && issues.Count == 0) EditorGUILayout.HelpBox("检查通过", MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var issue in issues)
            {
                EditorGUILayout.HelpBox(issue.message, MessageType.Warning);
                if (issue.target && GUILayout.Button("定位对象")) { Selection.activeObject = issue.target; EditorGUIUtility.PingObject(issue.target); }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
