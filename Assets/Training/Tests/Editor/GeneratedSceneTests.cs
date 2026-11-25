using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityTrain;
using UnityTrain.Editor;

public class GeneratedSceneTests
{
    const string Scene = "Assets/Training/Scenes/01_MachineRoom.unity";
    [SetUp] public void Open() { EditorSceneManager.OpenScene(Scene); }
    [Test] public void GeneratedSceneHasCompleteConfiguration()
    {
        Assert.IsEmpty(TrainingValidator.Scan(), "生成场景必须通过配置检查");
        var dashboard = Object.FindObjectOfType<TrainingDashboard>();
        Assert.AreEqual(4, dashboard.devices.Length);
        Assert.AreEqual(4, dashboard.patrol.points.Length);
        Assert.IsNotNull(dashboard.api.settings);
        Assert.IsNotNull(dashboard.noteField);
        foreach (var device in dashboard.devices)
        {
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(device));
            Assert.IsNotNull(device.animator.runtimeAnimatorController);
        }
    }
    [Test] public void ValidatorFindsDuplicatesMissingPointBindingAndEmptyRoute()
    {
        var devices = Object.FindObjectsOfType<DeviceController>();
        devices[1].deviceId = devices[0].deviceId;
        Assert.IsTrue(TrainingValidator.Scan().Any(x => x.message.Contains("重复")));
        var route = Object.FindObjectOfType<PatrolController>();
        route.points[0].device = null;
        Assert.IsTrue(TrainingValidator.Scan().Any(x => x.message.Contains("未绑定")));
        route.points = new InspectionPoint[0];
        Assert.IsTrue(TrainingValidator.Scan().Any(x => x.message.Contains("为空")));
    }
    [Test] public void PointHandleEquivalentEditSupportsUndoAndSavedScenePersists()
    {
        var point = Object.FindObjectOfType<PatrolController>().points[0];
        Vector3 old = point.transform.position;
        Undo.RecordObject(point.transform, "测试移动点");
        point.transform.position = old + Vector3.right;
        Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
        Assert.AreEqual(old, point.transform.position);
        // 保存到临时副本，避免测试修改课程场景。
        const string temp = "Assets/Training/Tests/Editor/TemporaryScene.unity";
        try
        {
            Undo.RecordObject(point.transform, "保存点");
            point.transform.position = old + Vector3.up;
            EditorSceneManager.SaveScene(point.gameObject.scene, temp, true);
            EditorSceneManager.OpenScene(temp);
            Assert.AreEqual(old + Vector3.up, Object.FindObjectOfType<PatrolController>().points[0].transform.position);
        }
        finally { EditorSceneManager.OpenScene(Scene); AssetDatabase.DeleteAsset(temp); }
    }
    [TearDown] public void Cleanup() { EditorSceneManager.OpenScene(Scene); }
}
