using System.Linq;
using Cinemachine;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class CinematicLessonTests
{
    const string Scene = "Assets/Training/Scenes/02_CinematicsLab.unity";
    [Test] public void CinematicSceneAndTimelineAssetsExist()
    {
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene), "需要实际保存的镜头教学场景");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<TimelineAsset>("Assets/Training/Timelines/MachineRoomSequence.playable"));
    }
    [Test] public void ThreeCameraShotsAndAllTrackBindingsAreSaved()
    {
        var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene(Scene);
            var director = Object.FindObjectOfType<PlayableDirector>();
            Assert.IsFalse(director.playOnAwake);
            var timeline = (TimelineAsset)director.playableAsset;
            var tracks = timeline.GetOutputTracks().ToArray();
            Assert.IsTrue(tracks.Any(t => t is CinemachineTrack));
            Assert.AreEqual(2, tracks.OfType<AnimationTrack>().Count());
            Assert.AreEqual(1, tracks.OfType<ActivationTrack>().Count());
            Assert.AreEqual(1, tracks.OfType<SignalTrack>().Count());
            foreach (var track in tracks) Assert.IsNotNull(director.GetGenericBinding(track), track.name);
            var shots = tracks.OfType<CinemachineTrack>().Single().GetClips().ToArray();
            Assert.AreEqual(3, shots.Length);
            foreach (var clip in shots) Assert.IsNotNull(((CinemachineShot)clip.asset).VirtualCamera.Resolve(director));
            Assert.IsNotNull(Camera.main.GetComponent<CinemachineBrain>());
        }
        finally { EditorSceneManager.OpenScene("Assets/Training/Scenes/01_MachineRoom.unity"); EditorSceneManager.RestoreSceneManagerSetup(previous); }
    }
    [Test] public void TimelineActuallyAnimatesDoorAndFollowTarget()
    {
        var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene(Scene);
            var director = Object.FindObjectOfType<PlayableDirector>();
            var door = GameObject.Find("cabinet-01").transform.Find("DoorPivot");
            var target = GameObject.Find("FollowTarget").transform;
            director.time = 1; director.Evaluate();
            float before = door.localEulerAngles.y; Vector3 start = target.position;
            director.time = 6; director.Evaluate();
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(before, door.localEulerAngles.y)), 60);
            Assert.Greater(Vector3.Distance(start, target.position), 2);
            director.Stop();
        }
        finally { EditorSceneManager.OpenScene("Assets/Training/Scenes/01_MachineRoom.unity"); EditorSceneManager.RestoreSceneManagerSetup(previous); }
    }
}
