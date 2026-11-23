using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

/// <summary>Solo un impacto directo de un proyectil completa la diana.</summary>
[RequireComponent(typeof(Rigidbody))]
public class TutorialTarget : MonoBehaviour
{
    public bool moving;
    [Min(0)] public float travel = 2.5f;
    [Min(0)] public float speed = 0.7f;
    public AudioClip hitSound;
    public TMP_Text label;
    public Renderer center;
    [Tooltip("Opcional: material de partículas para la pólvora. Vacío = usa ENT/Soft Snow.")]
    public Material materialPolvora;

    public bool Hit { get; private set; }
    public GameObject UltimoEfectoPolvora { get; private set; }

    Vector3 origin;
    float elapsed, flash;
    Rigidbody body;
    MaterialPropertyBlock tint;

    void Awake()
    {
        origin = transform.position;
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        tint = new MaterialPropertyBlock();
    }

    void FixedUpdate()
    {
        if (!moving || Hit) return;
        elapsed += Time.fixedDeltaTime;
        body.MovePosition(origin + transform.right * (Mathf.Sin(elapsed * speed) * travel));
    }

    void OnCollisionEnter(Collision collision)
    {
        if (Hit || collision.gameObject.GetComponentInParent<Shot>() == null) return;
        Hit = true;
        flash = 0.25f;
        if (label != null) label.text = "¡ACIERTO!";
        if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.8f);

