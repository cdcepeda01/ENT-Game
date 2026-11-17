using System.Collections.Generic;
using UnityEngine;


public class GeneradorEnemigos : MonoBehaviour
{
    [Header("Personajes")]
    [Tooltip("Prefabs de rebeldes. Si hay varios, se elige uno al azar cada vez.")]
    public GameObject[] personajes;

    [Header("Zona de aparición (caja verde)")]
    [Tooltip("Ancho (X) y fondo (Z) de la zona en metros. La altura la da la posición del objeto.")]
    public Vector2 tamanoZona = new Vector2(30f, 20f);

    [Header("Ritmo")]
    [Tooltip("Segundos entre rebeldes al empezar.")]
    public float intervaloInicial = 3f;
    [Tooltip("Lo más rápido que pueden aparecer.")]
    public float intervaloMinimo = 1f;
    [Tooltip("Cuántos segundos se acorta el intervalo con cada rebelde nuevo.")]
    public float aceleracion = 0.1f;
    [Tooltip("Máximo de rebeldes vivos al mismo tiempo.")]
    public int maximoALaVez = 8;

    [Header("Inicio")]
    [Tooltip("Si está marcado, empieza a generar al dar Play. Desmárcalo cuando el temporizador lo controle.")]
    public bool empezarSolo = true;

    public bool Activo { get; private set; }

    readonly List<GameObject> vivos = new List<GameObject>();
    float intervaloActual;
    float contador;

    void Start()
    {
        if (empezarSolo) Iniciar();
    }

    /// <summary>Empieza (o reinicia) la aparición de rebeldes.</summary>
    public void Iniciar()
    {
        intervaloActual = intervaloInicial;
        contador = 0.5f; // el primero sale casi de inmediato
        Activo = true;
    }

    /// <summary>Deja de crear rebeldes (los que ya están siguen).</summary>
    public void Detener()
    {
        Activo = false;
    }

    /// <summary>Borra todos los rebeldes que siguen en el campo.</summary>
    public void LimpiarEnemigos()
    {
        foreach (var e in vivos)
            if (e != null) Destroy(e);
        vivos.Clear();
    }

    void Update()
    {
        if (!Activo || personajes == null || personajes.Length == 0) return;

        vivos.RemoveAll(e => e == null);

        contador -= Time.deltaTime;
        if (contador > 0f) return;
        contador = intervaloActual;

        if (vivos.Count >= maximoALaVez) return;

        Crear();
        intervaloActual = Mathf.Max(intervaloMinimo, intervaloActual - aceleracion);
    }

    void Crear()
    {
        GameObject prefab = personajes[Random.Range(0, personajes.Length)];
        if (prefab == null) return;

        Vector3 local = new Vector3(
            Random.Range(-tamanoZona.x * 0.5f, tamanoZona.x * 0.5f),
            0f,
            Random.Range(-tamanoZona.y * 0.5f, tamanoZona.y * 0.5f));
        Vector3 punto = transform.TransformPoint(local);
        punto.y = transform.position.y;

        vivos.Add(Instantiate(prefab, punto, transform.rotation));
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(tamanoZona.x, 0.1f, tamanoZona.y));
    }
}
