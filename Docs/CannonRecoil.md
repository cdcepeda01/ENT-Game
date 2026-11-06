# Simulación física del retroceso del cañón

Implementada en Assets/Scripts/CannonRecoil.cs e integrada en el Fire existente de CannonController. La escena Level_01 incluye el componente y sus valores iniciales.

## Qué se simula

Se modela la traslación horizontal del conjunto del cañón y su frenado por fricción efectiva. La posición se obtiene integrando la velocidad; la velocidad cambia por los impulsos de disparo y las fuerzas de fricción. No hay Animator, clip, curva de retroceso, Tween, Lerp hacia un destino ni regreso automático.

La masa del cañón y el coeficiente de fricción son parámetros físicos, no duración ni amplitud de un efecto. Un segundo disparo suma su impulso al estado actual: no reinicia una secuencia visual.

## Ecuaciones y unidades

- M: masa del cañón sin el proyectil que sale, en kg.
- m: masa del Rigidbody del proyectil, en kg.
- J: impulso vectorial del disparo, en N·s = kg·m/s.
- V: velocidad del cañón antes de disparar, en m/s.
- mu: coeficiente de fricción efectiva, sin unidades.
- g: valor absoluto de Physics.gravity.y, en m/s².

El campo existente projectileForce conserva su nombre para mantener referencias serializadas, pero ForceMode.Impulse lo interpreta como un **impulso**, no como una fuerza constante. [Documentación de Unity: AddForce](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rigidbody/addforce).

Para un disparo horizontal:

