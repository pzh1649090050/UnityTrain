using System;
using System.IO;
using System.Xml;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// 同步入口适用于本项目不跨帧的 EditMode 测试，执行完由原生 -quit 退出。
public static class TrainingBatchTests
{
    class Results : ICallbacks
    {
        public ITestResultAdaptor result;
        public void RunStarted(ITestAdaptor test) { }
        public void RunFinished(ITestResultAdaptor value) { result = value; }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor value) { }
    }
    public static void Run()
    {
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        var listener = new Results(); api.RegisterCallbacks(listener);
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, assemblyNames = new[] { "Training.Tests" } }) { runSynchronously = true });
        var result = listener.result;
        if (result == null) throw new Exception("测试没有生成结果");
        Directory.CreateDirectory("TestResults");
        var document = new XmlDocument(); var root = document.CreateElement("test-run"); document.AppendChild(root);
        root.SetAttribute("result", result.TestStatus.ToString()); root.SetAttribute("passed", result.PassCount.ToString());
        root.SetAttribute("failed", result.FailCount.ToString()); root.SetAttribute("total", (result.PassCount + result.FailCount + result.SkipCount).ToString());
        var suite = new XmlDocument(); suite.LoadXml(result.ToXml().OuterXml); root.AppendChild(document.ImportNode(suite.DocumentElement, true));
        document.Save("TestResults/editmode.xml"); api.UnregisterCallbacks(listener); UnityEngine.Object.DestroyImmediate(api);
        if (result.FailCount > 0 || result.PassCount == 0) throw new Exception("Unity tests failed: " + result.FailCount + "; " + result.Message);
        Debug.Log("TRAINING_TESTS_OK " + result.PassCount);
    }
}
