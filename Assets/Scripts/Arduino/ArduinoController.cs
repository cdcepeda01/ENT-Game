using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Adapta C1 a las acciones del cañón y al EventSystem existente.</summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class ArduinoController : MonoBehaviour
{
    public static ArduinoController Instance { get; private set; }
    public enum InputLayout { TwoAxes, HorizontalOnlyA0, HorizontalOnlyA1, VerticalOnlyA0 }
    [Tooltip("Two Axes: A0 inclina y A1 gira. Horizontal Only: solo gira desde el pin indicado; ignora el otro pin.")]
    public InputLayout inputLayout = InputLayout.TwoAxes;
    public bool VerticalInputEnabled => inputLayout == InputLayout.TwoAxes || inputLayout == InputLayout.VerticalOnlyA0;
    public bool HorizontalInputEnabled => inputLayout != InputLayout.VerticalOnlyA0;
    InputLayout activeLayout;

    [Serializable]
    public class Axis
    {
        public int min = 0, max = 1023;
        [Tooltip("Lectura cruda con el mando en el centro: aquí no hay movimiento.")]
        public int center = 512;
        [Tooltip("Zona central sin movimiento, como soltar W/S o A/D.")]
        [Range(0.01f, 0.4f)] public float deadZone = 0.1f;
        public bool invert;
        [Range(0.1f, 2f)] public float sensitivity = 1f;
        [Tooltip("Histéresis contra ruido, en unidades del ADC.")]
        [Range(1, 30)] public int noiseThreshold = 4;
        [Tooltip("Movimiento acumulado mínimo para tomar el control al teclado.")]
        [Range(2, 100)] public int activityThreshold = 12;
        [Tooltip("Paquetes consecutivos en la misma dirección antes de mover. C1 envía 40 por segundo.")]
        [Range(2, 8)] public int directionConfirmationSamples = 3;
        [Header("Diagnóstico en Play")]
        public int raw;
        public float value;
        public float movement;
        public int Revision { get; private set; }
        bool initialized;
        int a, b, accepted, activityAnchor;
        int candidateDirection, directionSamples;

        public void Reset()
        {
            initialized = false; Revision = 0;
            candidateDirection = directionSamples = 0;
            raw = 0; value = 0.5f; movement = 0f;
        }

        public bool Sample(int reading)
        {
            raw = reading;
            if (!initialized)
            {
                a = b = accepted = activityAnchor = reading;
                initialized = true;
                UpdateValue();
                return false; // Conectar la placa no mueve el cañón.
            }
            // Mediana de tres muestras contra picos de una sola lectura.
            int median = Mathf.Max(Mathf.Min(a, b), Mathf.Min(Mathf.Max(a, b), reading));
            a = b; b = reading;
            if (Mathf.Abs(median - accepted) >= Mathf.Max(1, noiseThreshold)) accepted = median;
            UpdateValue();
            // Una mediana de tres NO elimina una señal que alterna bajo/alto.
            // Confirmar la dirección con muestras crudas consecutivas antes de
            // permitir que el Arduino tome el control o cambie de sentido.
            float rawMovement = ToMovement(reading);
            int direction = rawMovement == 0f ? 0 : rawMovement > 0f ? 1 : -1;
            if (direction == 0)
            {
                candidateDirection = directionSamples = 0;
                movement = 0f;
                activityAnchor = reading;
            }
            else
            {
                if (candidateDirection != direction) { candidateDirection = direction; directionSamples = 0; }
                directionSamples = Mathf.Min(directionSamples + 1, Mathf.Max(2, directionConfirmationSamples));
                if (directionSamples < Mathf.Max(2, directionConfirmationSamples) ||
                    Mathf.Sign(movement) != direction) movement = 0f;
            }
            if (Mathf.Abs(accepted - activityAnchor) < Mathf.Max(noiseThreshold + 1, activityThreshold)) return false;
            if (movement == 0f) return false;
            activityAnchor = accepted;
            // El ruido dentro del centro no debe quitarle el control al teclado.
            if (movement != 0f) Revision++;
            return true;
        }

        void UpdateValue()
        {
            float t = Mathf.InverseLerp(min, Mathf.Max(min + 1, max), accepted);
            if (invert) t = 1f - t;
            value = Mathf.Clamp01(0.5f + (t - 0.5f) * sensitivity);
            movement = ToMovement(accepted);
        }

        float ToMovement(int reading)
        {
            int upper = Mathf.Max(min + 2, max);
            int neutral = Mathf.Clamp(center, min + 1, upper - 1);
            float direction = reading >= neutral
                ? Mathf.InverseLerp(neutral, upper, reading)
                : -Mathf.InverseLerp(neutral, min, reading);
            float magnitude = Mathf.Abs(direction);
            float zone = Mathf.Clamp(deadZone, 0.01f, 0.4f);
            float result = magnitude <= zone ? 0f : Mathf.Sign(direction) *
                Mathf.Clamp01((magnitude - zone) / (1f - zone) * sensitivity);
            return invert ? -result : result;
        }
    }

    [Tooltip("Joystick 1, VRy en A0: solo inclinación, equivalente a W/S.")]
    public Axis vertical = new Axis { invert = true };
    [Tooltip("Joystick 2, VRx en A1: solo giro, equivalente a A/D.")]
    public Axis horizontal = new Axis();
    [Tooltip("Vacío: busca el cañón activo al cambiar de escena.")]
    public CannonController cannon;
    [Tooltip("Filtrado adicional en Unity; el sketch también elimina rebotes.")]
    [Range(0f, 0.1f)] public float buttonDebounce = 0.025f;
    [Tooltip("En el menú, el potenciómetro vertical elige la opción resaltada.")]
    public bool navigateMenusWithPotentiometers = true;
    public bool HasInput { get; private set; }
    public int InputSession => currentSession;
    int currentSession = -1;
    readonly DebouncedButton menu = new DebouncedButton();
    readonly DebouncedButton fire = new DebouncedButton();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    void OnEnable()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        ResetInput();
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
        ResetInput();
    }

    void ResetInput()
    {
        activeLayout = inputLayout;
        HasInput = false;
        currentSession = -1;
        vertical.Reset(); horizontal.Reset();
        menu.Reset(false, 0); fire.Reset(false, 0);
    }

    void Update()
    {
        ArduinoSerial serial = ArduinoSerial.Instance;
        if (serial == null || !serial.IsConnected) { if (HasInput) ResetInput(); return; }
        if (cannon == null || !cannon.isActiveAndEnabled) cannon = FindAnyObjectByType<CannonController>();
        while (serial.TryDequeue(out ArduinoSerial.Frame frame)) ProcessFrame(frame);
    }

    // La prueba sin hardware utiliza esta misma ruta, sin reemplazar filtros ni acciones.
    public void ProcessFrame(ArduinoSerial.Frame frame)
    {
        if (activeLayout != inputLayout) ResetInput();
        int horizontalReading = inputLayout == InputLayout.HorizontalOnlyA0 ? frame.Vertical : frame.Horizontal;
        double now = frame.ReceivedAt / (double)System.Diagnostics.Stopwatch.Frequency;
        if (currentSession != frame.Session)
        {
            ResetInput();
            currentSession = frame.Session;
            menu.Reset(frame.Menu, now); fire.Reset(frame.Fire, now);
            if (VerticalInputEnabled) vertical.Sample(frame.Vertical);
            if (HorizontalInputEnabled) horizontal.Sample(horizontalReading);
            HasInput = true;
            return; // Al reconectar, un botón mantenido requiere soltarse antes de actuar.
        }
        bool vChanged = VerticalInputEnabled && vertical.Sample(frame.Vertical);
        bool hChanged = HorizontalInputEnabled && horizontal.Sample(horizontalReading);
        bool menuPressed = menu.Sample(frame.Menu, now, buttonDebounce);
        bool firePressed = fire.Sample(frame.Fire, now, buttonDebounce);
        if (vChanged || hChanged || menuPressed || firePressed) InputHint.UsingArduino = true;
        // Si llegan juntos, menú tiene prioridad y consume la pulsación de disparo.
        if (menuPressed) { ToggleMenu(); return; }
        bool inMenu = MenuManager.IsAnyMenuOpen;
        if (inMenu && navigateMenusWithPotentiometers) NavigateMenu(vChanged, hChanged);
        if (!firePressed) return;
        if (inMenu) Submit();
        else if (cannon != null) cannon.Fire();
    }

    void ToggleMenu()
    {
        MenuManager.MarkMenuInteraction();
        PauseMenu pause = FindAnyObjectByType<PauseMenu>();
        if (pause != null) { pause.OpenOrBack(); return; }
        if (MenuManager.Instance != null) { MenuManager.Instance.ToggleMenu(); return; }
        MainMenu main = FindAnyObjectByType<MainMenu>();
        if (main != null) main.Back();
    }

    static Selectable[] MenuOptions()
    {
        GameObject root = MenuManager.ActiveMenuRoot;
        if (root == null) return new Selectable[0];
        return Array.FindAll(root.GetComponentsInChildren<Selectable>(), s => s.IsActive() && s.IsInteractable());
    }

    void NavigateMenu(bool vChanged, bool hChanged)
    {
        EventSystem events = EventSystem.current;
        if (events == null) return;
        Selectable[] choices = MenuOptions();
        if (choices.Length == 0) return;
        if (vChanged)
        {
            float t = vertical.invert ? 1f - vertical.value : vertical.value;
            int index = Mathf.Min(choices.Length - 1, Mathf.FloorToInt(t * choices.Length));
            events.SetSelectedGameObject(choices[index].gameObject);
        }
        if (hChanged && events.currentSelectedGameObject != null &&
            events.currentSelectedGameObject.TryGetComponent(out Slider slider) && slider.IsInteractable())
            slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, horizontal.value);
    }

    public void Submit()
    {
        if (!MenuManager.IsAnyMenuOpen || EventSystem.current == null) return;
        MenuManager.MarkMenuInteraction();
        Selectable[] choices = MenuOptions();
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !Array.Exists(choices, s => s.gameObject == selected))
        {
            selected = choices.Length > 0 ? choices[0].gameObject : null;
            EventSystem.current.SetSelectedGameObject(selected);
        }
        if (selected != null)
            ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }

    sealed class DebouncedButton
    {
        bool candidate, stable, armed;
        double changedAt;
        public void Reset(bool down, double now)
        { candidate = stable = down; armed = !down; changedAt = now; }
        public bool Sample(bool down, double now, float debounce)
        {
            if (candidate != down) { candidate = down; changedAt = now; }
            if (candidate == stable || now - changedAt < debounce) return false;
            stable = candidate;
            if (!stable) { armed = true; return false; }
            if (!armed) return false;
            armed = false;
            return true;
        }
    }
}
