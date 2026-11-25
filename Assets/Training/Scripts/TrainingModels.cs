using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityTrain
{
    [Serializable] public class DeviceData
    {
        public string id;
        public string name;
        public bool running;
        public bool doorOpen;
        public bool alarm;
    }
    [Serializable] public class DeviceList { public DeviceData[] devices; }
    [Serializable] public class DeviceReply { public DeviceData device; }
    [Serializable] public class DeviceCommand { public string command; }
    [Serializable] public class InspectionRecord
    {
        public string deviceId, result, note, checkedAt;
    }
    [Serializable] public class InspectionBatch
    {
        public string batchId;
        public InspectionRecord[] records;
    }
    [Serializable] public class SubmitReply { public bool ok; public string batchId; public int count; }

    public static class DeviceValidation
    {
        public static List<string> CheckIds(IEnumerable<string> ids)
        {
            var errors = new List<string>();
            var found = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids)
                if (string.IsNullOrWhiteSpace(id)) errors.Add("设备 ID 不能为空");
                else if (!found.Add(id)) errors.Add("重复设备 ID：" + id);
            return errors;
        }
    }

    public static class ApiJson
    {
        public static DeviceData[] ReadDevices(string json)
        {
            try
            {
                var envelope = JsonUtility.FromJson<DeviceList>(json);
                if (envelope == null || envelope.devices == null) throw new ArgumentException("缺少 devices 数组");
                var ids = new List<string>();
                foreach (var device in envelope.devices)
                {
                    if (device == null) throw new ArgumentException("设备为空");
                    ids.Add(device.id);
                }
                if (DeviceValidation.CheckIds(ids).Count > 0) throw new ArgumentException("设备 ID 缺失或重复");
                return envelope.devices;
            }
            catch (Exception error) { throw new ArgumentException("设备 JSON 不合法：" + error.Message, error); }
        }
    }

    // 纯 C# 状态机不依赖场景，便于单元测试与课堂逐步调试。
    public enum PatrolState { Manual, Moving, AtPoint, Paused, Completed }
    public class InspectionProgress
    {
        public PatrolState State { get; private set; }
        public int Index { get; private set; }
        int count;
        PatrolState beforePause;
        public bool Start(int pointCount)
        {
            Reset(); count = pointCount;
            if (count <= 0) return false;
            State = PatrolState.Moving; return true;
        }
        public void Pause()
        {
            if (State != PatrolState.Moving && State != PatrolState.AtPoint) return;
            beforePause = State; State = PatrolState.Paused;
        }
        public void Resume() { if (State == PatrolState.Paused) State = beforePause; }
        public void Arrive() { if (State == PatrolState.Moving) State = PatrolState.AtPoint; }
        public void Next()
        {
            if (State != PatrolState.AtPoint) return;
            Index++;
            State = Index >= count ? PatrolState.Completed : PatrolState.Moving;
        }
        public void UseManual() { State = PatrolState.Manual; }
        public void Reset() { Index = 0; count = 0; State = PatrolState.Manual; }
    }

    public class InspectionBook
    {
        public List<InspectionRecord> Records { get; } = new List<InspectionRecord>();
        public string BatchId { get; private set; } = Guid.NewGuid().ToString("N");
        public bool Submitted { get; private set; }
        public void Record(string deviceId, bool normal, string note)
        {
            Records.RemoveAll(record => record.deviceId == deviceId);
            Records.Add(new InspectionRecord { deviceId = deviceId, result = normal ? "normal" : "abnormal",
                note = note, checkedAt = DateTime.UtcNow.ToString("o") });
            BatchId = Guid.NewGuid().ToString("N"); Submitted = false;
        }
        public void MarkSubmitted(bool success) { Submitted = success; }
        public void Clear() { Records.Clear(); BatchId = Guid.NewGuid().ToString("N"); Submitted = false; }
    }
}
