using System.IO;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityTrain.Editor
{
    public static class FanInspectionWiring
    {
        [MenuItem("培训/绑定场景中的巡检镜头")]
        public static void Bind()
        {
            var lesson = Object.FindObjectOfType<FanTrainingController>();
            var camera = Object.FindObjectsOfType<CinemachineBlendListCamera>(true).FirstOrDefault(c => c.name == "巡检");
            if (!lesson || !camera || lesson.gameObject.scene != camera.gameObject.scene || Application.isPlaying) return;
            Undo.RecordObject(lesson, "绑定巡检镜头序列");
            Undo.RecordObject(camera, "巡检等待按钮触发");
            lesson.inspectionCamera = camera; camera.enabled = false;
            EditorUtility.SetDirty(lesson); EditorUtility.SetDirty(camera);
            EditorSceneManager.MarkSceneDirty(lesson.gameObject.scene);
            EditorSceneManager.SaveScene(lesson.gameObject.scene);
            Directory.CreateDirectory("TestResults");
            File.WriteAllText("TestResults/fan-inspection-binding.txt", "Scene=" + lesson.gameObject.scene.path +
                "\nCamera=" + camera.name + "\nInstructions=" + camera.m_Instructions.Length +
                "\nOrder=" + string.Join(" -> ", camera.m_Instructions.Select(i => i.m_VirtualCamera ? i.m_VirtualCamera.name : "Missing")) +
                "\nReady=" + lesson.HasInspectionSetup +
                "\nButton=" + lesson.inspectionButton.onClick.GetPersistentMethodName(0));
            Debug.Log("巡检按钮已绑定“巡检”Blend List Camera");
        }
    }
}
