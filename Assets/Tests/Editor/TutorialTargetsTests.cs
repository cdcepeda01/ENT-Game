using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TutorialTargetsTests
{
    [UnityTest]
    public IEnumerator SceneTargetsMoveHorizontallyAndRegisterDirectHits()
    {
        yield return new EnterPlayMode();
        SceneManager.LoadScene("S2_Tutorial");
        yield return null;
        Time.timeScale = 1;
        var targets = Object.FindObjectsByType<TutorialTarget>();
        Assert.That(targets.Length, Is.EqualTo(3));
        Assert.That(targets.Count(t => t.moving), Is.EqualTo(1));
        var positions = targets.Select(t => t.transform.position).ToArray();
        yield return Wait(.5f);
        for (int i = 0; i < targets.Length; i++)
        {
            var delta = targets[i].transform.position - positions[i];
            if (targets[i].moving) { Assert.That(Mathf.Abs(delta.y), Is.LessThan(.001f)); Assert.That(delta.magnitude, Is.GreaterThan(.1f)); }
            else Assert.That(delta.magnitude, Is.LessThan(.001f));
        }
        foreach (var target in targets)
        {
            // A real physics collision, while the tutorial is still on step one.
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.localScale = Vector3.one * .3f;
            ball.transform.position = target.transform.position + target.transform.forward * 1.5f;
            ball.AddComponent<Shot>();
            var rb = ball.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = -target.transform.forward * 15;
            yield return Wait(.3f);
            Assert.That(target.Hit, Is.True, target.name);
            Assert.That(target.label.text, Is.EqualTo("¡ACIERTO!"));
            var stopped = target.transform.position;
            yield return Wait(.2f);
            Assert.That(Vector3.Distance(stopped, target.transform.position), Is.LessThan(.001f));
            Object.Destroy(ball);
        }
        Assert.That(targets.Count(t => t.Hit), Is.EqualTo(3));
    }
    static IEnumerator Wait(float seconds) { double until = Time.realtimeSinceStartupAsDouble + seconds; while (Time.realtimeSinceStartupAsDouble < until) yield return null; }
    [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    [MenuItem("Tools/Simulation/Check tutorial targets %#F4")]
    public static void Run()
    {
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Results());
        api.Execute(new ExecutionSettings(new Filter { testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,
            testNames = new[] { "TutorialTargetsTests.SceneTargetsMoveHorizontallyAndRegisterDirectHits" } }));
    }
    class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor t) { }
        public void TestStarted(ITestAdaptor t) { }
        public void TestFinished(ITestResultAdaptor t) { }
        public void RunFinished(ITestResultAdaptor r) { TestRunnerApi.SaveResultToFile(r, "Logs/Tutorial-test-results.xml"); Debug.Log($"[Tutorial tests] Passed {r.PassCount}, failed {r.FailCount}"); }
    }
}

