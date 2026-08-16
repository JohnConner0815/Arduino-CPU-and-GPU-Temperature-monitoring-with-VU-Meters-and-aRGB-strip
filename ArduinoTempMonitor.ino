#include <FastLED.h>

// --- PIN DEFINITIONS ---
#define CPU_METER_PIN 9
#define GPU_METER_PIN 6
#define LED_DATA_PIN 5

// --- LED STRIP SETTINGS ---
#define NUM_LEDS 9
#define LED_TYPE WS2812B
#define COLOR_ORDER GRB
CRGB leds[NUM_LEDS];

// --- VARIABLES ---
String inputString = "";      
bool stringComplete = false;  

// Blink state variables
unsigned long previousBlinkTime = 0;  
bool blinkState = false;              

void setup() {
  // Initialize Serial communication to match the PC tool (19200 baud)
  Serial.begin(19200);
  
  // Initialize PWM pins for VU meters
  pinMode(CPU_METER_PIN, OUTPUT);
  pinMode(GPU_METER_PIN, OUTPUT);

  // Initialize FastLED for the WS2812B strip
  FastLED.addLeds<LED_TYPE, LED_DATA_PIN, COLOR_ORDER>(leds, NUM_LEDS).setCorrection(TypicalLEDStrip);
  
  // Set global brightness (0-255 scale)
  FastLED.setBrightness(57);
  
  // Set initial states
  analogWrite(CPU_METER_PIN, 0);
  analogWrite(GPU_METER_PIN, 0);
  updateLEDs(0, 0);
}

void loop() {
  // Read incoming serial data from the PC
  while (Serial.available()) {
    char inChar = (char)Serial.read();
    
    if (inChar == '\n') {
      stringComplete = true;
    } else {
      inputString += inChar;
    }
  }

  // When a full line is received (e.g., "65,72\n")
  if (stringComplete) {
    inputString.trim(); // Remove any hidden \r characters
    
    // Find the comma separating CPU and GPU
    int commaIndex = inputString.indexOf(',');
    
    if (commaIndex > 0) {
      // Extract numbers and convert to integers
      int cpuTemp = inputString.substring(0, commaIndex).toInt();
      int gpuTemp = inputString.substring(commaIndex + 1).toInt();
      
      // Safety clamp: Ensure temps stay strictly within 0-100 range
      cpuTemp = constrain(cpuTemp, 0, 100);
      gpuTemp = constrain(gpuTemp, 0, 100);
      
      // Update the hardware
      updateMeters(cpuTemp, gpuTemp);
      updateLEDs(cpuTemp, gpuTemp);
    }
    
    // Clear for next reading
    inputString = "";
    stringComplete = false;
  }
}

void updateMeters(int cpuTemp, int gpuTemp) {
  // Map 0-100°C to 0-242 PWM (0-95%) for CPU calibration
  analogWrite(CPU_METER_PIN, map(cpuTemp, 0, 100, 0, 242));
  
  // Map 0-100°C to 0-229 PWM (0-90%) for GPU calibration
  analogWrite(GPU_METER_PIN, map(gpuTemp, 0, 100, 0, 229));
}

void updateLEDs(int cpuTemp, int gpuTemp) {
  bool cpuBlink = (cpuTemp >= 96);
  bool gpuBlink = (gpuTemp >= 81);
  
  // If either temperature hits the critical threshold, ALL LEDs blink red
  if (cpuBlink || gpuBlink) {
    if (millis() - previousBlinkTime >= 300) {
      blinkState = !blinkState; // Toggle state
      previousBlinkTime = millis();
    }
    
    if (blinkState) {
      // All LEDs RED (except LED 5)
      fill_solid(leds, 4, CRGB::Red);   // GPU LEDs 1-4
      leds[4] = CRGB::Black;            // LED 5 OFF
      fill_solid(&leds[5], 4, CRGB::Red); // CPU LEDs 6-9
    } else {
      // All LEDs OFF
      fill_solid(leds, NUM_LEDS, CRGB::Black);
    }
  } 
  else {
    // Normal operation: Get colors based on profiles
    CRGB cpuColor = getCpuColor(cpuTemp);
    CRGB gpuColor = getGpuColor(gpuTemp);

    // LEDs 1-4 (Indices 0-3): GPU Color
    fill_solid(leds, 4, gpuColor);

    // LED 5 (Index 4): Always OFF
    leds[4] = CRGB::Black;

    // LEDs 6-9 (Indices 5-8): CPU Color
    fill_solid(&leds[5], 4, cpuColor);
  }

  FastLED.show();
}

// ==============================================================================
// CPU COLOR PROFILE
// ==============================================================================
CRGB getCpuColor(int temp) {
  if (temp <= 30) return CRGB::Cyan;
  if (temp <= 35) return blend(CRGB::Cyan, CRGB::Green, map(temp, 30, 35, 0, 255));
  if (temp <= 65) return CRGB::Green; // Extended to 65 to bridge your gap seamlessly
  if (temp <= 70) return blend(CRGB::Green, CRGB::Yellow, map(temp, 65, 70, 0, 255));
  if (temp <= 75) return CRGB::Yellow;
  if (temp <= 80) return blend(CRGB::Yellow, CRGB::Orange, map(temp, 75, 80, 0, 255));
  if (temp <= 85) return CRGB::Orange;
  if (temp <= 90) return blend(CRGB::Orange, CRGB::Red, map(temp, 85, 90, 0, 255));
  return CRGB::Red; // 91 - 95
}

// ==============================================================================
// GPU COLOR PROFILE
// ==============================================================================
CRGB getGpuColor(int temp) {
  if (temp <= 30) return CRGB::Cyan;
  if (temp <= 35) return blend(CRGB::Cyan, CRGB::Green, map(temp, 30, 35, 0, 255));
  if (temp <= 50) return CRGB::Green;
  if (temp <= 55) return blend(CRGB::Green, CRGB::Yellow, map(temp, 50, 55, 0, 255));
  if (temp <= 60) return CRGB::Yellow;
  if (temp <= 65) return blend(CRGB::Yellow, CRGB::Orange, map(temp, 60, 65, 0, 255));
  if (temp <= 70) return CRGB::Orange;
  if (temp <= 75) return blend(CRGB::Orange, CRGB::Red, map(temp, 70, 75, 0, 255));
  return CRGB::Red; // 76 - 80
}
