# Arduino-CPU-and-GPU-Temperature-monitoring-with-VU-Meters-and-aRGB-strip

You need all 3 system files, as well as LibreHardwareMonitorLib.dll and ArduinoTempMonitor.exe, in the same folder for it to work.
The display is relatively basic.
The LED strip is used for both displays, with LEDs 1–4 showing the GPU temperature and LEDs 6–9 showing the CPU temperature. LED No. 5 remains off at all times in this configuration.
The color of the LEDs changes depending on the temperature and can be modified via the sketch file.
If the VU meter needle flickers due to the PWM signal, a simple low-pass filter with a capacitor can help. In my case, I didn't need one because the PWM frequency is high enough and the needle is too slow to respond.
![](https://github.com/JohnConner0815/Arduino-CPU-and-GPU-Temperature-monitoring-with-VU-Meters-and-aRGB-strip/blob/main/ArduinoTempMonitor.jpg)
