## Prueba activa: VRy en A0

El prefab usa Vertical Only A0: únicamente inclinación del barrel. A1 se ignora y el giro horizontal de Arduino queda deshabilitado. W/S y A/D siguen disponibles. Con USB desconectado, sustituir VRx por VRy en A0, conservando 5V y GND. No es necesario recargar firmware. Los modos horizontales descritos abajo quedan disponibles en Input Layout.

# Cañón con Arduino y teclado

## Configuración activa: solo VRx en A0

El prefab Arduino está configurado en **Input Layout = Horizontal Only A0**.
Conectar +5V y GND del joystick a la placa y VRx a A0. El giro horizontal usa
exclusivamente A0; A1 se ignora, aunque esté desconectado o entregue valores extremos.
Arduino no cambia la inclinación del barrel en este modo. W/S sigue inclinando
con teclado y A/D sigue girando. No hace falta volver a cargar el sketch: C1
conserva sus campos físicos A0 y A1, y Unity asigna el canal a la acción elegida.

Para volver al montaje de dos ejes descrito abajo, seleccionar **Two Axes**
(A0 vertical, A1 horizontal). **Horizontal Only A1** permite probar solo VRx en A1.
Detener Play antes de cambiar la configuración que se quiere guardar.

## Estado y análisis del proyecto

Unity **6000.6.0f1**, Input System **1.20.0**, destino comprobado: **Windows Editor / Windows Standalone**. Standalone ya utiliza API Compatibility Level **.NET Framework**. En otras plataformas la parte serial se excluye por compilación y el teclado permanece disponible; no se afirma compatibilidad USB con Android/WebGL.

Se revisaron Assets, Packages, ProjectSettings, escenas, prefabs, scripts de juego, UI, simulación, ejemplos Complete y Arduino/Arduino.ino antes de editar.

