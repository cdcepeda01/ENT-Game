using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Refuerza el ambiente invernal del tutorial con ráfagas de nieve y bruma helada a ras del suelo.</summary>
public class AmbienteInviernoTutorial : MonoBehaviour
{
    public Material materialNieve;
    public Vector3 centroCampo = new Vector3(-3f, 2f, -105f);
    [Tooltip("Desactiva todos los colisionadores en hijos decorativos para no interferir con enemigos ni disparos.")]
    public bool desactivarColisionesHijos = false;

    Material materialRuntime;

    void Awake()
    {
        if (!desactivarColisionesHijos) return;
        foreach (var col in GetComponentsInChildren<Collider>(true))
            col.enabled = false;
    }

    void Start()
    {
        Material mat = materialNieve;
        if (mat == null)
        {
            var shader = Shader.Find("ENT/Soft Snow")
                      ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                materialRuntime = new Material(shader);
                mat = materialRuntime;
            }
        }

        CrearRafagasNieve(mat);
        CrearBrumaHelada(mat);
    }

    void CrearRafagasNieve(Material mat)
    {
        var go = new GameObject("RafagasNieve", typeof(ParticleSystem));
        go.transform.SetParent(transform, false);
        go.transform.position = centroCampo + new Vector3(0f, 11f, 0f);

        var ps = go.GetComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.duration = 10f;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.22f);
        main.startColor = new Color(0.92f, 0.97f, 1f, 0.72f);
        main.maxParticles = 900;

        var emission = ps.emission;
        emission.rateOverTime = 95f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(44f, 2f, 44f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0.8f, 1.9f);
        vel.y = new ParticleSystem.MinMaxCurve(-2.3f, -1.4f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.55f;
        noise.frequency = 0.3f;

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.86f, 0.94f, 1f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.15f), new GradientAlphaKey(0.75f, 0.8f), new GradientAlphaKey(0f, 1f) });
        fade.color = grad;

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.maxParticleSize = 0.014f;
        if (mat != null) r.sharedMaterial = mat;

        ps.Play();
    }

    void CrearBrumaHelada(Material mat)
    {
        var go = new GameObject("BrumaHelada", typeof(ParticleSystem));
        go.transform.SetParent(transform, false);
        go.transform.position = centroCampo + new Vector3(0f, -0.6f, -4f);

        var ps = go.GetComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.duration = 14f;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 14f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(4.5f, 8.5f);
        main.startColor = new Color(0.78f, 0.89f, 0.98f, 0.11f);
        main.maxParticles = 45;

        var emission = ps.emission;
        emission.rateOverTime = 3.5f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(36f, 1.2f, 34f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
        vel.y = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(0.84f, 0.93f, 1f), 0f), new GradientColorKey(new Color(0.76f, 0.87f, 0.98f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0.9f, 0.7f), new GradientAlphaKey(0f, 1f) });
        fade.color = grad;

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (mat != null) r.sharedMaterial = mat;

        ps.Play();
    }

    void OnDestroy()
    {
        if (materialRuntime != null) Destroy(materialRuntime);
    }
}
