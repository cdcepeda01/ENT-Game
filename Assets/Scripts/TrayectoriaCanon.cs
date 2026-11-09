using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


[RequireComponent(typeof(CannonController))]
public class TrayectoriaCanon : MonoBehaviour
{
    [Tooltip("Punto de donde sale la bala. Vacío = usa el mismo del cañón.")]
    public Transform puntoSalida;

    [Header("Aspecto de la línea")]
    public Color colorInicio = new Color(1f, 0.85f, 0.45f, 1f);
    public Color colorFinal = new Color(1f, 0.85f, 0.45f, 0f);
    public float grosor = 0.12f;
    [Tooltip("Opcional. Vacío = material simple que trae Unity.")]
    public Material material;

    [Header("Cálculo")]
    [Tooltip("Segundos de vuelo que se dibujan como máximo.")]
    public float tiempoMaximo = 4f;
    [Tooltip("Cada cuánto se calcula un punto (más pequeño = más suave).")]
    public float paso = 0.03f;
    [Tooltip("Capas contra las que la línea se detiene.")]
    public LayerMask capasChoque = ~0;

    [Header("Marca de impacto (opcional)")]
    [Tooltip("Objeto que se coloca donde cae la bala, por ejemplo una esfera pequeña sin collider.")]
    public Transform marcaImpacto;

    CannonController cannon;
    LineRenderer linea;
    float masa = 1f, amortiguacion;
    bool usaGravedad = true;
    readonly List<Vector3> puntos = new List<Vector3>();

    void Awake()
    {
        cannon = GetComponent<CannonController>();

        if (puntoSalida == null)
        {
            FieldInfo campo = typeof(CannonController).GetField("barrelSpawnLocation",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (campo != null) puntoSalida = campo.GetValue(cannon) as Transform;
        }

        if (cannon.projectilePrefab != null &&
            cannon.projectilePrefab.TryGetComponent(out Rigidbody rb))
        {
            masa = Mathf.Max(0.0001f, rb.mass);
            amortiguacion = rb.linearDamping;
            usaGravedad = rb.useGravity;
        }

        GameObject go = new GameObject("LineaTrayectoria");
        go.transform.SetParent(transform, false);
        linea = go.AddComponent<LineRenderer>();
        linea.useWorldSpace = true;
        linea.widthMultiplier = grosor;
        linea.numCapVertices = 4;
        linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        linea.receiveShadows = false;
        linea.material = material != null ? material : new Material(Shader.Find("Sprites/Default"));
        var degradado = new Gradient();
        degradado.SetKeys(
            new[] { new GradientColorKey(colorInicio, 0f), new GradientColorKey(colorFinal, 1f) },
            new[] { new GradientAlphaKey(colorInicio.a, 0f), new GradientAlphaKey(colorFinal.a, 1f) });
        linea.colorGradient = degradado;
    }

    void LateUpdate()
    {
        bool visible = puntoSalida != null && cannon.isActiveAndEnabled && !CannonController.GameplayBlocked;
        linea.enabled = visible;
        if (marcaImpacto != null) marcaImpacto.gameObject.SetActive(false);
        if (!visible) return;

        Vector3 pos = puntoSalida.position;
        Vector3 vel = puntoSalida.forward * cannon.projectileForce / masa;
        Vector3 gravedad = usaGravedad ? Physics.gravity : Vector3.zero;
        float dt = Mathf.Max(0.005f, paso);

        puntos.Clear();
        puntos.Add(pos);
        for (float t = 0f; t < tiempoMaximo; t += dt)
        {
            vel += gravedad * dt;
            vel *= 1f / (1f + amortiguacion * dt);
            Vector3 siguiente = pos + vel * dt;

            if (Choca(pos, siguiente, out Vector3 impacto))
            {
                puntos.Add(impacto);
                if (marcaImpacto != null)
                {
                    marcaImpacto.position = impacto;
                    marcaImpacto.gameObject.SetActive(true);
                }
                break;
            }
            puntos.Add(siguiente);
            pos = siguiente;
        }

        linea.positionCount = puntos.Count;
        linea.SetPositions(puntos.ToArray());
    }

    bool Choca(Vector3 desde, Vector3 hasta, out Vector3 impacto)
    {
        impacto = hasta;
        Vector3 dir = hasta - desde;
        float distancia = dir.magnitude;
        if (distancia <= 0f) return false;

        RaycastHit[] golpes = Physics.RaycastAll(desde, dir / distancia, distancia, capasChoque,
            QueryTriggerInteraction.Ignore);
        float mejor = float.MaxValue;
        bool encontrado = false;
        foreach (RaycastHit g in golpes)
        {
            if (g.transform.IsChildOf(transform)) continue;            // el propio cañón
            if (marcaImpacto != null && g.transform.IsChildOf(marcaImpacto)) continue;
            if (g.distance < mejor) { mejor = g.distance; impacto = g.point; encontrado = true; }
        }
        return encontrado;
    }
}
