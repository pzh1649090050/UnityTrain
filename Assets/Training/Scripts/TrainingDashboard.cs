using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityTrain
{
    public class TrainingDashboard : MonoBehaviour
    {
        public DeviceController[] devices;
        public PatrolController patrol;
        public TrainingApi api;
        public UnityEngine.UI.Text detailText, routeText, recordsText, networkText, modeText;
        public UnityEngine.UI.InputField noteField, addressField;
        public UnityEngine.UI.Button[] networkButtons;
        public DeviceController Selected { get; private set; }
        public bool HttpMode { get; private set; }
        public bool Busy { get; private set; }
        public string NetworkMessage { get; private set; } = "本地演示：无需启动服务";
        public InspectionBook Book { get; } = new InspectionBook();
        void OnEnable() { patrol.Reached += Select; patrol.ResetPerformed += ClearRecords; }
        void OnDisable() { patrol.Reached -= Select; patrol.ResetPerformed -= ClearRecords; }
        void Start()
        {
            if (addressField) addressField.text = api.settings.baseUrl;
            if (devices.Length > 0) Select(devices[0]);
        }
        public void Select(DeviceController device)
        {
            if (Selected != device && noteField) noteField.text = "";
            Selected = device;
        }
        public void SelectIndex(int index) { if (index >= 0 && index < devices.Length) Select(devices[index]); }
        public void ToggleMode()
        {
            if (Busy) return;
            HttpMode = !HttpMode;
            NetworkMessage = HttpMode ? "HTTP 模式：请启动本地服务，然后同步设备" : "本地演示：控制仅修改场景状态";
        }
        public void SetAddress(string value)
        {
            if (Busy) return;
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                api.settings.baseUrl = value.TrimEnd('/');
            else NetworkMessage = "请输入合法的 http 或 https 地址";
        }
        public void Command(string command)
        {
            if (!Selected || Busy) return;
            if (!HttpMode) { Selected.ApplyCommand(command); return; }
            StartCoroutine(SendCommand(Selected, command));
        }
        IEnumerator SendCommand(DeviceController target, string command)
        {
            Busy = true;
            yield return api.Send("POST", "/api/devices/" + Uri.EscapeDataString(target.deviceId) + "/commands",
                JsonUtility.ToJson(new DeviceCommand { command = command }), (json, error) =>
                {
                    if (error != null) { NetworkMessage = "控制失败：" + error; return; }
                    try
                    {
                        var reply = JsonUtility.FromJson<DeviceReply>(json);
                        if (reply == null || reply.device == null || reply.device.id != target.deviceId)
                            throw new ArgumentException("设备响应 ID 不匹配");
                        target.ApplyState(reply.device); NetworkMessage = "设备指令执行成功";
                    }
                    catch (Exception exception) { NetworkMessage = "响应解析失败：" + exception.Message; }
                });
            Busy = false;
        }
        public void SyncDevices()
        {
            if (Busy) return;
            if (!HttpMode) { NetworkMessage = "请先切换 HTTP 模式"; return; }
            StartCoroutine(QueryDevices());
        }
        IEnumerator QueryDevices()
        {
            Busy = true;
            yield return api.Send("GET", "/api/devices", null, (json, error) =>
            {
                if (error != null) { NetworkMessage = "连接失败，可手动切换本地演示：" + error; return; }
                try
                {
                    var data = ApiJson.ReadDevices(json);
                    int matched = 0;
                    foreach (var item in data)
                        foreach (var device in devices) if (device.deviceId == item.id) { device.ApplyState(item); matched++; }
                    NetworkMessage = "同步成功：" + matched + "/" + devices.Length + " 台场景设备";
                }
                catch (Exception exception) { NetworkMessage = "响应解析失败：" + exception.Message; }
            });
            Busy = false;
        }
        public void Record(bool normal)
        {
            if (!Selected || Busy) return;
            Book.Record(Selected.deviceId, normal, noteField ? noteField.text : "");
            if (noteField) noteField.text = "";
            SaveDraft();
        }
        public void NextPoint()
        {
            if (patrol.Progress.State == PatrolState.AtPoint)
            {
                var device = patrol.points[patrol.Progress.Index].device;
                if (!Book.Records.Exists(record => record.deviceId == device.deviceId))
                { NetworkMessage = "请先记录当前巡检设备的检查结果"; return; }
            }
            patrol.Next();
        }
        public void Submit()
        {
            if (Busy || Book.Records.Count == 0) { NetworkMessage = "没有待提交记录，或请求正在进行"; return; }
            if (!HttpMode)
            { SaveDraft(); NetworkMessage = "已保存本地草稿；切换 HTTP 模式才能提交服务端"; return; }
            StartCoroutine(SubmitRecords());
        }
        IEnumerator SubmitRecords()
        {
            Busy = true;
            var batch = new InspectionBatch { batchId = Book.BatchId, records = Book.Records.ToArray() };
            yield return api.Send("POST", "/api/inspections", JsonUtility.ToJson(batch), (json, error) =>
            {
                bool success = false;
                if (error == null)
                {
                    try
                    {
                        var reply = JsonUtility.FromJson<SubmitReply>(json);
                        success = reply != null && reply.ok && reply.batchId == batch.batchId && reply.count == batch.records.Length;
                        if (!success) error = "服务端未确认此批记录";
                    }
                    catch (Exception exception) { error = exception.Message; }
                }
                Book.MarkSubmitted(success);
                NetworkMessage = success ? "巡检提交成功（服务端已保存）" : "提交失败，记录已保留：" + error;
                SaveDraft();
            });
            Busy = false;
        }
        void SaveDraft()
        {
            try
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "inspection-draft.json"),
                    JsonUtility.ToJson(new InspectionBatch { batchId = Book.BatchId, records = Book.Records.ToArray() }, true));
            }
            catch (Exception exception) { NetworkMessage = "本地草稿保存失败：" + exception.Message; }
        }
        public void ClearRecords() { if (!Busy) { Book.Clear(); SaveDraft(); } }
        public void ResetAll() { if (!Busy) patrol.ResetRoute(); }
        public void Back() { if (!Busy) SceneManager.LoadScene("00_Welcome"); }
        void Update()
        {
            if (detailText) detailText.text = Selected ? Selected.displayName + "\n" + Selected.deviceId + "\n\n运行：" + (Selected.running ? "运行中" : "停止") +
                "    柜门：" + (Selected.doorOpen ? "开启" : "关闭") + "\n告警：" + (Selected.alarm ? "异常告警" : "正常") : "点击机房设备查看详情";
            string[] states = { "手动", "移动中", "等待检查", "暂停", "完成" };
            if (routeText) routeText.text = "巡检 / " + states[(int)patrol.Progress.State] + "    已检 " + Book.Records.Count + "/" + devices.Length +
                "\n" + patrol.Message;
            if (modeText) modeText.text = HttpMode ? "HTTP 模式 · 切换本地" : "本地模式 · 切换 HTTP";
            if (networkText) networkText.text = (Busy ? "请求中… " : "") + NetworkMessage;
            if (addressField) addressField.interactable = !Busy;
            if (networkButtons != null) foreach (var button in networkButtons) if (button) button.interactable = !Busy;
            if (recordsText)
            {
                var text = new StringBuilder(Book.Submitted ? "已提交服务端\n" : "检查记录（未提交）\n");
                foreach (var record in Book.Records)
                    text.Append(record.deviceId).Append(" · ").Append(record.result == "normal" ? "正常" : "异常")
                        .Append("\n").Append(string.IsNullOrEmpty(record.note) ? "无备注" : record.note).Append("\n\n");
                recordsText.text = text.ToString();
            }
        }
    }
}
