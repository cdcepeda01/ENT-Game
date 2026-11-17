using System;
using UnityEngine;


[RequireComponent(typeof(Animator))]
[DefaultExecutionOrder(-100)]
public class EnemigoRebelde : MonoBehaviour
{
    /// <summary>Avisa cuando un rebelde es derribado (para el puntaje).</summary>
    public static event Action<EnemigoRebelde> AlSerDerribado;
    /// <summary>Avisa cuando un rebelde llega al castillo.</summary>
    public static event Action<EnemigoRebelde> AlLlegarAlCastillo;

    [Header("Movimiento")]
    [Tooltip("Metros por segundo.")]
    public float velocidad = 1.4f;
    [Tooltip("Hacia dónde camina. Vacío = busca el cañón.")]
    public Transform objetivo;
    [Tooltip("A esta distancia (en metros) del objetivo se considera que llegó al castillo.")]
    public float distanciaLlegada = 8f;

    [Header("Al ser derribado")]
    public float segundosAntesDeDesaparecer = 4f;
    [Tooltip("Opcional: sonido al caer.")]
    public AudioClip sonidoCaida;
    [Tooltip("Puntos que da al ser derribado (se usa en el puntaje).")]
    public int puntos = 10;

    [Header("Física del esqueleto")]
    public float masaTotal = 60f;

    Animator animator;
    Ragdoll ragdoll;
    bool terminado;

    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
        gameObject.tag = "Target";

        ConstruirEsqueleto();

        // Cuerpo principal (lo que detecta la bala mientras camina).
        var capsula = GetComponent<CapsuleCollider>();
        if (capsula == null)
        {
            capsula = gameObject.AddComponent<CapsuleCollider>();
            capsula.direction = 1;
            capsula.height = 1.8f;
            capsula.radius = 0.35f;
            capsula.center = new Vector3(0f, 0.9f, 0f);
        }

        var cuerpo = GetComponent<Rigidbody>();
        if (cuerpo == null) cuerpo = gameObject.AddComponent<Rigidbody>();
        cuerpo.mass = 1f;
        cuerpo.useGravity = false;
        cuerpo.constraints = RigidbodyConstraints.FreezeAll; // el cuerpo raíz no cae; caen los huesos

        var audio = GetComponent<AudioSource>();
        if (audio == null) audio = gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;
        if (sonidoCaida != null) audio.clip = sonidoCaida;

