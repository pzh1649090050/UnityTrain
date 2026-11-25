using System;
using NUnit.Framework;
using UnityEngine;
using UnityTrain;

public class TrainingCoreTests
{
    [Test] public void RouteCannotStartWithoutPoints()
    {
        var route = new InspectionProgress();
        Assert.IsFalse(route.Start(0));
        Assert.AreEqual(PatrolState.Manual, route.State);
    }
    [Test] public void RoutePauseContinueCompleteAndReset()
    {
        var route = new InspectionProgress();
        Assert.IsTrue(route.Start(2));
        route.Pause(); Assert.AreEqual(PatrolState.Paused, route.State);
        route.Resume(); Assert.AreEqual(PatrolState.Moving, route.State);
        route.Arrive(); Assert.AreEqual(PatrolState.AtPoint, route.State);
        route.Pause(); route.Resume(); Assert.AreEqual(PatrolState.AtPoint, route.State);
        route.Next(); Assert.AreEqual(1, route.Index);
        route.Arrive(); route.Next(); Assert.AreEqual(PatrolState.Completed, route.State);
        route.Reset(); Assert.AreEqual(0, route.Index);
        Assert.AreEqual(PatrolState.Manual, route.State);
    }
    [Test] public void ManualStopsAutomaticMovement()
    {
        var route = new InspectionProgress(); route.Start(3); route.UseManual(); route.Next();
        Assert.AreEqual(PatrolState.Manual, route.State);
        Assert.AreEqual(0, route.Index);
    }
    [TestCase("{broken")]
    [TestCase("{}")]
    [TestCase("{\"devices\":[{}]}")]
    [TestCase("{\"devices\":[{\"id\":\"a\"},{\"id\":\"a\"}]}")]
    public void InvalidDeviceJsonRejected(string json)
    { Assert.Throws<ArgumentException>(() => ApiJson.ReadDevices(json)); }
    [Test] public void ValidDeviceJsonPreservesState()
    {
        var devices = ApiJson.ReadDevices("{\"devices\":[{\"id\":\"fan-01\",\"running\":true,\"alarm\":true}]}");
        Assert.AreEqual("fan-01", devices[0].id); Assert.IsTrue(devices[0].running);
        Assert.IsTrue(devices[0].alarm);
    }
    [Test] public void DeviceIdsMustBeUniqueAndNonempty()
    {
        Assert.IsNotEmpty(DeviceValidation.CheckIds(new[] {"a", "a"}));
        Assert.IsNotEmpty(DeviceValidation.CheckIds(new[] {"a", " "}));
        Assert.IsEmpty(DeviceValidation.CheckIds(new[] {"a", "b"}));
    }
    [Test] public void RecordsReplacePerDeviceAndFailedSubmitRetainsData()
    {
        var book = new InspectionBook();
        book.Record("a", true, "初检"); book.Record("a", false, "复检异常");
        Assert.AreEqual(1, book.Records.Count);
        Assert.AreEqual("abnormal", book.Records[0].result);
        string batch = book.BatchId;
        book.MarkSubmitted(false); Assert.IsFalse(book.Submitted);
        Assert.AreEqual(batch, book.BatchId); Assert.AreEqual(1, book.Records.Count);
        book.MarkSubmitted(true); Assert.IsTrue(book.Submitted);
        book.Record("b", true, ""); Assert.IsFalse(book.Submitted);
        Assert.AreNotEqual(batch, book.BatchId);
    }
}
