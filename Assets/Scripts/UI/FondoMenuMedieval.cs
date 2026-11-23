using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Ambientación medieval viva para el fondo del menú principal (funciona incluso con Time.timeScale = 0).</summary>
public class FondoMenuMedieval : MonoBehaviour
{
    [Header("Cámara")]
    public Transform camara;
    public Vector3 puntoMirada = new Vector3(-2.5f, 3.8f, 6.5f);
    public float amplitudSway = 0.65f;
    public float velocidadSway = 0.18f;

    [Header("Antorchas del castillo")]
    public Light[] antorchas;
    public float intensidadBase = 20f;
    public float variacionAntorcha = 4.5f;

    [Header("Brasas en el aire")]
    public Material materialBrasas;

    Vector3 posicionBaseCamara;
    Material materialRuntime;

    void Start()
    {
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        if (camara != null) posicionBaseCamara = camara.position;
        CrearBrasas();
    }

    void Update()
    {
        float t = Time.unscaledTime;

        if (camara != null)
        {
            float dx = Mathf.Sin(t * velocidadSway) * amplitudSway;
            float dy = Mathf.Sin(t * velocidadSway * 1.4f + 0.8f) * (amplitudSway * 0.22f);
            camara.position = posicionBaseCamara + new Vector3(dx, dy, 0f);
            camara.LookAt(puntoMirada);
        }

        if (antorchas != null)
        {
            for (int i = 0; i < antorchas.Length; i++)
            {
                if (antorchas[i] == null) continue;
                float parpadeo = Mathf.Sin(t * 5.2f + i * 1.7f) * 0.6f
                               + Mathf.Sin(t * 11.7f + i * 3.1f) * 0.4f;
                antorchas[i].intensity = intensidadBase + parpadeo * variacionAntorcha;
            }
        }
    }

    void CrearBrasas()
    {
        var go = new GameObject("Brasas Medievales", typeof(ParticleSystem));
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(-3f, 0.5f, 5f);

        var ps = go.GetComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.duration = 10f;
        main.prewarm = true;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.68f, 0.22f, 0.9f),
            new Color(1f, 0.42f, 0.1f, 0.85f));
        main.maxParticles = 140;

        var emission = ps.emission;
        emission.rateOverTime = 16f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(26f, 1f, 24f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.55f);
        vel.y = new ParticleSystem.MinMaxCurve(0.45f, 1.1f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.4f;
        noise.frequency = 0.25f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.82f, 0.35f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0.08f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.2f), new GradientAlphaKey(0.75f, 0.75f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (materialBrasas != null)
        {
            renderer.sharedMaterial = materialBrasas;
        }
        else
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                materialRuntime = new Material(shader);
                renderer.sharedMaterial = materialRuntime;
            }
        }

        ps.Play();
    }

    void OnDestroy()
    {
        if (materialRuntime != null) Destroy(materialRuntime);
    }
}
