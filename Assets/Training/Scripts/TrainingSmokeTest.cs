using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityTrain
{
    // 仅显式传入命令行标志才运行，用于验证真实 Windows Player，而非模拟组件行为。
    public class TrainingSmokeTest : MonoBehaviour
    {
        [Serializable] class Result { public bool passed; public string error; public string[] checks; }
        readonly List<string> checks = new List<string>();
        string output, baseUrl, fixtureUrl;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--training-smoke") < 0) return;
            var go = new GameObject("Explicit Player Smoke Test"); DontDestroyOnLoad(go); go.AddComponent<TrainingSmokeTest>();
        }
        static string Arg(string key, string fallback)
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }
        void Check(bool value, string message)
        {
            if (!value) throw new Exception(message);
            checks.Add(message); Debug.Log("SMOKE_OK " + message);
        }
        IEnumerator Start()
        {
            output = Arg("--training-output", Application.persistentDataPath);
            baseUrl = Arg("--training-api", "http://127.0.0.1:8080");
            fixtureUrl = Arg("--training-fixture", "http://127.0.0.1:8082");
            Directory.CreateDirectory(output);
            var run = Run(); string error = null;
            while (true)
            {
                bool next;
                try { next = run.MoveNext(); }
                catch (Exception exception) { error = exception.ToString(); break; }
                if (!next) break;
                yield return run.Current;
            }
            File.WriteAllText(Path.Combine(output, "player-smoke.json"), JsonUtility.ToJson(new Result { passed = error == null, error = error, checks = checks.ToArray() }, true));
            if (error != null) Debug.LogError(error);
            Application.Quit(error == null ? 0 : 1);
        }
        IEnumerator WaitForRequest(TrainingDashboard d)
        {
            float end = Time.realtimeSinceStartup + 12;
            while (d.Busy && Time.realtimeSinceStartup < end) yield return null;
        }
        IEnumerator Run()
        {
            yield return new WaitForSeconds(1);
            Check(SceneManager.GetActiveScene().name == "00_Welcome", "入口场景已加载");
            var label = FindObjectOfType<UnityEngine.UI.Text>();
            label.font.RequestCharactersInTexture("机房巡检", 20);
            Check(label.font.HasCharacter('巡'), "随包中文字体可用");
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "welcome.png"));
            FindObjectOfType<TrainingWelcome>().Enter(); yield return new WaitForSeconds(1);
            var d = FindObjectOfType<TrainingDashboard>();
            Check(d && d.devices.Length == 4, "入口成功进入完整机房");
            Check(Mathf.Abs(d.patrol.cameraRig.position.z + 8) < .01f, "无输入时初始摄像机保持生成位置");
            var fan = Array.Find(d.devices, device => device.deviceId == "fan-01"); d.Select(d.devices[0]);
            var selectButton = Array.Find(FindObjectsOfType<DashboardAction>(), action => action.action == "select" && action.deviceIndex == 1);
            selectButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(d.Selected == fan, "持久化 uGUI 按钮事件正确选择设备");
            foreach (var device in d.devices)
            {
                Vector3 target = device.transform.position + Vector3.up * 1.3f;
                bool hit = Physics.Raycast(target + Vector3.back * 3, Vector3.forward, out var picked, 5);
                Check(hit && picked.collider.GetComponentInParent<DeviceController>() == device, "设备碰撞体可由射线选中 " + device.deviceId);
            }
            d.Command("start"); d.Command("open"); d.Command("alarm");
            yield return new WaitForSeconds(.8f);
            Check(fan.running && fan.doorOpen && fan.alarm && fan.animator.GetBool("Running"), "本地设备状态及 Animator 参数同步");
            Check(Mathf.Abs(Mathf.DeltaAngle(0, fan.transform.Find("DoorPivot").localEulerAngles.y)) > 60, "柜门 Animation Clip 实际播放");
            float rotation = fan.transform.Find("Rotor").localEulerAngles.z;
            yield return new WaitForSeconds(.15f);
            Check(Mathf.Abs(Mathf.DeltaAngle(rotation, fan.transform.Find("Rotor").localEulerAngles.z)) > 10, "风机循环动画实际旋转");
            d.Command("stop"); d.Command("close"); d.Command("clearAlarm");
            yield return new WaitForSeconds(.6f);
            Check(!fan.running && !fan.alarm && Mathf.Abs(Mathf.DeltaAngle(0, fan.transform.Find("DoorPivot").localEulerAngles.y)) < 2, "停机、关门与清除告警恢复状态");
            d.patrol.speed = 12; d.patrol.Begin(); d.patrol.Pause();
            Vector3 pos = d.patrol.cameraRig.position; yield return new WaitForSeconds(.2f);
            Check(Vector3.Distance(pos, d.patrol.cameraRig.position) < .01f, "暂停巡检时摄像机不移动");
            d.patrol.Resume(); yield return new WaitForSeconds(.1f); d.patrol.Manual();
            pos = d.patrol.cameraRig.position; yield return new WaitForSeconds(.2f);
            Check(d.patrol.Progress.State == PatrolState.Manual && Vector3.Distance(pos, d.patrol.cameraRig.position) < .01f, "切换手动后自动控制停止");
            d.patrol.Begin();
            for (int i = 0; i < 4; i++)
            {
                float end = Time.realtimeSinceStartup + 8;
                while (d.patrol.Progress.State != PatrolState.AtPoint && Time.realtimeSinceStartup < end) yield return null;
                Check(d.patrol.Progress.Index == i && d.Selected == d.patrol.points[i].device, "巡检到达并选中设备 " + i);
                d.noteField.text = "Player 自动验收 " + i; d.Record(i != 2); d.NextPoint();
            }
            Check(d.patrol.Progress.State == PatrolState.Completed && d.Book.Records.Count == 4, "四点巡检与检查记录完成");
            d.api.settings.baseUrl = baseUrl; d.ToggleMode(); d.SyncDevices(); yield return WaitForRequest(d);
            Check(!d.Busy && d.NetworkMessage.StartsWith("同步成功"), "真实 HTTP 设备同步");
            d.Select(fan); d.Command("start"); yield return WaitForRequest(d);
            Check(fan.running && d.NetworkMessage.Contains("成功"), "真实 HTTP 控制设备");
            d.Command("invalid"); yield return WaitForRequest(d);
            Check(d.NetworkMessage.Contains("控制失败"), "非法指令 HTTP 400 被展示");
            string id = fan.deviceId; fan.deviceId = "missing";
            d.Command("start"); yield return WaitForRequest(d); fan.deviceId = id;
            Check(d.NetworkMessage.Contains("404"), "未知设备 HTTP 404 被展示");
            d.Submit(); yield return WaitForRequest(d);
            Check(d.Book.Submitted && d.NetworkMessage.Contains("成功"), "真实 HTTP 提交并确认成功");
            d.Record(true); d.api.settings.baseUrl = "http://127.0.0.1:1"; d.Submit(); yield return WaitForRequest(d);
            Check(!d.Book.Submitted && d.Book.Records.Count == 4 && d.NetworkMessage.Contains("失败"), "连接失败时保留记录且不显示成功");
            d.api.settings.baseUrl = baseUrl; d.Submit(); yield return WaitForRequest(d);
            Check(d.Book.Submitted, "服务恢复后可重试提交");
            d.api.settings.baseUrl = fixtureUrl + "/invalid"; d.SyncDevices(); yield return WaitForRequest(d);
            Check(d.NetworkMessage.Contains("解析失败"), "无效 JSON 响应有明确提示");
            d.api.settings.baseUrl = fixtureUrl + "/slow"; d.api.settings.timeoutSeconds = 1;
            d.SyncDevices(); yield return WaitForRequest(d);
            Check(!d.Busy && d.NetworkMessage.Contains("连接失败"), "请求超时解除忙碌状态");
            d.api.settings.baseUrl = baseUrl; d.ToggleMode(); d.ResetAll();
            Check(!d.HttpMode && d.Book.Records.Count == 0 && d.patrol.Progress.State == PatrolState.Manual, "本地模式切换及重置清空记录");
            yield return null;
            Check(Mathf.Abs(d.patrol.cameraRig.position.z + 8) < .01f, "重置后一帧摄像机仍在初始位置");
            d.Select(fan); d.Command("start"); d.Command("open"); d.Command("alarm");
            yield return new WaitForSeconds(.6f);
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "machine-room.png"));
            yield return new WaitForSeconds(.5f);
            SceneManager.LoadScene("03_FanTraining"); yield return new WaitForSeconds(.8f);
            var fanLesson = FindObjectOfType<FanTrainingController>();
            Check(fanLesson && FindObjectsOfType<UnityEngine.UI.Button>().Length == 2, "风扇主讲场景与两个 uGUI 按钮已加载");
            Check(!fanLesson.IsRunning && fanLesson.cameraBrain.ActiveVirtualCamera.Name == "Fan_Overview", "风扇默认停止且使用全景镜头");
            fanLesson.fanButton.onClick.Invoke(); yield return new WaitForSeconds(.5f);
            var rotor = fanLesson.fanAnimator.transform.Find("Rotor"); float angle = rotor.localEulerAngles.z;
            yield return new WaitForSeconds(.18f);
            Check(fanLesson.IsRunning && Mathf.Abs(Mathf.DeltaAngle(angle, rotor.localEulerAngles.z)) > 20, "风扇按钮实际驱动 Animator 扇叶旋转");
            fanLesson.fanButton.onClick.Invoke(); yield return new WaitForSeconds(.4f); angle = rotor.localEulerAngles.z;
            yield return new WaitForSeconds(.15f);
            Check(!fanLesson.IsRunning && Mathf.Abs(Mathf.DeltaAngle(angle, rotor.localEulerAngles.z)) < 1, "关闭风扇后扇叶停止");
            fanLesson.fanButton.onClick.Invoke();
            fanLesson.inspectionButton.onClick.Invoke(); yield return new WaitForSeconds(.4f);
            Check(fanLesson.IsInspecting && fanLesson.inspectionDirector.state == UnityEngine.Playables.PlayState.Playing, "巡检按钮开始 Timeline");
            fanLesson.inspectionButton.onClick.Invoke(); double pausedTime = fanLesson.inspectionDirector.time;
            yield return new WaitForSeconds(.3f);
            Check(fanLesson.IsInspecting && fanLesson.inspectionDirector.state == UnityEngine.Playables.PlayState.Paused && Math.Abs(fanLesson.inspectionDirector.time - pausedTime) < .01, "同一巡检按钮可暂停且进度保持");
            fanLesson.inspectionButton.onClick.Invoke(); yield return new WaitForSeconds(.3f);
            Check(fanLesson.inspectionDirector.time > pausedTime && fanLesson.IsRunning, "巡检继续且风扇开关状态独立");
            while (fanLesson.inspectionDirector.time < 5.9) yield return null;
            Check(fanLesson.cameraBrain.ActiveVirtualCamera.Name == "Fan_BladeCloseup" && fanLesson.CheckpointCount == 1 && fanLesson.inspectionHighlight.activeSelf, "Timeline 切至扇叶特写并触发检查信号和激活灯");
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "fan-training.png"));
            while (fanLesson.inspectionDirector.time < 10.1) yield return null;
            Check(fanLesson.cameraBrain.ActiveVirtualCamera.Name == "Fan_InspectionFollow" && fanLesson.CheckpointCount == 2, "Cinemachine 跟随镜头与第二检查节点生效");
            float fanEnd = Time.realtimeSinceStartup + 5;
            while (fanLesson.IsInspecting && Time.realtimeSinceStartup < fanEnd) yield return null;
            Check(!fanLesson.IsInspecting && !fanLesson.inspectionHighlight.activeSelf && fanLesson.IsRunning, "巡检自然结束复位且不改变风扇运行状态");
            fanLesson.inspectionButton.onClick.Invoke(); yield return new WaitForSeconds(.2f);
            Check(fanLesson.IsInspecting && fanLesson.CheckpointCount == 0, "巡检可再次启动且节点计数清零");
            fanLesson.StopInspection(); yield return null;
            Check(!fanLesson.IsInspecting && !fanLesson.inspectionHighlight.activeSelf, "提前停止巡检正确收尾");
        }
    }
}
