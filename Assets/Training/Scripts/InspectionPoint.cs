using UnityEngine;
namespace UnityTrain
{
    public class InspectionPoint : MonoBehaviour
    {
        public DeviceController device;
        public float arrivalDistance = .12f;
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(.1f, .9f, .8f);
            Gizmos.DrawWireSphere(transform.position, .25f);
            if (device) Gizmos.DrawLine(transform.position, device.transform.position + Vector3.up);
        }
    }
}
