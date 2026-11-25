using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public static class TrainingPackageBootstrap
{
    static AddAndRemoveRequest request;
    static double deadline;
    static bool quitWhenDone;
    [MenuItem("培训/安装培训所需包")]
    public static void InstallLive() { Begin(false); }
    public static void Install()
    {
        Begin(true);
    }
    static void Begin(bool quit)
    {
        if (request != null && !request.IsCompleted) return;
        quitWhenDone = quit;
        request = Client.AddAndRemove(new[] { "com.unity.modules.physics@1.0.0", "com.unity.modules.imageconversion@1.0.0", "com.unity.modules.screencapture@1.0.0", "com.unity.cinemachine@2.9.7", "com.unity.timeline@1.6.2" });
        deadline = EditorApplication.timeSinceStartup + 180;
        EditorApplication.update += Poll;
    }
    static void Poll()
    {
        if (!request.IsCompleted && EditorApplication.timeSinceStartup < deadline) return;
        EditorApplication.update -= Poll;
        if (request.IsCompleted && request.Status == StatusCode.Success) { Debug.Log("TRAINING_PACKAGES_OK"); if (quitWhenDone) EditorApplication.Exit(0); }
        else { Debug.LogError(request.Error?.message ?? "Package install timed out"); if (quitWhenDone) EditorApplication.Exit(1); }
    }
}
