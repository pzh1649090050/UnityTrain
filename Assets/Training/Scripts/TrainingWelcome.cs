using UnityEngine;
using UnityEngine.SceneManagement;
namespace UnityTrain
{
    public class TrainingWelcome : MonoBehaviour
    {
        public void Enter() { SceneManager.LoadScene("01_MachineRoom"); }
        public void EnterCinematics() { SceneManager.LoadScene("02_CinematicsLab"); }
        public void EnterFanTraining() { SceneManager.LoadScene("03_FanTraining"); }
        public void Quit() { Application.Quit(); }
    }
}
