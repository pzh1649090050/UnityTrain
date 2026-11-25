using UnityEngine;
namespace UnityTrain
{
    public class CinematicsAction : MonoBehaviour
    {
        public CinematicsLesson lesson;
        public string action;
        public void Execute()
        {
            switch (action)
            {
                case "overview": lesson.ChooseCamera(0); break;
                case "cabinet": lesson.ChooseCamera(1); break;
                case "follow": lesson.ChooseCamera(2); break;
                case "target": lesson.ToggleTargetMotion(); break;
                case "play": lesson.PlaySequence(); break;
                case "pause": lesson.PauseSequence(); break;
                case "resume": lesson.ResumeSequence(); break;
                case "stop": lesson.StopSequence(); break;
                case "running": lesson.ToggleRunning(); break;
                case "door": lesson.ToggleDoor(); break;
                case "alarm": lesson.ToggleAlarm(); break;
                case "back": lesson.Back(); break;
            }
        }
    }
}
