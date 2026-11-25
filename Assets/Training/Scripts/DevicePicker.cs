using UnityEngine;
using UnityEngine.EventSystems;
namespace UnityTrain
{
    public class DevicePicker : MonoBehaviour
    {
        public TrainingDashboard dashboard;
        Camera view;
        void Awake() { view = GetComponent<Camera>(); }
        void Update()
        {
            if (!Input.GetMouseButtonDown(0) || (EventSystem.current && EventSystem.current.IsPointerOverGameObject())) return;
            if (Physics.Raycast(view.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100))
            {
                var device = hit.collider.GetComponentInParent<DeviceController>();
                if (device) dashboard.Select(device);
            }
        }
    }
}
