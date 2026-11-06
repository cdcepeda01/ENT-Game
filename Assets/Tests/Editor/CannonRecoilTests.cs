using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

[Category("CannonRecoil")]
public class CannonRecoilTests
{
    GameObject subject;

    CannonRecoil Create(float mass = 60f, float friction = 0.04f)
    {
        subject = new GameObject("Recoil physical test");
        CannonRecoil recoil = subject.AddComponent<CannonRecoil>();
        recoil.cannonMass = mass;
        recoil.frictionCoefficient = friction;
        return recoil;
    }

    [TearDown]
    public void Cleanup()
    {
        if (subject != null) Object.DestroyImmediate(subject);
    }

    [Test]
    public void ImpulseConservesHorizontalMomentum()
    {
        var recoil = Create();
        Vector3 impulse = new Vector3(18, 0, 24);
        recoil.ApplyProjectileImpulse(impulse);
        Assert.That((recoil.Momentum + impulse).magnitude, Is.LessThan(0.0001f));
        Assert.That(recoil.Velocity.magnitude, Is.EqualTo(0.5f).Within(0.0001f));
    }

    [Test]
    public void DoublingMassHalvesTheInitialSpeed()
    {
        var recoil = Create(120);
        recoil.ApplyProjectileImpulse(Vector3.forward * 30);
        Assert.That(recoil.Velocity.z, Is.EqualTo(-0.25f).Within(0.0001f));
    }

    [Test]
    public void VerticalImpulseIsBalancedByTheGroundConstraint()
    {
        var recoil = Create();
        Vector3 impulse = new Vector3(0, 20, 30);
        recoil.ApplyProjectileImpulse(impulse);
        Assert.That((recoil.Momentum + impulse - recoil.LastSupportImpulse).magnitude, Is.LessThan(0.0001f));
        recoil.Simulate(0.1f);
        Assert.That(subject.transform.position.y, Is.Zero);
    }

    [Test]
    public void FrictionStopsAtTheAnalyticalDistanceAndDoesNotReturn()
    {
        var recoil = Create();
        recoil.ApplyProjectileImpulse(Vector3.forward * 30);
        float energy = recoil.KineticEnergy;
        float distance = 0.5f * 0.5f / (2 * recoil.FrictionDeceleration);
        for (int i = 0; i < 200; i++) recoil.Simulate(0.02f);
        Assert.That(recoil.Velocity, Is.EqualTo(Vector3.zero));
        Assert.That(subject.transform.position.z, Is.EqualTo(-distance).Within(0.0001f));
        Assert.That(recoil.DissipatedEnergy, Is.EqualTo(energy).Within(0.0001f));
        Vector3 stopped = subject.transform.position;
        recoil.Simulate(5f);
        Assert.That(subject.transform.position, Is.EqualTo(stopped));
    }

    [Test]
    public void DifferentTimeStepsGiveTheSameStoppingDistance()
    {
        var recoil = Create();
        recoil.ApplyProjectileImpulse(Vector3.forward * 30);
        for (int i = 0; i < 10; i++) recoil.Simulate(0.5f);
        float coarse = recoil.TravelledDistance;
        recoil.ResetState();
        recoil.ApplyProjectileImpulse(Vector3.forward * 30);
        for (int i = 0; i < 1000; i++) recoil.Simulate(0.005f);
        Assert.That(recoil.TravelledDistance, Is.EqualTo(coarse).Within(0.0001f));
    }

    [Test]
    public void WithoutFrictionMomentumAndEnergyStayConstant()
    {
        var recoil = Create(friction: 0);
        recoil.ApplyProjectileImpulse(Vector3.forward * 30);
        float energy = recoil.KineticEnergy;
        recoil.Simulate(2f);
        Assert.That(subject.transform.position.z, Is.EqualTo(-1).Within(0.0001f));
        Assert.That(recoil.KineticEnergy, Is.EqualTo(energy).Within(0.0001f));
        Assert.That(recoil.DissipatedEnergy, Is.Zero);
    }

