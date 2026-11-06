// ENT-Game: KY-023 #1 VRy -> A0; KY-023 #2 VRx -> A1; protocolo C1.
// Cambiar los pines aquí; deben coincidir con el cableado, no con números en Unity.
const uint8_t PIN_VERTICAL = A0;
const uint8_t PIN_HORIZONTAL = A1;
const uint8_t PIN_MENU = 2;
const uint8_t PIN_FIRE = 3;
const unsigned long BAUD_RATE = 9600;
const unsigned long SEND_INTERVAL_MS = 25; // 40 Hz; cabe incluso a 9600 baudios.
const unsigned long DEBOUNCE_MS = 25;
const unsigned long SAMPLE_INTERVAL_US = 1000;
const unsigned int ADC_SETTLE_US = 200;
static_assert(PIN_VERTICAL != PIN_HORIZONTAL, "Cada potenciometro necesita un pin distinto");

struct Button {
  uint8_t pin;
  bool candidate;
  bool pressed;
  unsigned long changedAt;
};
void updateButton(Button &button, unsigned long now);

int readJoystickAxis(uint8_t pin) {
  // El ADC es compartido. Descartar la primera conversion al cambiar de canal
  // y dejar que se estabilice antes de medir el nuevo potenciometro.
  analogRead(pin);
  delayMicroseconds(ADC_SETTLE_US);
  return analogRead(pin);
}

Button menuButton = {PIN_MENU, false, false, 0};
Button fireButton = {PIN_FIRE, false, false, 0};
unsigned long lastSent = 0;
unsigned long lastSample = 0;
unsigned long verticalSum = 0, horizontalSum = 0;
uint16_t sampleCount = 0;

void updateButton(Button &button, unsigned long now) {
  bool reading = digitalRead(button.pin) == LOW;
  if (reading != button.candidate) {
    button.candidate = reading;
    button.changedAt = now;
  }
  if (now - button.changedAt >= DEBOUNCE_MS)
    button.pressed = button.candidate;
}

void setup() {
  pinMode(PIN_VERTICAL, INPUT);
  pinMode(PIN_HORIZONTAL, INPUT);
  pinMode(PIN_MENU, INPUT_PULLUP);
  pinMode(PIN_FIRE, INPUT_PULLUP);
  // Un botón mantenido al encender no se interpreta como una nueva pulsación.
  menuButton.candidate = menuButton.pressed = digitalRead(PIN_MENU) == LOW;
  fireButton.candidate = fireButton.pressed = digitalRead(PIN_FIRE) == LOW;
  Serial.begin(BAUD_RATE);
}

void loop() {
  unsigned long now = millis();
  updateButton(menuButton, now);
  updateButton(fireButton, now);
  unsigned long sampleTime = micros();
  if (sampleTime - lastSample >= SAMPLE_INTERVAL_US) {
    lastSample = sampleTime;
    // Promediar en el tiempo, no ocho conversiones casi simultáneas.
    // Cada canal mantiene su acumulador: nunca se mezclan los dos ejes.
    verticalSum += readJoystickAxis(PIN_VERTICAL);
    horizontalSum += readJoystickAxis(PIN_HORIZONTAL);
    ++sampleCount;
  }
  if (now - lastSent < SEND_INTERVAL_MS || sampleCount == 0) return;
  lastSent = now;
  // Capturar cada canal por separado antes de transmitir el paquete completo.
  const int vertical = (verticalSum + sampleCount / 2) / sampleCount;
  const int horizontal = (horizontalSum + sampleCount / 2) / sampleCount;
  verticalSum = horizontalSum = 0;
  sampleCount = 0;
  // CSV ASCII con prefijo de versión y fin de línea: C1,vertical,horizontal,menu,fire
  Serial.print("C1,");
  Serial.print(vertical);
  Serial.print(',');
  Serial.print(horizontal);
  Serial.print(',');
  Serial.print(menuButton.pressed ? 1 : 0);
  Serial.print(',');
  Serial.println(fireButton.pressed ? 1 : 0);
}
