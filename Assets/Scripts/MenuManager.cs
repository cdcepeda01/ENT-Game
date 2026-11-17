using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }
    public static int LastMenuInteractionFrame { get; private set; } = -1;
    public bool isMainMenuShowing = true;
    public CanvasGroup canvasGroup;
    SimpleControls controls;

    public static bool IsAnyMenuOpen => ActiveMenuRoot != null;
    public static GameObject ActiveMenuRoot
    {
        get
        {
            PauseMenu pause = FindAnyObjectByType<PauseMenu>();
            if (pause != null && pause.IsOpen)
                return pause.optionsPanel.activeSelf ? pause.optionsPanel : pause.pausePanel;
            if (Instance != null && Instance.isActiveAndEnabled && Instance.isMainMenuShowing)
                return Instance.gameObject;
            MainMenu main = FindAnyObjectByType<MainMenu>();
            if (main != null)
                return main.optionsPanel.activeSelf ? main.optionsPanel : main.mainPanel;
            return null;
        }
    }

    public static void MarkMenuInteraction() => LastMenuInteractionFrame = Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; LastMenuInteractionFrame = -1; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        controls = new SimpleControls();
        controls.gameplay.menu.performed += _ =>
        {
            // PauseMenu ya escucha Esc/Start en las escenas del vehículo.
            if (FindAnyObjectByType<PauseMenu>() == null) ToggleMenu();
        };
    }

    void OnEnable()
    {
        if (Instance == null) Instance = this;
        controls?.gameplay.menu.Enable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start() => SetVisible(isMainMenuShowing);
    void OnDisable()
    {
        controls?.Disable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnDestroy()
    {
        controls?.Dispose();
        if (Instance == this) Instance = null;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => MarkMenuInteraction();

    void LateUpdate()
    {
        if (!isMainMenuShowing) return;
        EnsureSelection();
    }

    public void EnsureSelection()
    {
        if (EventSystem.current == null) return;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null && selected.transform.IsChildOf(transform) &&
            selected.TryGetComponent(out Selectable existing) && existing.IsActive() && existing.IsInteractable()) return;
        foreach (Selectable choice in GetComponentsInChildren<Selectable>())
        {
            if (!choice.IsActive() || !choice.IsInteractable()) continue;
            EventSystem.current.SetSelectedGameObject(choice.gameObject);
            break;
        }
    }

    public void ToggleMenu()
    {
        // La pantalla principal no tiene una partida a la que regresar.
        if (isMainMenuShowing && SceneManager.GetActiveScene().name == "MainMenu") return;
        SetVisible(!isMainMenuShowing);
    }

    void SetVisible(bool visible)
    {
        MarkMenuInteraction();
        isMainMenuShowing = visible;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
        Time.timeScale = visible ? 0f : 1f;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        if (visible) EnsureSelection();
        else if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void LoadLevel(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"La escena {sceneIndex} no está en Build Profiles.", this);
            return;
        }
        SetVisible(false);
        SceneManager.LoadScene(sceneIndex);
    }

    public void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ───────────── AGREGADO: pantallas Inicio / Tutorial / Pausa ─────────────

    [Header("Pantallas ")]
    [Tooltip("Panel con Jugar, Ver tutorial y Salir.")]
    public GameObject panelInicio;
    [Tooltip("Panel del tutorial, con Jugar y Volver.")]
    public GameObject panelTutorial;
    [Tooltip("Panel de pausa, con Continuar, Reiniciar y Volver al menú.")]
    public GameObject panelPausa;

    [Header("Escenas ")]
    public string escenaMenu = "MainMenu";
    public string escenaJuego = "Level_01";

    bool viendoTutorial;

    // Muestra el panel que corresponde según la escena en la que se esté.
    void Update()
    {
        bool enMenu = SceneManager.GetActiveScene().name == escenaMenu;
        if (!enMenu) viendoTutorial = false;
        Mostrar(panelInicio, enMenu && !viendoTutorial);
        Mostrar(panelTutorial, enMenu && viendoTutorial);
        Mostrar(panelPausa, !enMenu && !mostrandoFin);
        Mostrar(panelFin, !enMenu && mostrandoFin);
        // La pantalla final no se puede cerrar con Esc: se vuelve a mostrar.
        if (mostrandoFin && !enMenu && !isMainMenuShowing) SetVisible(true);
    }

    static void Mostrar(GameObject panel, bool visible)
    {
        if (panel != null && panel.activeSelf != visible) panel.SetActive(visible);
    }

    // Botón "Jugar" (en Inicio y en Tutorial).
    public void Jugar()
    {
        viendoTutorial = false;
        CargarEscena(escenaJuego, false);
    }

    // Botón "Ver tutorial".
    public void VerTutorial()
    {
        viendoTutorial = true;
        Update();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        EnsureSelection();
    }

    // Botón "Volver" dentro del tutorial.
    public void VolverAlInicio()
    {
        viendoTutorial = false;
        Update();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        EnsureSelection();
    }

    // Botón "Continuar" de la pausa.
    public void Continuar()
    {
        if (SceneManager.GetActiveScene().name == escenaMenu) return;
        if (isMainMenuShowing) ToggleMenu();
    }

    // Botón "Reiniciar" de la pausa.
    public void Reiniciar()
    {
        CargarEscena(SceneManager.GetActiveScene().name, false);
    }

    // Botón "Volver al menú" de la pausa.
    public void IrAlMenu()
    {
        viendoTutorial = false;
        CargarEscena(escenaMenu, true);
    }

    // Botón "Salir".
    public void Salir()
    {
        QuitApplication();
    }

    // Botón "Jugar" de la pantalla Tutorial: carga el nivel con el tutorial guiado.
    public void ComenzarTutorial()
    {
        TutorialGuiado.Activo = true;
        viendoTutorial = false;
        CargarEscena(escenaTutorial, false);
    }

    [Header("Escena del tutorial")]
    public string escenaTutorial = "Level_03";

    void CargarEscena(string nombre, bool mostrarMenu)
    {
        if (!Application.CanStreamedLevelBeLoaded(nombre))
        {
            Debug.LogWarning($"La escena '{nombre}' no está en Build Profiles.", this);
            return;
        }
        mostrandoFin = false;
        SetVisible(mostrarMenu);
        SceneManager.LoadScene(nombre);
    }

    // ───────────── AGREGADO: pantalla final de la partida ─────────────

    [Header("Pantalla final")]
    [Tooltip("Panel con el puntaje, el récord, Volver a jugar y Menú.")]
    public GameObject panelFin;
    [Tooltip("Texto donde se escribe el puntaje de la partida.")]
    public TMPro.TMP_Text textoPuntajeFin;
    [Tooltip("Texto donde se escribe el récord (o ¡NUEVO RÉCORD!).")]
    public TMPro.TMP_Text textoRecordFin;

    bool mostrandoFin;

    /// <summary>La llama ControlPartida cuando se acaba el tiempo.</summary>
    public void MostrarFin(int puntos, int record, bool nuevoRecord)
    {
        if (textoPuntajeFin != null) textoPuntajeFin.text = $"Puntos: {puntos}";
        if (textoRecordFin != null)
            textoRecordFin.text = nuevoRecord ? "¡NUEVO RÉCORD!" : $"Récord: {record}";
        mostrandoFin = true;
        Update();
        SetVisible(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        EnsureSelection();
    }

    // Botón "Volver a jugar" de la pantalla final.
    public void VolverAJugar()
    {
        CargarEscena(SceneManager.GetActiveScene().name, false);
    }
}