        // Se agrega al final para que encuentre los huesos ya armados.
        ragdoll = GetComponent<Ragdoll>();
        if (ragdoll == null) ragdoll = gameObject.AddComponent<Ragdoll>();
    }

    void Start()
    {
        if (objetivo == null)
        {
            var canon = FindAnyObjectByType<CannonController>();
            if (canon != null) objetivo = canon.transform;
        }
    }

    void Update()
    {
        if (terminado) return;

        if (ragdoll != null && ragdoll.isRagdoll)
        {
            terminado = true;
            AlSerDerribado?.Invoke(this);
            Destroy(gameObject, segundosAntesDeDesaparecer);
            return;
        }

        if (objetivo == null) return;

        Vector3 direccion = objetivo.position - transform.position;
        direccion.y = 0f;
        if (direccion.magnitude <= distanciaLlegada)
        {
            terminado = true;
            AlLlegarAlCastillo?.Invoke(this);
            Destroy(gameObject);
            return;
        }

        direccion.Normalize();
        transform.position += direccion * velocidad * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direccion);
    }

    // ───────────── Física del esqueleto (ragdoll automático) ─────────────

    void ConstruirEsqueleto()
    {
        if (!animator.isHuman)
        {
            Debug.LogWarning("EnemigoRebelde: el modelo no es Humanoid (Rig → Animation Type → Humanoid).", this);
            return;
        }

        Transform B(HumanBodyBones h) => animator.GetBoneTransform(h);

        Transform cadera = B(HumanBodyBones.Hips);
        Transform espalda = B(HumanBodyBones.Spine);
        Transform cuello = B(HumanBodyBones.Neck) != null ? B(HumanBodyBones.Neck) : B(HumanBodyBones.Head);
        Transform cabeza = B(HumanBodyBones.Head);
        if (cadera == null || espalda == null || cabeza == null) return;

        // Cadera
        Rigidbody rbCadera = Cuerpo(cadera, 0.16f);
        var cajaCadera = cadera.gameObject.AddComponent<BoxCollider>();
        cajaCadera.size = Vector3.one * (0.25f / Escala(cadera));

        // Espalda
        Rigidbody rbEspalda = Cuerpo(espalda, 0.20f);
        Vector3 hastaCuello = espalda.InverseTransformPoint(cuello.position);
        var cajaEspalda = espalda.gameObject.AddComponent<BoxCollider>();
        int eje = EjeMayor(hastaCuello);
        Vector3 tam = Vector3.one * (0.28f / Escala(espalda));
        tam[eje] = Mathf.Abs(hastaCuello[eje]);
        cajaEspalda.size = tam;
        cajaEspalda.center = hastaCuello * 0.5f;
        Unir(espalda, rbCadera);

        // Cabeza
        Cuerpo(cabeza, 0.08f);
        var esfera = cabeza.gameObject.AddComponent<SphereCollider>();
        esfera.radius = 0.12f / Escala(cabeza);
        esfera.center = cabeza.InverseTransformPoint(cabeza.position + transform.up * 0.1f);
        Unir(cabeza, rbEspalda);

        // Piernas
        Extremidad(B(HumanBodyBones.LeftUpperLeg), B(HumanBodyBones.LeftLowerLeg), B(HumanBodyBones.LeftFoot), rbCadera, 0.10f, 0.06f, 0.07f, 0.05f);
        Extremidad(B(HumanBodyBones.RightUpperLeg), B(HumanBodyBones.RightLowerLeg), B(HumanBodyBones.RightFoot), rbCadera, 0.10f, 0.06f, 0.07f, 0.05f);

        // Brazos
        Extremidad(B(HumanBodyBones.LeftUpperArm), B(HumanBodyBones.LeftLowerArm), B(HumanBodyBones.LeftHand), rbEspalda, 0.05f, 0.03f, 0.05f, 0.04f);
        Extremidad(B(HumanBodyBones.RightUpperArm), B(HumanBodyBones.RightLowerArm), B(HumanBodyBones.RightHand), rbEspalda, 0.05f, 0.03f, 0.05f, 0.04f);
    }

    void Extremidad(Transform superior, Transform inferior, Transform fin, Rigidbody padre,
                    float masaSup, float masaInf, float radioSup, float radioInf)
    {
        if (superior == null || inferior == null || fin == null) return;
        Rigidbody rbSup = Cuerpo(superior, masaSup);
        Capsula(superior, inferior, radioSup);
        Unir(superior, padre);

        Cuerpo(inferior, masaInf);
        Capsula(inferior, fin, radioInf);
        Unir(inferior, rbSup);
    }

    Rigidbody Cuerpo(Transform hueso, float fraccionMasa)
    {
        var rb = hueso.gameObject.AddComponent<Rigidbody>();
        rb.mass = masaTotal * fraccionMasa;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        return rb;
    }

    void Capsula(Transform hueso, Transform fin, float radioMundo)
    {
        Vector3 local = hueso.InverseTransformPoint(fin.position);
        var c = hueso.gameObject.AddComponent<CapsuleCollider>();
        c.direction = EjeMayor(local);
        c.center = local * 0.5f;
        c.height = local.magnitude;
        c.radius = radioMundo / Escala(hueso);
    }

    static void Unir(Transform hueso, Rigidbody padre)
    {
        var j = hueso.gameObject.AddComponent<CharacterJoint>();
        j.connectedBody = padre;
        j.enablePreprocessing = false;
        var bajo = j.lowTwistLimit; bajo.limit = -20f; j.lowTwistLimit = bajo;
        var alto = j.highTwistLimit; alto.limit = 70f; j.highTwistLimit = alto;
        var s1 = j.swing1Limit; s1.limit = 30f; j.swing1Limit = s1;
        var s2 = j.swing2Limit; s2.limit = 30f; j.swing2Limit = s2;
    }

    static int EjeMayor(Vector3 v)
    {
        Vector3 a = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        return a.x >= a.y && a.x >= a.z ? 0 : (a.y >= a.z ? 1 : 2);
    }

    static float Escala(Transform t)
    {
        Vector3 s = t.lossyScale;
        float e = (Mathf.Abs(s.x) + Mathf.Abs(s.y) + Mathf.Abs(s.z)) / 3f;
        return e > 0.0001f ? e : 1f;
    }
}