- **Assets/Scripts/CannonController.cs**: cañón activo de Level_01. Movimiento local X del barrel y local Y de la base; ruedas y disparo con Rigidbody, impulso y shotDelay.
- **Assets/Samples/Input System/1.11.2/Simple Demo/SimpleControls**: acciones existentes. W/S y A/D, Space/acción primaria, Esc/Start. Se conservan el asset, sus bindings y las interacciones Tap/SlowTap.
- **Assets/Scripts/MenuManager.cs**: menú utilizado por MainMenu.unity, persistente entre escenas. Sus botones existentes cargan niveles y salen.
- **Assets/Scripts/UI/PauseMenu.cs y MainMenu.cs**: UI de la arquitectura de vehículo; el adaptador la reconoce cuando existe. Esos scripts y sus bindings permanecen intactos.
- **Assets/Scripts/Arduino/ArduinoSerial.cs y ArduinoController.cs**, **Assets/Prefabs/Arduino.prefab**: implementación anterior de acelerador/timón/freno. Se adapta este mismo par de componentes al cañón. El adaptador deja de sobrescribir Drive del vehículo.
- **Assets/Scripts/Complete/**: ejemplos de curso, sin referencias al cañón activo. No se modifican.
- **PlayerController, Pawn, Shot, ExplosiveShot, simulación, materiales, pivotes y físicas**: no se cambian.

Level_01 es la escena que actualmente tiene el cañón jugable. Level_02 y Level_03 son escenarios sin CannonController; añadir compatibilidad de entrada no añade un cañón a esos escenarios.

## Componentes y referencias ya configurados

- El prefab Arduino está en MainMenu y Level_01. Persiste con DontDestroyOnLoad y descarta duplicados.
- Level_01 incluye una instancia del menú existente, inicialmente cerrada, para que Esc y el botón físico funcionen al iniciar directamente esa escena.
- El menú mantiene su EventSystem y selección de Unity UI; abrirlo pausa y bloquea disparos y movimiento. Cerrarlo restaura la partida.
- Un CanvasGroup invisible deja de interceptar clics. Se elige una opción inicial para permitir navegación con teclado.
- Build Profiles conserva MainMenu=0 y Base=1, y añade Level_01=2, Level_02=3, Level_03=4. Los tres botones Level N de MainMenu apuntan a su nivel homónimo. Base permanece en el proyecto y en la compilación.
- Al entrar desde MainMenu, el Arduino y menú persistentes se reutilizan. No arrastrar otra copia dentro del cañón.

## Cableado propuesto (Arduino Uno/Nano clásico)

### Montaje actual: dos joysticks KY-023

| Pin | Joystick 1 (vertical) | Joystick 2 (horizontal) |
|---|---|---|
| +5V | 5V Arduino | 5V Arduino |
| GND | GND Arduino | GND Arduino |
| VRy | A0 | Sin conectar |
| VRx | Sin conectar | A1 |
| SW | Sin conectar; se conservan los botones externos | Sin conectar |

Cada joystick utiliza un solo eje. Las señales A0 y A1 deben estar en filas
independientes de la protoboard; solo alimentación y tierra se comparten.
El firmware C1 existente es compatible sin cambios. Al soltar, ambos mandos
deben permanecer dentro de la zona neutra (aproximadamente 461–563 con los
valores predeterminados), sin oscilaciones grandes. Un canal inestable puede
activar su eje aunque no se esté tocando ese joystick.

### Montaje anterior con potenciómetros

Dos potenciómetros lineales de aproximadamente 10 kΩ y dos pulsadores normalmente abiertos. Son pines propuestos: todas las constantes están al inicio del sketch.

| Componente | Conexión |
|---|---|
| Potenciómetro vertical, terminal central | A0 |
| Potenciómetro horizontal, terminal central | A1 |
| Terminales exteriores de ambos potenciómetros | VCC y GND |
| Botón de menú | D2 a GND al presionar |
| Botón disparo/confirmar | D3 a GND al presionar |
| Comunicación/alimentación | USB entre Arduino y PC |

```text
VCC ---[ potenciómetro 1 ]--- GND
                 |
                 A0   (vertical)

VCC ---[ potenciómetro 2 ]--- GND
                 |
                 A1   (horizontal)

D2 -----[ botón menú ]------ GND
D3 -----[ botón disparo ]--- GND
PC  ========== USB ======== Arduino
```

Usar 5 V de la propia placa para Uno/Nano de 5 V; en placas de 3,3 V usar su tensión de referencia adecuada. Los pulsadores usan INPUT_PULLUP: sueltos leen HIGH y presionados LOW; no necesitan una resistencia externa de pull-down. En pulsadores de cuatro patas, conectar contactos de lados que se unen al pulsar.

El sketch presupone ADC de **10 bits (0–1023)**. Si se usa una placa con otra resolución, configurarla a 10 bits o adaptar el rango del protocolo/validación.

Referencias: [lectura analógica oficial](https://docs.arduino.cc/tutorials/uno-rev3/AnalogReadSerial), [INPUT_PULLUP](https://docs.arduino.cc/language-reference/en/variables/constants/inputOutputPullup/).

## Comunicación

### Diagnóstico de ejes cruzados

El sketch toma A0 y A1 por separado, descarta la primera conversión al cambiar
de canal y espera 200 microsegundos. Muestrea ambos canales aproximadamente cada
1 ms y promedia las muestras acumuladas durante cada intervalo de envío de 25 ms,
con un acumulador independiente por eje. Esto sustituye el promedio de ocho
conversiones consecutivas, que no eliminaba las oscilaciones observadas entre paquetes.
Los campos siguen siendo `C1,A0,A1,menu,disparo`: A0 solo controla W/S y A1 solo A/D.
El filtrado no puede reparar un terminal central desconectado o unido al otro canal.
Arduino documenta que una entrada desconectada puede fluctuar y depender de las otras
entradas: [referencia analogRead](https://github.com/arduino/reference-en/blob/master/Language/Functions/Analog%20IO/analogRead.adoc).

Comprobación física con Unity fuera de Play y Monitor Serie a 9600:
1. Mover únicamente A0: solo debe variar el primer número después de C1.
2. Mover únicamente A1: solo debe variar el segundo número.
3. Si varían ambos con un único mando, revisar que los terminales centrales
   no compartan la misma fila de protoboard; cada uno debe llegar a su propio pin.
4. Si uno no responde, comprobar su terminal central, 5 V y GND común.
5. Cerrar el Monitor Serie antes de iniciar Play.

Capturas de diagnóstico de esta revisión: `Logs/Arduino-hardware-diagnostic.txt`
y `Logs/Arduino-hardware-after.txt` (archivos locales ignorados por Git).
En la primera captura A0 permaneció en 556 y A1 varió entre 494 y 583;
en la posterior A0 permaneció en 556 y A1 varió entre 478 y 581.
No hubo una maniobra física supervisada de cada potenciómetro, por lo que estas
capturas no demuestran qué cable o componente falla ni validan el recorrido completo.

ASCII CSV con cinco campos y fin de línea LF (CRLF también se acepta):

```text
C1,vertical,horizontal,menu,disparo
C1,512,300,0,1
```

C1 identifica la versión. Los dos valores analógicos deben estar entre 0 y 1023 y los botones ser exactamente 0 o 1. No se aceptan otros números de campos, prefijos, texto ni líneas parciales. El antiguo protocolo del vehículo de cinco números se rechaza deliberadamente: cargar el sketch actualizado.

- **9600 baudios**, 8 bits, sin paridad, 1 bit de parada; ambos extremos deben coincidir.
- Un paquete cada **25 ms (40 Hz)**. Se evitan delays en el sketch.
- Antirrebote de 25 ms en el sketch y filtrado adicional configurable en Unity.
- Unity lee en un hilo de fondo, con timeout de lectura de 100 ms y líneas de longitud limitada.
- Se publica cada paquete completo e inmutable en una cola acotada. El hilo principal consume los paquetes y ejecuta las acciones de Unity.
- Con 0,5 s sin datos válidos, deja de usar Arduino. El worker reintenta la conexión; al abrir COM permite hasta 3 s para el reinicio de la placa.
- Paquetes viejos se descartan. Si se llena la cola, se empieza una nueva sesión para no reproducir pulsaciones atrasadas.
- Una sesión nueva toma referencias iniciales sin mover el cañón ni activar botones mantenidos. Estos deben soltarse antes de volver a actuar.
- LastError, InvalidLines, IsConnected y ConnectedPort permiten depurar. Solo se registran cambios de conexión, sin inundar la consola.

## Coexistencia de controles

No hay selector manual de dispositivo y el Input System permanece activo.

| Acción | Teclado/control existente | Arduino |
|---|---|---|
| Inclinación | W/S, stick | Potenciómetro A0 |
| Rotación horizontal | A/D, stick | Potenciómetro A1 |
| Disparo | Space / acción primaria original | D3, nueva pulsación |
| Abrir/cerrar menú en partida | Esc / Start | D2, nueva pulsación |
| Navegar por UI | Flechas/WASD según el módulo UI, ratón/control | Potenciómetro vertical |
| Confirmar UI | Enter / Submit existente, clic | D3 |
| Ajustar slider seleccionado | Controles UI existentes | Potenciómetro horizontal |

El movimiento se arbitra **independientemente por eje**. Un giro significativo del potenciómetro toma su eje; una pulsación posterior del teclado vuelve a tomarlo. Si ambos cambian en el mismo frame, gana el teclado. Una tecla mantenida no genera actividad nueva cada frame: para retomar el control tras un giro posterior, soltar y pulsar de nuevo.

Los potenciómetros funcionan como W/S y A/D: **al centro se detienen; descentrados ordenan movimiento continuo en uno u otro sentido**, incluso si la lectura permanece fija. Cuanto mayor sea la desviación del centro, mayor será la velocidad, hasta Aim Speed o Turn Speed. Al volver al centro se conserva el ángulo alcanzado. No se convierte la posición del mando en un ángulo absoluto.

Una mediana de tres muestras rechaza picos; una histéresis de 4 unidades evita vibración. Además, cada eje necesita tres paquetes consecutivos fuera del centro en el mismo sentido antes de activar movimiento (configurable con Direction Confirmation Samples). Un cambio acumulado de 12 unidades confirmado determina actividad significativa. Una señal que alterna de sentido no reclama el control al teclado. La zona muerta central predeterminada es del 10 % por lado. Conectar no reclama el control; las fluctuaciones dentro del centro tampoco impiden usar el teclado. SmoothDamp suaviza la orden de movimiento; una lectura en el centro la corta inmediatamente, sin esperar la confirmación.

El menú usa el mismo EventSystem para ambos dispositivos. El potenciómetro vertical cambia la selección solo cuando se mueve significativamente; así una posición fija no impide navegar con teclado. D3 se convierte en Submit y nunca se dirige a Fire mientras hay un menú abierto. También se bloquea el frame de transición para evitar disparar al confirmar una opción que cierre el menú. Con ambos botones simultáneos tiene prioridad el menú.

## Inspector

En **Assets/Prefabs/Arduino.prefab**:

- ArduinoSerial: Port Name, Baud Rate, Data Timeout, Log Connection Changes.
- ArduinoController: Vertical/Horizontal, Min/Max para calibrar el ADC, Center (512 por defecto), Dead Zone (0,1), Invert, Sensitivity, Noise Threshold, Activity Threshold, Button Debounce y Navigate Menus With Potentiometers. Movement muestra la orden entre −1 y +1; 0 equivale a soltar las teclas.
- Cannon puede quedar vacío: se busca el cañón activo. Raw y Value muestran la lectura y su valor normalizado durante Play.

En el **CannonController de Level_01**:

- Aim Rotation Min/Max: ángulo X local permitido de la pieza barrel; actualmente −30° a +30°.
- Turn Rotation Min/Max: giro Y local relativo a la orientación inicial de la base; actualmente −60° a +60°.
- Aim Speed/Turn Speed: velocidades originales del teclado.
- Arduino Smooth Time: 0,15 s para suavizar la orden; Arduino Max Speed: límite adicional de 90°/s. La velocidad efectiva utiliza Aim Speed para A0 y Turn Speed para A1, igual que el teclado.
- Shot Delay y Projectile Force: las restricciones existentes de disparo se comparten entre dispositivos.

La inversión vertical predeterminada hace que un valor mayor eleve el cañón (X local negativo en este modelo). Invert permite ajustarlo al sentido del cableado. No se cambian padres, pivotes ni referencias de barrel, ruedas o punto de salida; se conservan los otros ejes locales.

## Puesta en marcha

1. Abrir Arduino/Arduino.ino en Arduino IDE. Elegir la placa y el puerto correctos, revisar pines y cargar el sketch.
2. Abrir brevemente el Monitor Serial a 9600. Comprobar líneas como C1,512,512,0,0, y cambios separados al mover cada componente.
3. **Cerrar el Monitor Serial y Serial Plotter**: Unity necesita acceso exclusivo a COM.
4. En el prefab Arduino, escribir el puerto (por ejemplo COM3) y dejar Baud Rate=9600. Vacío habilita búsqueda automática, que abre los puertos disponibles; indicar COM es más preciso.
5. Guardar el prefab y abrir MainMenu o Level_01. Iniciar Play. El objeto Arduino persiste al cambiar de escena.
6. Primero comprobar W/S, A/D, Space y Esc con Arduino desconectado.
7. Conectar Arduino, esperar la conexión y mover cada potenciómetro por separado. La conexión inicial no cambia la puntería.
8. Ajustar Min/Max mirando Raw, luego inversión y sensibilidad. Si hay ruido, subir los umbrales antes de aumentar el suavizado.
9. Pulsar D2 para abrir el menú; girar A0 para resaltar una opción y pulsar D3 para seleccionarla. D2 vuelve al juego. Probar también flechas/Enter/Esc.
10. Si se cambian COM o Baud Rate durante Play, desactivar y activar **solo ArduinoSerial** para reiniciar el worker; también se aplican al volver a entrar en Play.
11. Desconectar y reconectar USB durante la partida. El teclado debe seguir disponible y los botones mantenidos no deben activarse al reconectar.

Se compiló y cargó el sketch actualizado en Arduino Uno, COM4 (3024 bytes de programa, 224 bytes de RAM). El puerto del prefab permanece automático. Tras el cambio del muestreo temporal, una captura real de 254 paquetes dio A0=516 y A1=496–525, dentro de la zona neutra. La captura anterior al cambio daba A1=408–595. Archivo local: `Logs/Joystick-temporal-filter.txt`. Esto verifica la mejora en reposo, no demuestra por sí solo la independencia física al mover cada joystick ni repara conexiones defectuosas.

## Pruebas y alcance

Validación actualizada: **41 pruebas aprobadas, 0 fallidas** en Unity 6000.6.0f1. Incluye órdenes equivalentes a W/S y A/D, giro continuo con lectura constante, parada inmediata en el centro, zona muerta que no interfiere con el teclado, menús, cambios de escena y retroceso físico. Las pruebas automáticas de entrada inyectan paquetes; se comprobó además la recepción de datos reales de la placa por COM3.

En Unity: **Tools > Arduino > Run integration checks** (Ctrl+Shift+F8), o Test Runner, categoría CannonArduino. Guardar antes cualquier escena que se esté editando. Las pruebas cargan temporalmente Level_01, entran en Play y salen al terminar. El resultado se guarda en Logs/Arduino-test-results.xml.

Las pruebas verifican parser, rangos, ruido/picos, giros lentos, inversión, sensibilidad, arbitraje, límites y suavizado. La prueba en la escena real usa un teclado virtual del Input System y paquetes en la cola del transporte, y comprueba movimiento por eje, botones, recarga, mantener presionado, menús, confirmación con Arduino/Enter, ausencia de disparos accidentales, desconexión y nueva sesión.

La inyección de paquetes comprueba el recorrido desde la cola hasta el juego; **no sustituye una prueba de USB/puerto físico ni mide el ruido del circuito real**. El sketch se compiló y enlazó con el compilador AVR instalado y el core oficial Uno (ATmega328P): 3440 bytes de programa y 223 bytes de RAM. El firmware HEX de comprobación queda en Temp/ArduinoUnoBuild; para cargarlo normalmente se recomienda Arduino IDE y el sketch fuente.

## Archivos de esta integración

Modificados: Arduino/Arduino.ino, ArduinoSerial.cs, ArduinoController.cs, CannonController.cs, MenuManager.cs, Assets/Prefabs/Arduino.prefab, Assets/Scenes/MainMenu.unity, Assets/Scenes/Level_01.unity y ProjectSettings/EditorBuildSettings.asset.

Añadidos: Assets/Tests/Editor/CannonArduinoTests.cs (y sus metadatos Unity), esta guía y el resumen verificable de pruebas en Logs. Los cambios anteriores del proyecto y sus assets se conservan.