        Vector3 puntoImpacto = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        Vector3 normalImpacto = collision.contactCount > 0 ? collision.GetContact(0).normal : transform.forward;
        if (normalImpacto.sqrMagnitude < 0.001f) normalImpacto = transform.forward;
        UltimoEfectoPolvora = LanzarParticulasPolvora(puntoImpacto, normalImpacto.normalized);
    }

    void Update()
    {
        if (!Hit || center == null) return;
        flash -= Time.deltaTime;
        tint.SetColor("_BaseColor", flash > 0 ? Color.white : new Color(0.2f, 0.85f, 0.35f));
        center.SetPropertyBlock(tint);
    }

    GameObject LanzarParticulasPolvora(Vector3 punto, Vector3 normal)
    {
        var raiz = new GameObject("ExplosionPolvora");
        raiz.transform.position = punto + normal * 0.08f;
        raiz.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);

        Material mat = materialPolvora;
        Material matRuntime = null;
        if (mat == null)
        {
            var shader = Shader.Find("ENT/Soft Snow")
                      ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                matRuntime = new Material(shader);
                mat = matRuntime;
            }
        }

        // 1. Chispas ardientes de pólvora saliendo disparadas por el impacto
        CrearCapaPolvora(
            raiz.transform, "ChispasPolvora", mat,
            cantidad: 58,
            vidaMin: 0.35f, vidaMax: 0.9f,
            velMin: 4.5f, velMax: 11.5f,
            tamMin: 0.07f, tamMax: 0.16f,
            gravedad: 1.35f,
            anguloCono: 62f, radioCono: 0.18f,
            colorInicio: new Color(1f, 0.92f, 0.55f, 1f),
            colorMedio: new Color(1f, 0.45f, 0.08f, 0.95f),
            colorFin: new Color(0.35f, 0.08f, 0.02f, 0f),
            escalaFinal: 0.25f,
            estirado: true,
            ruido: 0.35f);

        // 2. Nube espesa de humo de pólvora negra
        CrearCapaPolvora(
            raiz.transform, "HumoPolvora", mat,
            cantidad: 32,
            vidaMin: 1.15f, vidaMax: 2.25f,
            velMin: 1.1f, velMax: 3.6f,
            tamMin: 0.42f, tamMax: 1.05f,
            gravedad: -0.16f,
            anguloCono: 78f, radioCono: 0.32f,
            colorInicio: new Color(0.16f, 0.15f, 0.14f, 0.88f),
            colorMedio: new Color(0.30f, 0.30f, 0.32f, 0.62f),
            colorFin: new Color(0.55f, 0.57f, 0.62f, 0f),
            escalaFinal: 2.3f,
            estirado: false,
            ruido: 0.65f);

        // 3. Destello breve de ignición en el centro del impacto
        CrearCapaPolvora(
            raiz.transform, "DestelloPolvora", mat,
            cantidad: 14,
            vidaMin: 0.10f, vidaMax: 0.26f,
            velMin: 0.3f, velMax: 1.8f,
            tamMin: 0.35f, tamMax: 0.85f,
            gravedad: 0f,
            anguloCono: 85f, radioCono: 0.12f,
            colorInicio: new Color(1f, 0.96f, 0.72f, 1f),
            colorMedio: new Color(1f, 0.52f, 0.10f, 0.85f),
            colorFin: new Color(0.8f, 0.18f, 0.02f, 0f),
            escalaFinal: 1.5f,
            estirado: false,
            ruido: 0.1f);

        // Breve destello de luz cálida al detonar la pólvora
        var luzGo = new GameObject("DestelloLuzPolvora", typeof(Light));
        luzGo.transform.SetParent(raiz.transform, false);
        var luz = luzGo.GetComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.62f, 0.2f);
        luz.range = 8f;
        luz.intensity = 28f;

        var limpieza = raiz.AddComponent<LimpiezaPolvora>();
        limpieza.Configurar(luz, matRuntime, 3.2f);
        return raiz;
    }

    static void CrearCapaPolvora(
        Transform padre, string nombre, Material mat,
        int cantidad, float vidaMin, float vidaMax,
        float velMin, float velMax, float tamMin, float tamMax,
        float gravedad, float anguloCono, float radioCono,
        Color colorInicio, Color colorMedio, Color colorFin,
        float escalaFinal, bool estirado, float ruido)
    {
        var go = new GameObject(nombre, typeof(ParticleSystem));
        go.transform.SetParent(padre, false);
        var ps = go.GetComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.duration = vidaMax;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vidaMin, vidaMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velMin, velMax);
        main.startSize = new ParticleSystem.MinMaxCurve(tamMin, tamMax);
        main.gravityModifier = gravedad;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = cantidad + 8;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)cantidad) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = anguloCono;
        shape.radius = radioCono;

        var colorLifetime = ps.colorOverLifetime;
        colorLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[]
            {
                new GradientColorKey(colorInicio, 0f),
                new GradientColorKey(colorMedio, 0.35f),
                new GradientColorKey(colorFin, 1f),
            },
            new[]
            {
                new GradientAlphaKey(colorInicio.a, 0f),
                new GradientAlphaKey(colorMedio.a, 0.4f),
                new GradientAlphaKey(0f, 1f),
            });
        colorLifetime.color = grad;

        var sizeLifetime = ps.sizeOverLifetime;
        sizeLifetime.enabled = true;
        sizeLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, escalaFinal));

        var limitVel = ps.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.dampen = estirado ? 0.08f : 0.14f;

        if (ruido > 0f)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = ruido;
            noise.frequency = 0.8f;
        }

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = estirado ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        if (estirado)
        {
            renderer.lengthScale = 2.4f;
            renderer.velocityScale = 0.08f;
        }
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (mat != null) renderer.sharedMaterial = mat;

        ps.Play();
    }

    sealed class LimpiezaPolvora : MonoBehaviour
    {
        Light luz;
        Material matRuntime;
        float intensidadInicial;
        float tiempoLuz;

        public void Configurar(Light destello, Material materialTemporal, float duracionTotal)
        {
            luz = destello;
            matRuntime = materialTemporal;
            intensidadInicial = luz != null ? luz.intensity : 0f;
            Destroy(gameObject, duracionTotal);
        }

        void Update()
        {
            if (luz == null) return;
            tiempoLuz += Time.deltaTime;
            float t = Mathf.Clamp01(tiempoLuz / 0.22f);
            luz.intensity = Mathf.Lerp(intensidadInicial, 0f, t);
            if (t >= 1f) luz.enabled = false;
        }

        void OnDestroy()
        {
            if (matRuntime != null) Destroy(matRuntime);
        }
    }
}
