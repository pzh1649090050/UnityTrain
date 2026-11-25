using System.Linq;
using Cinemachine;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Timeline;
using UnityTrain;

public class FanTrainingTests
{
    [Test] public void PartiallyWiredLessonDoesNotThrowBeforeTimelineChapter()
    {
        var go = new GameObject("PartialLessonForTest");
        try
        {
            var lesson = go.AddComponent<FanTrainingController>();
            Assert.DoesNotThrow(() => lesson.ToggleInspection());
            Assert.DoesNotThrow(() => lesson.StopInspection());
        }
        finally { Object.DestroyImmediate(go); }
    }
    [Test] public void FanEditorToolFindsMissingReference()
    {
        var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene("Assets/Training/Scenes/03_FanTraining.unity");
            var lesson = Object.FindObjectOfType<FanTrainingController>();
            Assert.IsEmpty(UnityTrain.Editor.FanTrainingTools.Check(lesson));
            lesson.inspectionCamera = null;
            lesson.inspectionDirector = null;
            Assert.IsTrue(UnityTrain.Editor.FanTrainingTools.Check(lesson).Any(s => s.Contains("Timeline")));
        }
        finally { EditorSceneManager.OpenScene("Assets/Training/Scenes/01_MachineRoom.unity"); EditorSceneManager.RestoreSceneManagerSetup(previous); }
    }
    [Test] public void FanSceneContainsModelAndExactlyTwoWiredButtons()
    {
        var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene("Assets/Training/Scenes/03_FanTraining.unity");
            var lesson = Object.FindObjectOfType<FanTrainingController>();
            Assert.IsNotNull(lesson);
            Assert.AreEqual(2, Object.FindObjectsOfType<UnityEngine.UI.Button>().Length);
            Assert.AreEqual("ToggleFan", lesson.fanButton.onClick.GetPersistentMethodName(0));
            Assert.AreEqual("ToggleInspection", lesson.inspectionButton.onClick.GetPersistentMethodName(0));
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(lesson.fanAnimator));
            Assert.AreEqual(4, lesson.fanAnimator.transform.Find("Rotor").Cast<Transform>().Count(t => t.name.StartsWith("Blade")));
            Assert.IsEmpty(Object.FindObjectsOfType<FlyCamera>());
        }
        finally { EditorSceneManager.OpenScene("Assets/Training/Scenes/01_MachineRoom.unity"); EditorSceneManager.RestoreSceneManagerSetup(previous); }
    }
    [Test] public void FanAnimatorHasRunningParameterAndLoopingRotorClip()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Training/Animations/Fan.controller");
        Assert.IsNotNull(controller);
        Assert.IsTrue(controller.parameters.Any(p => p.name == "Running" && p.type == AnimatorControllerParameterType.Bool));
        var spin = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Training/Animations/FanRotorSpin.anim");
        Assert.IsTrue(AnimationUtility.GetAnimationClipSettings(spin).loopTime);
        Assert.IsTrue(AnimationUtility.GetCurveBindings(spin).Any(b => b.path == "Rotor" && b.propertyName == "localEulerAnglesRaw.z"));
    }
    [Test] public void FanTimelineHasBlendedCameraShotsAndBoundTeachingTracks()
    {
        var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene("Assets/Training/Scenes/03_FanTraining.unity");
            var lesson = Object.FindObjectOfType<FanTrainingController>();
            if (lesson.UsesCameraSequence)
            {
                Assert.IsTrue(lesson.HasInspectionSetup);
                Assert.AreEqual(4, lesson.inspectionCamera.m_Instructions.Length);
                foreach (var instruction in lesson.inspectionCamera.m_Instructions)
                    Assert.IsNotNull(instruction.m_VirtualCamera);
                return;
            }
            var timeline = (TimelineAsset)lesson.inspectionDirector.playableAsset;
            Assert.AreEqual(12, timeline.fixedDuration, .001);
            var tracks = timeline.GetOutputTracks().ToArray();
            foreach (var track in tracks) Assert.IsNotNull(lesson.inspectionDirector.GetGenericBinding(track));
            var shots = tracks.OfType<CinemachineTrack>().Single().GetClips().OrderBy(c => c.start).ToArray();
            Assert.AreEqual(3, shots.Length);
            Assert.Greater(shots[0].end, shots[1].start); Assert.Greater(shots[1].end, shots[2].start);
            Assert.AreEqual(1, tracks.OfType<AnimationTrack>().Count());
            Assert.AreEqual(2, tracks.OfType<SignalTrack>().Single().GetMarkers().Count());
            Assert.IsNotNull(lesson.cameraBrain.GetComponent<Camera>());
        }
        finally { EditorSceneManager.OpenScene("Assets/Training/Scenes/01_MachineRoom.unity"); EditorSceneManager.RestoreSceneManagerSetup(previous); }
    }
}
