using System;
using UnityEngine;
namespace UnityTrain
{
    public class PatrolController : MonoBehaviour
    {
        public Transform cameraRig;
        public InspectionPoint[] points;
        public float speed = 3;
        public InspectionProgress Progress { get; } = new InspectionProgress();
        public event Action<DeviceController> Reached;
        public event Action ResetPerformed;
        public string Message { get; private set; } = "手动漫游";
        Vector3 initialPosition;
        Quaternion initialRotation;
        void Awake()
        {
            if (cameraRig) { initialPosition = cameraRig.position; initialRotation = cameraRig.rotation; }
        }
        public void Begin()
        {
            if (!cameraRig || points == null || points.Length == 0 || Array.Exists(points, p => !p || !p.device))
            { Message = "路线配置不完整，请使用配置检查工具"; return; }
            Progress.Start(points.Length); Message = "自动巡检中";
        }
        public void Pause() { Progress.Pause(); Message = "已暂停"; }
        public void Resume() { Progress.Resume(); Message = "自动巡检中"; }
        public void Manual() { Progress.UseManual(); Message = "手动漫游"; }
        public void Next() { Progress.Next(); }
        public void ResetRoute()
        {
            Progress.Reset();
            if (cameraRig) { cameraRig.SetPositionAndRotation(initialPosition, initialRotation); }
            Message = "已重置路线与检查记录"; ResetPerformed?.Invoke();
        }
        void Update()
        {
            if (Progress.State != PatrolState.Moving) return;
            if (!cameraRig || points == null || Progress.Index >= points.Length || !points[Progress.Index] || !points[Progress.Index].device)
            { Manual(); Message = "巡检点失效，已停止自动巡检"; return; }
            var point = points[Progress.Index];
            cameraRig.position = Vector3.MoveTowards(cameraRig.position, point.transform.position, speed * Time.deltaTime);
            Vector3 look = point.device.transform.position + Vector3.up * 1.2f - cameraRig.position;
            if (look.sqrMagnitude > .01f)
                cameraRig.rotation = Quaternion.Slerp(cameraRig.rotation, Quaternion.LookRotation(look), Time.deltaTime * 4);
            if (Vector3.Distance(cameraRig.position, point.transform.position) <= point.arrivalDistance)
            {
                Progress.Arrive(); Message = "已到达：" + point.device.displayName + "，请记录结果后前往下一点";
                Reached?.Invoke(point.device);
            }
        }
        void OnDrawGizmos()
        {
            if (points == null) return;
            Gizmos.color = Color.cyan;
            for (int i = 1; i < points.Length; i++)
                if (points[i - 1] && points[i]) Gizmos.DrawLine(points[i - 1].transform.position, points[i].transform.position);
        }
    }
}