\`\`\`text
v_proyectil = V + J/m
v_cañón    = V - J/M

Momento antes:   (M + m) V
Momento después: M (V - J/M) + m (V + J/m)
               = (M + m) V
\`\`\`

El proyectil hereda V antes de recibir AddForce(J, Impulse). La simulación del cañón recibe el opuesto del mismo J; no existe un multiplicador artístico de retroceso.

Cuando el cañón está elevado, se proyecta el impulso de reacción sobre el plano XZ. La componente vertical se equilibra con una reacción externa del suelo. Por eso el momento de cañón+proyectil **solo se conserva horizontalmente**; el suelo intercambia el momento vertical.

Entre disparos se utiliza fricción de Coulomb efectiva:

\`\`\`text
F_fricción = -mu M g normalizar(V)
a          = -mu g normalizar(V)
dx/dt      = V
\`\`\`

En cada paso h se integra el tramo de desaceleración constante. Si el cuerpo se detiene dentro del paso, se calcula el tiempo exacto hasta V=0 y se integra solo ese tramo. Esto impide que el frenado invierta la velocidad o provoque vibración.

Para un disparo horizontal desde reposo y mu>0:

\`\`\`text
rapidez inicial = |J| / M
tiempo de parada = rapidez inicial / (mu g)
distancia de parada = rapidez inicial² / (2 mu g)
energía disipada = (1/2) M rapidez inicial²
\`\`\`

Con mu=0 conserva su velocidad: no se introduce un frenado oculto.

## Ejemplo de los valores configurados

| Parámetro | Valor |
|---|---:|
| Masa del cañón | 60 kg |
| Impulso existente del disparo | 30 N·s |
| Fricción efectiva | 0,04 |
| Gravedad, si la configuración sigue en Tierra | 9,81 m/s² |

En un disparo horizontal desde reposo resulta aproximadamente:

- Velocidad inicial del cañón: 0,50 m/s hacia atrás.
- Tiempo hasta detenerse: 1,27 s.
- Desplazamiento: 0,319 m.
- Energía cinética inicial/fricción disipada: 7,5 J.

El prefab Bomb_Shot tiene masa de 2 kg: con 30 N·s su incremento de velocidad es 15 m/s. Desde un cañón en movimiento se añade su velocidad inicial V.

Estos números no se almacenan como trayectoria ni temporizador: son predicciones que se pueden contrastar con la ejecución. Al variar masa, impulso, inclinación, fricción o disparar de nuevo durante el movimiento, el resultado cambia.

## Uso en Unity

1. Abrir Assets/Scenes/Level_01.unity e iniciar Play.
2. Seleccionar Cannon en la jerarquía y observar su componente CannonRecoil.
3. Disparar con Space o con el botón Arduino ya configurado. Ambas entradas utilizan el mismo Fire.
4. Comparar cannonMass=60 y 120: a igual impulso, la velocidad inicial se reduce a la mitad; con el mismo mu, la distancia de parada pasa a un cuarto.
5. Probar frictionCoefficient=0: el cañón continúa desplazándose.
6. Repetir un disparo antes de que termine el retroceso y observar la suma de velocidades.
7. Abrir el menú: el tiempo de juego y el retroceso quedan pausados.
8. Para reiniciar a propósito, abrir el menú contextual del componente y elegir “Restablecer simulación de retroceso”, o llamar ResetState().

El Inspector expone la velocidad, el último impulso del proyectil, la reacción vertical del suelo, la distancia recorrida y la energía disipada. Las propiedades Velocity, Momentum, Displacement y KineticEnergy sirven también para análisis y pruebas.

La posición final se mantiene al detenerse. Volver solo al punto de salida requeriría otra fuerza física, por ejemplo un resorte/amortiguador de una cureña, y no forma parte de este cañón libre sobre suelo.

## Integración con la simulación existente

CannonRecoil implementa ISimulable.

- En Level_01, que no contiene un gestor Simulate, avanza en FixedUpdate con Time.fixedDeltaTime.
- Si existe un gestor Simulate, se registra una sola vez y utiliza Simulate.Step(h). No se ejecuta a la vez el integrador propio.
- Se respetan pausa, paso manual y ResetSimulation del gestor.
- Al destruir el cañón se retira del gestor.
- Se añadieron IsRegistered y Remove a Simulate, y se evitó registrar dos veces un objeto durante Start/Add.

El movimiento de puntería sigue afectando a sus ejes locales originales. La reacción es una traslación del objeto completo en coordenadas del mundo, en sentido opuesto al impulso horizontal del disparo. La dirección del retroceso no gira artificialmente al cambiar la puntería durante el frenado.

## Alcance físico del modelo

Es una **simulación física de masa puntual con restricción de suelo horizontal**, no una simulación completa de un vehículo de ruedas:

- La altura se mantiene por la restricción ideal de apoyo.
- No calcula vuelco, suspensión, contacto irregular, colisiones del cañón con obstáculos ni dinámica rotacional de ruedas.
- mu agrupa la resistencia efectiva al desplazamiento; no modela por separado rodadura y deslizamiento de cada rueda.
- El impulso del proyectil es una entrada del modelo; no se simulan combustión, presión del gas ni balística interna.
- No se incluye impulso adicional de los gases. Para incluirlo habría que modelar también la masa/velocidad expulsada, conservando momento.
- Los parámetros iniciales son valores de demostración, no una calibración contra un cañón real.

Estas hipótesis deben aparecer en la explicación académica para distinguir el fenómeno que sí se simula de los fenómenos que se han simplificado.

## Pruebas

Validación en Unity 6000.6.0f1: **41 pruebas aprobadas, 0 fallidas** (9 físicas y 32 de teclado/Arduino/menús, incluido el control por velocidad con centro neutro).

Tools > Simulation > Run cannon checks (Ctrl+Shift+F9) ejecuta las pruebas físicas y la regresión existente de teclado/Arduino. El resultado XML queda en Logs/Arduino-test-results.xml.

Las comprobaciones físicas cubren conservación del momento horizontal, masa, reacción del suelo, distancia de frenado, balance de energía, consistencia entre pasos de tiempo, ausencia de fricción, suma de disparos, reinicio y un disparo real en Level_01 con herencia de velocidad y pausa.

