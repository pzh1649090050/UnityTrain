using UnityEngine;
using UnityEngine.EventSystems;
namespace UnityTrain
{
    public class FlyCamera : MonoBehaviour
    {
        public PatrolController patrol;
        public float speed = 4, sensitivity = 2;
        void Update()
        {
            if (patrol && patrol.Progress.State != PatrolState.Manual && patrol.Progress.State != PatrolState.Completed) return;
            if (EventSystem.current && EventSystem.current.currentSelectedGameObject &&
                EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>()) return;
            if (Input.GetMouseButton(1))
            {
                Vector3 angles = transform.eulerAngles;
                float pitch = angles.x > 180 ? angles.x - 360 : angles.x;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * sensitivity, -80, 80);
                transform.rotation = Quaternion.Euler(pitch, angles.y + Input.GetAxis("Mouse X") * sensitivity, 0);
            }
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 delta = forward * Input.GetAxisRaw("Vertical") + transform.right * Input.GetAxisRaw("Horizontal");
            if (Input.GetKey(KeyCode.E)) delta += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) delta -= Vector3.up;
            Vector3 position = transform.position + Vector3.ClampMagnitude(delta, 1) * speed * Time.deltaTime;
            position.x = Mathf.Clamp(position.x, -8, 8);
            position.z = Mathf.Clamp(position.z, -8, 7);
            position.y = Mathf.Clamp(position.y, .7f, 6);
            transform.position = position;
        }
    }
}
