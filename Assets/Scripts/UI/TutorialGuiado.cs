using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class TutorialGuiado : MonoBehaviour
{
    /// <summary>Lo activa MenuManager.ComenzarTutorial(); se consume al iniciar el nivel.</summary>
    public static bool Activo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Activo = false;

    [Header("Referencias de la interfaz")]
    [Tooltip("Recuadro que contiene los textos del tutorial.")]
    public GameObject panel;
    [Tooltip("Texto pequeño: 'Paso 1 de 4'.")]
    public TMP_Text textoPaso;
    [Tooltip("Texto grande con la instrucción.")]
    public TMP_Text textoInstruccion;

    [Header("Cañón (vacío = lo busca solo)")]
    public CannonController cannon;

    [Header("Cajas (agregado)")]
    [Tooltip("Grupo con las cajas de práctica. Solo se ve en el tutorial.")]
    public GameObject cajasTutorial;
    [Tooltip("Grupo con las cajas del juego. Se oculta durante el tutorial.")]
    public GameObject cajasJuego;
    [Tooltip("Al terminar el tutorial, arranca el juego normal con las cajas del juego.")]
    public bool iniciarJuegoAlFinal = true;
    [Tooltip("Actívalo si esta escena es solo para el tutorial: así también aparece al Reiniciar.")]
    public bool siempreMostrar = false;

    [Header("Sonidos (agregado)")]
    [Tooltip("Suena cada vez que se completa un paso.")]
    public AudioClip sonidoPaso;
    [Tooltip("Suena al terminar el tutorial. Vacío = usa el sonido de paso.")]
    public AudioClip sonidoFinal;
    [Range(0f, 1f)] public float volumen = 0.8f;
    AudioSource fuenteAudio;

    [Header("Textos (se pueden cambiar aquí)")]
    [TextArea] public string pasoInclinar = "Mueve la palanca IZQUIERDA hacia adelante y hacia atrás para subir y bajar el cañón.";
    [TextArea] public string pasoGirar = "Mueve la palanca DERECHA hacia los lados para girar el cañón.";
    [TextArea] public string pasoDisparar = "Presiona el BOTÓN de la palanca derecha para disparar.";
    [TextArea] public string pasoObjetivo = "Apunta y derriba las 3 cajas.";
    [TextArea] public string textoBien = "¡Bien hecho!";
    [TextArea] public string textoFinal = "¡Listo, artillero!\nAhora comienza el juego.";

    [Header("Ajustes")]
    [Tooltip("Grados que debe moverse el cañón para dar el paso por cumplido.")]
    public float gradosInclinar = 3f;
    public float gradosGirar = 5f;
    [Tooltip("Metros que debe moverse una caja para contar como derribada.")]
    public float distanciaCaja = 0.3f;
    [Tooltip("Parte del nombre de los objetos que cuentan como cajas.")]
    public string nombreCajas = "WoodenCrate";
    public float segundosBien = 1.2f;
    public float segundosFinal = 4f;

    const int TotalPasos = 4;

    void Start()
    {
        bool mostrar = Activo || siempreMostrar;
        Activo = false; // Reiniciar o volver a entrar con "Jugar" no repite el tutorial.
        if (panel != null) panel.SetActive(false);
        // Cajas de práctica solo en el tutorial; cajas del juego solo en el juego.
        if (cajasTutorial != null) cajasTutorial.SetActive(mostrar);
        if (cajasJuego != null) cajasJuego.SetActive(!mostrar);
        if (!mostrar) { enabled = false; return; }
        if (cannon == null) cannon = FindAnyObjectByType<CannonController>();
        StartCoroutine(Ejecutar());
    }

    IEnumerator Ejecutar()
    {
        if (panel != null) panel.SetActive(true);

        // Paso 1: inclinar (palanca izquierda).
        Mostrar(1, pasoInclinar);
        float inicioV = cannon != null ? cannon.VerticalAngle : 0f;
        yield return new WaitUntil(() => cannon == null ||
            Mathf.Abs(Mathf.DeltaAngle(inicioV, cannon.VerticalAngle)) >= gradosInclinar);
        yield return Bien();

        // Paso 2: girar (palanca derecha).
        Mostrar(2, pasoGirar);
        float inicioH = cannon != null ? cannon.HorizontalAngle : 0f;
        yield return new WaitUntil(() => cannon == null ||
            Mathf.Abs(Mathf.DeltaAngle(inicioH, cannon.HorizontalAngle)) >= gradosGirar);
        yield return Bien();

        // Paso 3: disparar (botón). Se detecta una bala nueva en la escena.
        Mostrar(3, pasoDisparar);
        Object balaPrevia = BuscarBala();
        yield return new WaitUntil(() =>
        {
            Object bala = BuscarBala();
            return bala != null && bala != balaPrevia;
        });
        yield return Bien();

        // Paso 4: derribar las cajas de práctica (o una caja del juego si no hay grupo de práctica).
        Mostrar(4, pasoObjetivo);
        var cajas = new List<Transform>();
        var posiciones = new List<Vector3>();
        if (cajasTutorial != null)
        {
            foreach (Rigidbody rb in cajasTutorial.GetComponentsInChildren<Rigidbody>())
            { cajas.Add(rb.transform); posiciones.Add(rb.transform.position); }
        }
        else
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>())
                    if (t.name.Contains(nombreCajas) && t.GetComponent<Rigidbody>() != null)
                    { cajas.Add(t); posiciones.Add(t.position); }
        }
        int necesarias = cajasTutorial != null ? cajas.Count : Mathf.Min(1, cajas.Count);
        int contadas = -1;
        while (necesarias > 0)
        {
            int movidas = CajasMovidas(cajas, posiciones);
            if (movidas != contadas)
            {
                contadas = movidas;
                if (textoPaso != null && cajasTutorial != null)
                    textoPaso.text = $"Paso 4 de {TotalPasos}  ·  Cajas: {movidas} / {necesarias}";
            }
            if (movidas >= necesarias) break;
            yield return null;
        }
        yield return Bien();

        // Final.
        Sonar(sonidoFinal != null ? sonidoFinal : sonidoPaso);
        if (textoPaso != null) textoPaso.text = "";
        if (textoInstruccion != null) textoInstruccion.text = textoFinal;
        yield return new WaitForSeconds(segundosFinal);
        if (panel != null) panel.SetActive(false);

        // Arranca el juego real (misma escena, ya sin tutorial y con las cajas del juego).
        if (iniciarJuegoAlFinal)
        {
            if (MenuManager.Instance != null) MenuManager.Instance.Jugar();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    int CajasMovidas(List<Transform> cajas, List<Vector3> posiciones)
    {
        int total = 0;
        for (int i = 0; i < cajas.Count; i++)
            if (cajas[i] == null || Vector3.Distance(cajas[i].position, posiciones[i]) >= distanciaCaja) total++;
        return total;
    }

    void Mostrar(int paso, string instruccion)
    {
        if (textoPaso != null) textoPaso.text = $"Paso {paso} de {TotalPasos}";
        if (textoInstruccion != null) textoInstruccion.text = instruccion;
    }

    void Sonar(AudioClip clip)
    {
        if (clip == null) return;
        if (fuenteAudio == null)
        {
            fuenteAudio = gameObject.AddComponent<AudioSource>();
            fuenteAudio.playOnAwake = false;
            fuenteAudio.spatialBlend = 0f; // 2D: se oye igual en toda la pantalla
        }
        fuenteAudio.PlayOneShot(clip, volumen);
    }

    IEnumerator Bien()
    {
        Sonar(sonidoPaso);
        if (textoInstruccion != null) textoInstruccion.text = textoBien;
        yield return new WaitForSeconds(segundosBien);
    }

    static Object BuscarBala()
    {
        Object bala = FindAnyObjectByType<ExplosiveShot>();
        if (bala == null) bala = FindAnyObjectByType<Shot>();
        return bala;
    }

    bool AlgunaCajaMovida(List<Transform> cajas, List<Vector3> posiciones)
    {
        for (int i = 0; i < cajas.Count; i++)
        {
            if (cajas[i] == null) return true; // destruida por la zona de puntaje
            if (Vector3.Distance(cajas[i].position, posiciones[i]) >= distanciaCaja) return true;
        }
        return false;
    }
}
