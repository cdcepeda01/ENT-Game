using UnityEngine;

/// <summary>Una sola ruta de rotación/disparo; Input System y Arduino conservan sus entradas.</summary>
[RequireComponent(typeof(CannonRecoil))]
public class CannonController : MonoBehaviour
{
    [SerializeField] private Transform barrel;
    [SerializeField] private Transform barrelSpawnLocation;
    [SerializeField] private Transform wheelFrontRight;
    [SerializeField] private Transform wheelFrontLeft;
    [SerializeField] private Transform wheelBackRight;
    [SerializeField] private Transform wheelBackLeft;
    public GameObject projectilePrefab;
    [Tooltip("Impulso aplicado al proyectil en N·s (kg·m/s). El cañón recibe el opuesto.")]
    public float projectileForce = 30f;
    public float aimSpeed = 10f;
    public float aimRotationMin = -10f;
    public float aimRotationMax = 10f;
    public float turnSpeed = 30f;
    [Tooltip("Límites horizontales relativos a la orientación local inicial.")]
    public float turnRotationMin = -30f;
    public float turnRotationMax = 30f;
    public float shotDelay = 0.5f;
    [Header("Arduino: control de velocidad equivalente a W/S y A/D")]
    [Tooltip("Suavizado de la orden de movimiento. El centro detiene el eje inmediatamente.")]
    [Range(0.02f, 1f)] public float arduinoSmoothTime = 0.15f;
    [Tooltip("Límite adicional en grados/s. Se conservan Aim Speed y Turn Speed del teclado.")]
    [Min(1f)] public float arduinoMaxSpeed = 90f;

    SimpleControls controls;
    CannonRecoil recoil;
    float timeSinceLastShot;
    float aimAngle, turnAngle;
    Vector3 initialBarrelEuler, initialBaseEuler;
    Quaternion frontLeftRotation, frontRightRotation, backLeftRotation, backRightRotation;
    readonly AxisOwner verticalOwner = new AxisOwner();
    readonly AxisOwner horizontalOwner = new AxisOwner();
    bool fireStartedInGameplay;
    int arduinoSession = -1;
    ArduinoController.InputLayout arduinoLayout;
    public float VerticalAngle => aimAngle;
    public float HorizontalAngle => turnAngle;
    public bool ArduinoOwnsVertical => verticalOwner.UsingArduino;
    public bool ArduinoOwnsHorizontal => horizontalOwner.UsingArduino;
    public static bool GameplayBlocked => Time.timeScale <= 0f || MenuManager.IsAnyMenuOpen ||
                                          MenuManager.LastMenuInteractionFrame == Time.frameCount;

    void Awake()
    {
        recoil = GetComponent<CannonRecoil>();
        if (recoil == null) recoil = gameObject.AddComponent<CannonRecoil>();
        controls = new SimpleControls();
        // Tap/SlowTap existentes siguen intactos. Recordar dónde empezó la pulsación
        // evita que su liberación dispare después de cerrar un menú.
        controls.gameplay.fire.started += _ => fireStartedInGameplay = !GameplayBlocked;
        controls.gameplay.fire.performed += _ => { if (fireStartedInGameplay) Fire(); };
        controls.gameplay.fire.canceled += _ => fireStartedInGameplay = false;
        initialBaseEuler = transform.localEulerAngles;
        initialBarrelEuler = barrel != null ? barrel.localEulerAngles : Vector3.zero;
        aimAngle = Mathf.Clamp(Mathf.DeltaAngle(0, initialBarrelEuler.x), aimRotationMin, aimRotationMax);
        frontLeftRotation = wheelFrontLeft != null ? wheelFrontLeft.localRotation : Quaternion.identity;
        frontRightRotation = wheelFrontRight != null ? wheelFrontRight.localRotation : Quaternion.identity;
        backLeftRotation = wheelBackLeft != null ? wheelBackLeft.localRotation : Quaternion.identity;
        backRightRotation = wheelBackRight != null ? wheelBackRight.localRotation : Quaternion.identity;
    }

    void OnEnable() => controls?.Enable();
    void OnDisable()
    {
        controls?.Disable();
        fireStartedInGameplay = false;
        verticalOwner.Release(); horizontalOwner.Release();
    }
    void OnDestroy() => controls?.Dispose();

