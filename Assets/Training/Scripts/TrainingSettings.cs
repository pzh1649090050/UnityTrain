using UnityEngine;
namespace UnityTrain
{
    [CreateAssetMenu(menuName = "Unity 培训/网络配置")]
    public class TrainingSettings : ScriptableObject
    {
        public string baseUrl = "http://127.0.0.1:8080";
        [Range(1, 30)] public int timeoutSeconds = 5;
    }
}
