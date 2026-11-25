using UnityEngine;
namespace UnityTrain
{
    // 持久化按钮事件：可在 Inspector 中看到动作与目标，而非隐藏在运行时闭包中。
    public class DashboardAction : MonoBehaviour
    {
        public TrainingDashboard dashboard;
        public string action;
        public int deviceIndex;
        public void Execute()
        {
            switch (action)
            {
                case "select": dashboard.SelectIndex(deviceIndex); break;
                case "begin": dashboard.patrol.Begin(); break;
                case "pause": dashboard.patrol.Pause(); break;
                case "resume": dashboard.patrol.Resume(); break;
                case "manual": dashboard.patrol.Manual(); break;
                case "next": dashboard.NextPoint(); break;
                case "reset": dashboard.ResetAll(); break;
                case "normal": dashboard.Record(true); break;
                case "abnormal": dashboard.Record(false); break;
                case "submit": dashboard.Submit(); break;
                case "mode": dashboard.ToggleMode(); break;
                case "sync": dashboard.SyncDevices(); break;
                case "back": dashboard.Back(); break;
                default: dashboard.Command(action); break;
            }
        }
    }
}
