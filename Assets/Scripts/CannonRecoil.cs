using UnityEngine;

/// <summary>
/// Modelo de traslación de un cañón apoyado en un suelo horizontal ideal.
/// Estado dinámico: velocidad y posición. Entrada: el impulso real aplicado al proyectil.
/// </summary>
[DisallowMultipleComponent]
public class CannonRecoil : MonoBehaviour, ISimulable
{
    [Min(0.01f), Tooltip("Masa del cañón cargado sin el proyectil que acaba de salir, en kg.")]
    public float cannonMass = 60f;
    [Min(0f), Tooltip("Coeficiente de fricción efectiva del apoyo. 0: suelo sin rozamiento.")]
    public float frictionCoefficient = 0.04f;

    [Header("Estado de la simulación (actualizado durante Play)")]
    [SerializeField] Vector3 velocity;
    [SerializeField] Vector3 lastProjectileImpulse;
    [SerializeField] Vector3 lastSupportImpulse;
    [SerializeField] float travelledDistance;
    [SerializeField] float dissipatedEnergy;

    Vector3 initialPosition;
    bool initialized;
    Simulate simulationManager;

    public Vector3 Velocity => velocity;
    public Vector3 Momentum => Mathf.Max(0.01f, cannonMass) * velocity;
    public Vector3 LastProjectileImpulse => lastProjectileImpulse;
    public Vector3 LastSupportImpulse => lastSupportImpulse;
    public Vector3 Displacement => initialized ? transform.position - initialPosition : Vector3.zero;
    public float KineticEnergy => 0.5f * Mathf.Max(0.01f, cannonMass) * velocity.sqrMagnitude;
    public float TravelledDistance => travelledDistance;
    public float DissipatedEnergy => dissipatedEnergy;
    public float FrictionDeceleration => Mathf.Max(0f, frictionCoefficient) * Mathf.Abs(Physics.gravity.y);

    void Awake() => Initialize();

    void Initialize()
    {
        if (initialized) return;
        initialPosition = transform.position;
        initialized = true;
    }

    void Start()
    {
        simulationManager = global::Simulate.Instance;
        if (simulationManager != null && !simulationManager.IsRegistered(this))
            simulationManager.Add(this);
    }

    void FixedUpdate()
    {
        simulationManager = global::Simulate.Instance;
        // Simulate.Step es el único integrador cuando estamos registrados allí.
        // Level_01 no tiene ese gestor: se usa el paso fijo de Unity.
        if (simulationManager != null && simulationManager.isActiveAndEnabled &&
            simulationManager.IsRegistered(this)) return;
        Simulate(Time.fixedDeltaTime);
    }

    void OnDestroy()
    {
        if (simulationManager != null) simulationManager.Remove(this);
    }

    /// <summary>Delta p del cañón = -J del proyectil, en el plano de apoyo.</summary>
    public void ApplyProjectileImpulse(Vector3 projectileImpulse)
    {
        if (!isActiveAndEnabled) return;
        Initialize();
        lastProjectileImpulse = projectileImpulse;
        // El suelo ideal impide el movimiento vertical y aporta este impulso externo.
        lastSupportImpulse = Vector3.up * projectileImpulse.y;
        velocity -= Vector3.ProjectOnPlane(projectileImpulse, Vector3.up) / Mathf.Max(0.01f, cannonMass);
    }

    public void Simulate(float h)
    {
        if (!isActiveAndEnabled || h <= 0f) return;
        Initialize();
        float speed = velocity.magnitude;
        if (speed == 0f) return;
        Vector3 direction = velocity / speed;
        float deceleration = FrictionDeceleration;
        // Integración de dv/dt = -mu*g*normalizar(v). Si se detiene dentro
        // del paso, integrar solo hasta v=0 para no invertir el movimiento.
        float movingTime = deceleration > 0f ? Mathf.Min(h, speed / deceleration) : h;
        float distance = Mathf.Max(0f, speed * movingTime - 0.5f * deceleration * movingTime * movingTime);
        float nextSpeed = Mathf.Max(0f, speed - deceleration * h);
        transform.position += direction * distance;
        velocity = direction * nextSpeed;
        travelledDistance += distance;
        dissipatedEnergy += 0.5f * Mathf.Max(0.01f, cannonMass) * (speed * speed - nextSpeed * nextSpeed);
    }

    [ContextMenu("Restablecer simulación de retroceso")]
    public void ResetState()
    {
        Initialize();
        transform.position = initialPosition;
        velocity = lastProjectileImpulse = lastSupportImpulse = Vector3.zero;
        travelledDistance = dissipatedEnergy = 0f;
    }

    void OnValidate()
    {
        cannonMass = Mathf.Max(0.01f, cannonMass);
        frictionCoefficient = Mathf.Max(0f, frictionCoefficient);
    }
}