    [Test]
    public void NewShotsAccumulateRatherThanRestartARecoilAnimation()
    {
        var recoil = Create(friction: 0);
        recoil.ApplyProjectileImpulse(Vector3.forward * 30);
        recoil.Simulate(0.2f);
        recoil.ApplyProjectileImpulse(Vector3.forward * 30);
        Assert.That(recoil.Velocity.z, Is.EqualTo(-1).Within(0.0001f));
        recoil.Simulate(0.2f);
        Assert.That(subject.transform.position.z, Is.EqualTo(-0.3f).Within(0.0001f));
    }

    [Test]
    public void ResetRestoresPositionVelocityAndMeasurements()
    {
        var recoil = Create();
        recoil.ApplyProjectileImpulse(Vector3.right * 30);
        recoil.Simulate(0.1f);
        recoil.ResetState();
        Assert.That(subject.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(recoil.Velocity, Is.EqualTo(Vector3.zero));
        Assert.That(recoil.TravelledDistance, Is.Zero);
        Assert.That(recoil.DissipatedEnergy, Is.Zero);
    }

    [UnityTest]
    public IEnumerator ActualFireTransfersOppositeMomentumAndInheritsChassisVelocity()
    {
        yield return new EnterPlayMode();
        SceneManager.LoadScene("Level_01");
        yield return null;
        yield return null;
        var cannon = Object.FindAnyObjectByType<CannonController>();
        var recoil = cannon.GetComponent<CannonRecoil>();
        Assert.That(recoil, Is.Not.Null);
        var serial = ArduinoSerial.Instance;
        var adapter = ArduinoController.Instance;
        if (serial != null) serial.enabled = false;
        if (adapter != null) adapter.enabled = false;
        var probe = new GameObject("RecoilProbe");
        probe.transform.position = Vector3.one * 10000;
        probe.AddComponent<Rigidbody>().useGravity = false;
        GameObject original = cannon.projectilePrefab;
        float delay = cannon.shotDelay, friction = recoil.frictionCoefficient;
        try
        {
            recoil.ResetState();
            recoil.frictionCoefficient = 0;
            cannon.projectilePrefab = probe;
            cannon.shotDelay = 0;
            // El primer disparo aplica J y -J una sola vez.
            cannon.Fire();
            Vector3 impulse = recoil.LastProjectileImpulse;
            Assert.That(impulse.magnitude, Is.EqualTo(cannon.projectileForce).Within(0.0001f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Rigidbody shot = Object.FindObjectsByType<Rigidbody>().Single(b => b.name == "RecoilProbe(Clone)");
            Vector3 momentum = shot.mass * shot.linearVelocity + recoil.Momentum;
            Assert.That(Vector3.ProjectOnPlane(momentum, Vector3.up).magnitude, Is.LessThan(0.005f));
            Assert.That(Vector3.Dot(recoil.Displacement, impulse), Is.LessThan(0));
            // Otro disparo con el cañón ya moviéndose: la munición hereda esa velocidad.
            Vector3 before = recoil.Velocity;
            Object.Destroy(shot.gameObject);
            yield return null;
            cannon.Fire();
            yield return new WaitForFixedUpdate();
            yield return null;
            shot = Object.FindObjectsByType<Rigidbody>().Single(b => b.name == "RecoilProbe(Clone)");
            Assert.That((shot.linearVelocity - before - recoil.LastProjectileImpulse / shot.mass).magnitude,
                Is.LessThan(0.005f));
            Assert.That((recoil.Velocity - before + Vector3.ProjectOnPlane(recoil.LastProjectileImpulse, Vector3.up) / recoil.cannonMass).magnitude,
                Is.LessThan(0.005f));
            // La pausa congela el integrador fijo.
            float scale = Time.timeScale;
            Time.timeScale = 0;
            Vector3 stoppedForPause = cannon.transform.position;
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(cannon.transform.position, Is.EqualTo(stoppedForPause));
            Time.timeScale = scale;
        }
        finally
        {
            Time.timeScale = 1;
            cannon.projectilePrefab = original;
            cannon.shotDelay = delay;
            recoil.frictionCoefficient = friction;
            recoil.ResetState();
            Object.Destroy(probe);
            foreach (var body in Object.FindObjectsByType<Rigidbody>())
                if (body.name == "RecoilProbe(Clone)") Object.Destroy(body.gameObject);
            if (serial != null) serial.enabled = true;
            if (adapter != null) adapter.enabled = true;
        }
    }

    [UnityTearDown]
    public IEnumerator LeavePlayMode()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
    }
}
