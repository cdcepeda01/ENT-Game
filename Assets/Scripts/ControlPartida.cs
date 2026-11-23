using System.Collections;
using TMPro;
using UnityEngine;


public class ControlPartida : MonoBehaviour
{
    [Header("Partida")]
    [Tooltip("Duración de la partida en segundos.")]
    public float duracion = 60f;
    [Tooltip("El generador de rebeldes de la escena. Vacío = lo busca solo.")]
    public GeneradorEnemigos generador;

    [Header("Textos en pantalla")]
    public TMP_Text textoTiempo;
    public TMP_Text textoPuntos;
    [Tooltip("Opcional: texto grande que aparece al acabar el tiempo.")]
    public TMP_Text textoFinTiempo;

    [Header("Final")]
    [Tooltip("Segundos que se ve el aviso de fin antes de la pantalla final.")]
    public float esperaAntesDePantallaFinal = 2f;
    public string mensajeFin = "¡El asedio terminó!";

    const string ClaveRecord = "RecordCanonEmperor";

    float tiempoRestante;
    int puntos;
    bool terminada;

    public int Puntos => puntos;
    public static int Record => PlayerPrefs.GetInt(ClaveRecord, 0);

    void OnEnable() => EnemigoRebelde.AlSerDerribado += SumarPuntos;
    void OnDisable() => EnemigoRebelde.AlSerDerribado -= SumarPuntos;

    void Start()
    {
        if (generador == null) generador = FindAnyObjectByType<GeneradorEnemigos>();
        tiempoRestante = duracion;
        puntos = 0;
        if (textoFinTiempo != null) textoFinTiempo.gameObject.SetActive(false);
        if (generador != null) generador.Iniciar();
        ActualizarTextos();
    }

    void Update()
    {
        if (terminada) return;

        tiempoRestante -= Time.deltaTime; // con la pausa abierta el tiempo no corre
        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            ActualizarTextos();
            StartCoroutine(Terminar());
            return;
        }
        ActualizarTextos();
    }

    void SumarPuntos(EnemigoRebelde enemigo)
    {
        if (terminada) return;
        puntos += enemigo != null ? enemigo.puntos : 10;
        ActualizarTextos();
        if (DebeCelebrar(gameObject.scene.name, puntos))
        {
            terminada = true;
            if (generador != null) generador.Detener();
            if (puntos > Record) { PlayerPrefs.SetInt(ClaveRecord, puntos); PlayerPrefs.Save(); }
            if (MenuManager.Instance != null) MenuManager.Instance.TerminarTutorial("S4_Felicidades");
            else { Time.timeScale = 1f; UnityEngine.SceneManagement.SceneManager.LoadScene("S4_Felicidades"); }
        }
    }

    public static bool DebeCelebrar(string escena, int puntuacion) => escena == "S3_JuegoPrincipal" && puntuacion > 100;

    void ActualizarTextos()
    {
        int segundos = Mathf.CeilToInt(tiempoRestante);
        string color = segundos <= 10 ? "FF9A82" : "FFF5DF";
        if (textoTiempo != null)
            textoTiempo.text = $"<size=26><color=#D7B77C>Tiempo restante</color></size>\n<color=#{color}>{segundos / 60:00}:{segundos % 60:00}</color>";
        if (textoPuntos != null)
            textoPuntos.text = $"<color=#A51D2D><size=28>Puntos</size>\n{puntos:N0}</color>";
    }

    IEnumerator Terminar()
    {
        terminada = true;
        if (generador != null)
        {
            generador.Detener();
            generador.LimpiarEnemigos();
        }

        int recordAnterior = Record;
        bool nuevoRecord = puntos > recordAnterior;
        if (nuevoRecord)
        {
            PlayerPrefs.SetInt(ClaveRecord, puntos);
            PlayerPrefs.Save();
        }

        if (textoFinTiempo != null)
        {
            textoFinTiempo.text = mensajeFin;
            textoFinTiempo.gameObject.SetActive(true);
        }
        yield return new WaitForSecondsRealtime(esperaAntesDePantallaFinal);
        if (textoFinTiempo != null) textoFinTiempo.gameObject.SetActive(false);

        if (MenuManager.Instance != null)
            MenuManager.Instance.MostrarFin(puntos, nuevoRecord ? puntos : recordAnterior, nuevoRecord);
        else
            Debug.LogWarning("ControlPartida: no hay MenuManager en la escena para mostrar la pantalla final.", this);
    }

    [ContextMenu("Borrar récord guardado")]
    void BorrarRecord()
    {
        PlayerPrefs.DeleteKey(ClaveRecord);
        Debug.Log("Récord borrado.");
    }
}