    void Update()
    {
        Vector2 move = controls.gameplay.move.ReadValue<Vector2>();
        ArduinoController arduino = ArduinoController.Instance;
        bool available = arduino != null && arduino.HasInput;
        if (available && (arduinoSession != arduino.InputSession || arduinoLayout != arduino.inputLayout))
        {
            arduinoSession = arduino.InputSession;
            arduinoLayout = arduino.inputLayout;
            verticalOwner.Suspend(move.y, arduino.vertical.Revision);
            horizontalOwner.Suspend(move.x, arduino.horizontal.Revision);
        }
        if (GameplayBlocked)
        {
            verticalOwner.Suspend(move.y, available ? arduino.vertical.Revision : 0);
            horizontalOwner.Suspend(move.x, available ? arduino.horizontal.Revision : 0);
            fireStartedInGameplay = false;
            return;
        }
        timeSinceLastShot += Time.deltaTime;
        aimAngle = verticalOwner.Resolve(aimAngle, move.y, aimSpeed, aimRotationMin, aimRotationMax,
            available && arduino.VerticalInputEnabled, available ? arduino.vertical : null, arduinoSmoothTime, arduinoMaxSpeed, Time.deltaTime);
        turnAngle = horizontalOwner.Resolve(turnAngle, move.x, turnSpeed, turnRotationMin, turnRotationMax,
            available && arduino.HorizontalInputEnabled, available ? arduino.horizontal : null, arduinoSmoothTime, arduinoMaxSpeed, Time.deltaTime);
        ApplyRotation();
    }

    void ApplyRotation()
    {
        if (barrel != null)
        {
            Vector3 rotation = initialBarrelEuler;
            rotation.x = aimAngle;
            barrel.localEulerAngles = rotation;
        }
        Vector3 baseRotation = initialBaseEuler;
        baseRotation.y += turnAngle;
        transform.localEulerAngles = baseRotation;
        RotateWheel(wheelFrontLeft, frontLeftRotation, turnAngle);
        RotateWheel(wheelBackLeft, backLeftRotation, turnAngle * 2f);
        RotateWheel(wheelFrontRight, frontRightRotation, -turnAngle);
        RotateWheel(wheelBackRight, backRightRotation, -turnAngle * 2f);
    }

    static void RotateWheel(Transform wheel, Quaternion initial, float angle)
    {
        if (wheel != null) wheel.localRotation = initial * Quaternion.Euler(angle, 0, 0);
    }

    /// <summary>Teclado y botón físico llaman exactamente a este método.</summary>
    public void Fire()
    {
        if (!isActiveAndEnabled || GameplayBlocked || timeSinceLastShot < shotDelay) return;
        if (projectilePrefab == null || barrelSpawnLocation == null) return;
        var cannonBall = Instantiate(projectilePrefab, barrelSpawnLocation.position, barrelSpawnLocation.rotation);
        if (cannonBall.TryGetComponent(out Rigidbody body) && !body.isKinematic)
        {
            Vector3 impulse = cannonBall.transform.forward * projectileForce;
            // Antes de salir, la munición comparte la velocidad del cañón.
            body.linearVelocity = recoil != null && recoil.isActiveAndEnabled ? recoil.Velocity : Vector3.zero;
            body.AddForce(impulse, ForceMode.Impulse);
            if (recoil != null) recoil.ApplyProjectileImpulse(impulse);
        }
        timeSinceLastShot = 0f;
    }

    /// <summary>Arbitraje independiente por eje; pulsaciones simultáneas favorecen al teclado.</summary>
    public sealed class AxisOwner
    {
        public bool UsingArduino { get; private set; }
        int lastRevision;
        float previousKeyboard, velocity, smoothedMovement;
        public void Release() { UsingArduino = false; velocity = smoothedMovement = 0; lastRevision = 0; previousKeyboard = 0; }
        public void Suspend(float keyboard, int revision)
        { UsingArduino = false; velocity = smoothedMovement = 0; previousKeyboard = keyboard; lastRevision = revision; }
        public float Resolve(float current, float keyboard, float keyboardSpeed, float min, float max,
            bool available, ArduinoController.Axis arduino, float smoothTime, float maxSpeed, float deltaTime)
        {
            if (available && arduino.Revision != lastRevision) UsingArduino = true;
            if (!available || (Mathf.Abs(keyboard) > 0.1f && Mathf.Abs(keyboard - previousKeyboard) > 0.1f))
            { UsingArduino = false; velocity = smoothedMovement = 0; }
            previousKeyboard = keyboard;
            lastRevision = available ? arduino.Revision : 0;
            if (!UsingArduino) return Mathf.Clamp(current + keyboard * keyboardSpeed * deltaTime, min, max);
            // El potenciómetro indica dirección/velocidad, no un ángulo objetivo.
            if (arduino.movement == 0f)
            {
                velocity = smoothedMovement = 0f;
                return Mathf.Clamp(current, min, max);
            }
            smoothedMovement = Mathf.SmoothDamp(smoothedMovement, arduino.movement, ref velocity,
                Mathf.Max(0.02f, smoothTime), Mathf.Infinity, deltaTime);
            float speed = Mathf.Min(Mathf.Abs(keyboardSpeed), Mathf.Max(1f, maxSpeed));
            return Mathf.Clamp(current + smoothedMovement * speed * deltaTime, min, max);
        }
    }
}
