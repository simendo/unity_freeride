// MPU-6050 LED AHRS
// 
// Uses modified demo code from Patrick Lloyd's MPU-6050 LED AHRS
// and Jeff Rowberg's i2cdevlib library to visualize the yaw 
// rotation of the MPU-6050 with two WS2812 RGB LED strips.

// License Goodness
/* ============================================
Adafruit NeoPixel library.
Written by Phil Burgess / Paint Your Dragon for Adafruit Industries,
contributions by PJRC, Michael Miller and other members of the open
source community.

NeoPixel is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as
published by the Free Software Foundation, either version 3 of
the License, or (at your option) any later version.

NeoPixel is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with NeoPixel.  If not, see
<http://www.gnu.org/licenses/>.
===============================================
*/

/* ============================================
I2Cdev device library code is placed under the MIT license
Copyright (c) 2012 Jeff Rowberg
Updates available at: https://github.com/jrowberg/i2cdevlib

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
===============================================
*/

#include <avr/wdt.h>

#include <Adafruit_NeoPixel.h>  // NeoPixel library from Adafruit
#define PIXELPIN_A 6              // Arduino pin connected to strip A
#define PIXELPIN_B 5              // Arduino pin connected to strip B
#define NUMPIXELS 44           // Number of RGB LEDs per strip

#ifdef __AVR__
#include <avr/power.h>  // AVR Specific power library
#endif

// I2Cdev and MPU6050 must be installed as libraries, or else the .cpp/.h files
// for both classes must be in the include path of your project
#include "I2Cdev.h"

// MotionApps utilizes the "Digital Motion Processor" (DMP) on the MPU-6050
// to filter and fuse raw sensor data into useful quantities like quaternions,
// Euler angles, or Yaw/Pitch/Roll inertial angles
#include "MPU6050_6Axis_MotionApps20.h"

// Arduino Wire library is required if I2Cdev I2CDEV_ARDUINO_WIRE implementation
// is used in I2Cdev.h
#if I2CDEV_IMPLEMENTATION == I2CDEV_ARDUINO_WIRE
#include "Wire.h"
#endif

#include <SPI.h> // Library for writing to SD card 
#include <SD.h>

MPU6050 mpu;

/* =========================================================================
   NOTE: In addition to connection 5.0v, GND, SDA, and SCL, this sketch
   depends on the MPU-6050's INT pin being connected to the Arduino's
   external interrupt #0 pin. On the Arduino Uno and Mega 2560, this is
   digital I/O pin 2.
 * ========================================================================= */

#define OUTPUT_READABLE_YAWPITCHROLL

// MPU control/status vars
bool dmpReady = false;   // set true if DMP init was successful
uint8_t mpuIntStatus;    // holds actual interrupt status byte from MPU
uint8_t devStatus;       // return status after each device operation (0 = success, !0 = error)
uint16_t packetSize;     // expected DMP packet size (default is 42 bytes)
uint16_t fifoCount;      // count of all bytes currently in FIFO
int16_t ax, ay, az;      // acceleration x, y, z
uint8_t fifoBuffer[64];  // FIFO storage buffer

// orientation/motion vars
Quaternion q;         // [w, x, y, z]         quaternion container
VectorInt16 aa;       // [x, y, z]            accel sensor measurements
VectorInt16 aaReal;   // [x, y, z]            gravity-free accel sensor measurements
VectorInt16 aaWorld;  // [x, y, z]            world-frame accel sensor measurements
VectorFloat gravity;  // [x, y, z]            gravity vector
float euler[3];       // [psi, theta, phi]    Euler angle container
float ypr[3];         // [yaw, pitch, roll]   yaw/pitch/roll container and gravity vector
float initialYaw = 0.0;
int initialIndex = NUMPIXELS;
//int dataCounter; //counter for reducing number of writes to SD card


// ================================================================
// ===                NEOPIXEL AHRS ROUTINE                     ===
// ================================================================

// When we setup the NeoPixel library, we tell it how many pixels, and which pin to use to send signals.
// Note that for older NeoPixel strips you might need to change the third parameter--see the strandtest
// example in the lbrary folder for more information on possible values.
Adafruit_NeoPixel pixelsA = Adafruit_NeoPixel(NUMPIXELS, PIXELPIN_A, NEO_GRB + NEO_KHZ800);
Adafruit_NeoPixel pixelsB = Adafruit_NeoPixel(NUMPIXELS, PIXELPIN_B, NEO_GRB + NEO_KHZ800);


void update_led_ahrs(float yaw, float pitch, float roll) {
  // Note: the YPR values are in DEGREES! not radians

  // Clean slate.
  pixelsA.clear();
  pixelsB.clear();

  float relativeYaw = yaw - initialYaw;
  if (relativeYaw < 0) {
        relativeYaw += 360.0;
  }
  float adjustedYaw = fmod(relativeYaw + 360, 360);

  //int initialIndex = int(NUMPIXELS * relativeYaw / 360.0);

  int yawIndex = int(NUMPIXELS * yaw / 360.0);
  int relativeYawIndex = int( 2 * NUMPIXELS * relativeYaw / 360.0);
  relativeYawIndex -= NUMPIXELS;
  if (relativeYawIndex < 0) {
      relativeYawIndex += 2 * NUMPIXELS; 
  }
  

  //not used
  float rollBrightness = 255 * roll / 180.0;
  float pitchBrightness = 255 * pitch / 180.0;
  float yawBrightness = 255 * yaw / 180.0;

  int i;

  int totalSteps = 2;  //Adjust for smoother or faster transitions

  ///////////Initial color mapping 
  /*
  float redLevel;
  float greenLevel; 
  float blueLevel;
  
  for (i = 0; i < (NUMPIXELS); i++) {
    if (yaw >= 0) {
      redLevel = 0;
      if (yaw >= 90){
        greenLevel = 255 - 2 * (255 * (180-yaw) / 180.0);
        blueLevel = 2 * (255 * (180-yaw) / 180.0);
      } else{
        greenLevel = 255 - 2 * (255 * yaw / 180.0);
        blueLevel = 2 * (255 * yaw / 180.0);
      }
      //pixelsA.setPixelColor((yaw_index - (NUMPIXELS / 8) + i) % NUMPIXELS, pixelsA.Color(red_level, green_level, blue_level));
      //pixelsB.setPixelColor((yaw_index + (3 * NUMPIXELS / 8) + i) % NUMPIXELS, pixelsB.Color(0, 255 - 2 * yaw_brightness, yaw_brightness));
    } else {
      blueLevel = 0;
      if (yaw < -90){
        redLevel = 2 * (255 * (180+yaw) / 180.0);
        greenLevel = 255 - 2 * (255 * (180+yaw) / 180.0);
      }
      else{
        redLevel = 0 - 2 * (255 * yaw / 180.0);
        greenLevel = 255 + 2 * (255 * yaw / 180.0);
      }
      //pixelsA.setPixelColor((yaw_index - (NUMPIXELS / 8) + i) % NUMPIXELS, pixels.Color(red_level, green_level, blue_level));
      //pixelsB.setPixelColor((yaw_index + (3 * NUMPIXELS / 8) + i) % NUMPIXELS, pixels.Color(-1 * yaw_brightness, 255 + 2 * yaw_brightness, 0));
    }

    */

    //Updated color mapping
    for (int i = 0; i < NUMPIXELS; i++) {
      // Calculate color based on adjustedYaw
      int redLevel = 0;
      int greenLevel = 0;
      int blueLevel = 0;

      // Transition logic, adjusted for starting with green
    if (adjustedYaw < 30) { // Green to yellow transition
        greenLevel = 255;
        redLevel = map(adjustedYaw, 0, 30, 0, 255);
    } else if (adjustedYaw < 60) { // Yellow to red transition
        redLevel = 255;
        greenLevel = map(adjustedYaw, 30, 60, 255, 0);
    } else if (adjustedYaw < 90) { // Red to magenta transition
        redLevel = 255;
        blueLevel = map(adjustedYaw, 60, 90, 0, 255);
    } else if (adjustedYaw < 120) { // Magenta to blue transition
        redLevel = map(adjustedYaw, 90, 120, 255, 0);
        blueLevel = 255;
    } else if (adjustedYaw < 150) { // Blue to cyan transition
        blueLevel = 255;
        greenLevel = map(adjustedYaw, 120, 150, 0, 255);
    } else if (adjustedYaw < 180) { // Cyan back to green transition (completing first half)
        greenLevel = 255;
        blueLevel = map(adjustedYaw, 150, 180, 255, 0);
    } else if (adjustedYaw < 210) { // Green to yellow transition (opposite direction)
        greenLevel = 255;
        redLevel = map(adjustedYaw, 180, 210, 0, 255);
    } else if (adjustedYaw < 240) { // Yellow to red transition (opposite direction)
        redLevel = 255;
        greenLevel = map(adjustedYaw, 210, 240, 255, 0);
    } else if (adjustedYaw < 270) { // Red to magenta transition (opposite direction)
        redLevel = 255;
        blueLevel = map(adjustedYaw, 240, 270, 0, 255);
    } else if (adjustedYaw < 300) { // Magenta to blue transition (opposite direction)
        redLevel = map(adjustedYaw, 270, 300, 255, 0);
        blueLevel = 255;
    } else if (adjustedYaw < 330) { // Blue to cyan transition (opposite direction)
        blueLevel = 255;
        greenLevel = map(adjustedYaw, 300, 330, 0, 255);
    } else { // Cyan back to green transition (completing full cycle in opposite direction)
        greenLevel = 255;
        blueLevel = map(adjustedYaw, 330, 360, 255, 0);
    }

    //int distance = min(abs(relative_yaw_index - i), 2 * NUMPIXELS - abs(relative_yaw_index - i)); 
    int distanceA = min(abs(NUMPIXELS - relativeYawIndex - i), 2 * NUMPIXELS);
    int brightnessA = calculateBrightness(distanceA);
    int adjustedRedA = redLevel * brightnessA / 255;
    int adjustedGreenA = greenLevel * brightnessA / 255;
    int adjustedBlueA = blueLevel * brightnessA / 255;

    int distanceB = min(abs(NUMPIXELS - relativeYawIndex + i), 2 * NUMPIXELS);
    int brightnessB = calculateBrightness(distanceB);
    int adjustedRedB = redLevel * brightnessB / 255;
    int adjustedGreenB = greenLevel * brightnessB / 255;
    int adjustedBlueB = blueLevel * brightnessB / 255;

    //With fading effect
    //pixelsA.setPixelColor((initialIndex + i) % (NUMPIXELS), pixelsA.Color(adjustedRedA, adjustedGreenA, adjustedBlueA));
    //pixelsB.setPixelColor((initialIndex + i) % NUMPIXELS, pixelsB.Color(adjustedRedB, adjustedGreenB, adjustedBlueB));

    //All White - used when measuring the amount of current drawn by the strips
    pixelsA.setPixelColor((initialIndex + i) % (NUMPIXELS), pixelsA.Color(255,255,255));
    pixelsB.setPixelColor((initialIndex + i) % NUMPIXELS, pixelsB.Color(255,255,255));
  
    //Without fading effect
    //pixelsA.setPixelColor((initialIndex + i) % (NUMPIXELS), pixelsA.Color(redLevel, greenLevel, blueLevel));
    //pixelsB.setPixelColor((initialIndex + i) % NUMPIXELS, pixelsB.Color(redLevel, greenLevel, blueLevel));
    }

  /* //White indicator used in test and development
  if (relativeYawIndex >= 0 && relativeYawIndex < NUMPIXELS) {
      pixelsA.setPixelColor(NUMPIXELS - relativeYawIndex, pixelsA.Color(255, 255, 255));  // White as can be
  } else if (relativeYawIndex >= NUMPIXELS && relativeYawIndex < 2 * NUMPIXELS) {
      pixelsB.setPixelColor(relativeYawIndex - NUMPIXELS, pixelsB.Color(255, 255, 255));  // White as can be
  }
  */
  pixelsA.show();
  pixelsB.show();
}
    
    

// ================================================================
// ===               INTERRUPT DETECTION ROUTINE                ===
// ================================================================

// indicates whether MPU interrupt pin has gone high
volatile bool mpuInterrupt = false;
void dmpDataReady() {
  mpuInterrupt = true;
}


// ================================================================
// ===                      INITIAL SETUP                       ===
// ================================================================

void setup() {
  // One watchdog timer will reset the device if it is unresponsive
  // for a second or more
  wdt_enable(WDTO_1S);

// join I2C bus (I2Cdev library doesn't do this automatically)
#if I2CDEV_IMPLEMENTATION == I2CDEV_ARDUINO_WIRE
  Wire.begin();
  TWBR = 24;  // 400kHz I2C clock (200kHz if CPU is 8MHz)
#elif I2CDEV_IMPLEMENTATION == I2CDEV_BUILTIN_FASTWIRE
  Fastwire::setup(400, true);
#endif

  // Start the NeoPixel device and turn all the LEDs off
  pixelsA.begin();
  pixelsB.begin();
  pixelsA.show();  // Initialize all pixels to 'off'
  pixelsB.show();  // Initialize all pixels to 'off'

  // initialize serial communication
  Serial.begin(38400);
  while (!Serial)
    ;  // wait for Leonardo enumeration, others continue immediately

  
  // initialize device
  Serial.println(F("Initializing I2C devices..."));
  mpu.initialize();

  // verify connection
  Serial.println(F("Testing device connections..."));
  Serial.println(mpu.testConnection() ? F("MPU6050 connection successful") : F("MPU6050 connection failed"));

  //mpu.setDLPFMode(MPU6050_DLPF_BW_20);

  // Set the sample rate divider for 100Hz DMP updates
  //mpu.setRate(19); // (1000Hz / (1 + 9) = 100Hz) 


  // load and configure the DMP
  Serial.println(F("Initializing DMP..."));
  devStatus = mpu.dmpInitialize();
  

  // gyro offsets
  mpu.setXGyroOffset(118);  //37 (220)
  mpu.setYGyroOffset(-38);   //47 (76)
  mpu.setZGyroOffset(-7);  //42 (-85)
  mpu.setXAccelOffset(-5249); //(-740)
  mpu.setYAccelOffset(-2608); //(-2222)
  mpu.setZAccelOffset(1307);  //1780 (3083)

  // make sure it worked (returns 0 if so)
  if (devStatus == 0) {
    // turn on the DMP, now that it's ready
    Serial.println(F("Enabling DMP..."));
    mpu.setDMPEnabled(true);

    // enable Arduino interrupt detection
    Serial.println(F("Enabling interrupt detection (Arduino external interrupt 0)..."));
    attachInterrupt(0, dmpDataReady, RISING);
    mpuIntStatus = mpu.getIntStatus();

    // set our DMP Ready flag so the main loop() function knows it's okay to use it
    Serial.println(F("DMP ready! Waiting for first interrupt..."));
    dmpReady = true;

    // get expected DMP packet size for later comparison
    packetSize = mpu.dmpGetFIFOPacketSize();
  } else {
    // ERROR!
    // 1 = initial memory load failed
    // 2 = DMP configuration updates failed
    // (if it's going to break, usually the code will be 1)
    Serial.print(F("DMP Initialization failed (code "));
    Serial.print(devStatus);
    Serial.println(F(")"));
  }

  delay(400); //give some time to settle
  float yawDeg = ypr[0] * 180 / M_PI;
  initialYaw = yawDeg;

  /*
  delay(100); 
  File dataFile;
  dataFile = SD.open("newData.txt", FILE_WRITE);
  */
  //delay(1000);
  /*
  
  if(!SD.begin(4)){
    Serial.println("SD card not initialized!");
  }

  else{
  dataFile = SD.open("newData.txt", FILE_WRITE);
  dataCounter = 0;
  dataFile.print("Initial Yaw:\t");
  dataFile.print(initialYaw);
  dataFile.close();
  }
  delay(1000); 
  */
}


// ================================================================
// ===                    MAIN PROGRAM LOOP                     ===
// ================================================================

void loop() {
  // the program is alive...for now.
  unsigned long startTime = millis();
  wdt_reset();

  // if programming failed, don't try to do anything
  if (!dmpReady) return;

  // wait for MPU interrupt or extra packet(s) available
  while (!mpuInterrupt && fifoCount < packetSize) {
  }

  // reset interrupt flag and get INT_STATUS byte
  mpuInterrupt = false;
  mpuIntStatus = mpu.getIntStatus();

  // get current FIFO count
  fifoCount = mpu.getFIFOCount();

  // check for overflow (this should never happen unless our code is too inefficient)
  if ((mpuIntStatus & 0x10) || fifoCount == 1024) {
    // reset so we can continue cleanly
    mpu.resetFIFO();
    Serial.println(F("FIFO overflow!"));

    // otherwise, check for DMP data ready interrupt (this should happen frequently)
  } else if (mpuIntStatus & 0x02) {
    // wait for correct available data length, should be a VERY short wait
    while (fifoCount < packetSize) fifoCount = mpu.getFIFOCount();

    // read a packet from FIFO
    mpu.getFIFOBytes(fifoBuffer, packetSize);

    // track FIFO count here in case there is > 1 packet available
    // (this lets us immediately read more without waiting for an interrupt)
    fifoCount -= packetSize;

    // display Euler angles in degrees
    mpu.dmpGetQuaternion(&q, fifoBuffer);
    mpu.dmpGetGravity(&gravity, &q);
    mpu.dmpGetYawPitchRoll(ypr, &q, &gravity);
    mpu.getAcceleration(&ax, &ay, &az);
    float yawDeg = ypr[0] * 180 / M_PI;
    float pitchDeg = ypr[1] * 180 / M_PI;
    float rollDeg = ypr[2] * 180 / M_PI;

    // make pretty colors happen
    update_led_ahrs(yawDeg, pitchDeg, rollDeg);


    /*
    // Write ypr to SD card 
    //if(dataCounter == 50){
    File dataFile = SD.open("loopData", FILE_WRITE);
    if (dataFile){
    //dataFile.print("ypr:\t");
    dataFile.println(yawDeg);
    //dataFile.print("\t");
    //dataFile.print(pitchDeg);
    //dataFile.print("\t");
    //dataFile.println(rollDeg);
    Serial.print("Writing to SD");
    }
    //else{
    //  Serial.print("NO SD!");
    //}
    dataFile.close(); 
    dataCounter = 0;
    //}
    //else{
    //  dataCounter += 1;
    //}
    */
    // print some descriptive YRP data

    /*
    Serial.print("ypr:\t");
    Serial.print(yaw_deg);
    Serial.print("\t");
    Serial.print(pitch_deg);
    Serial.print("\t");
    Serial.println(roll_deg);
    */



    /*
    int16_t ax, ay, az;
    mpu.getAcceleration(&ax, &ay, &az);

    Serial.print("ax, ay, az:\t");
    Serial.print(ax);
    Serial.print("\t");
    Serial.print(ay);
    Serial.print("\t");
    Serial.println(az);
    */

    /*
    uint16_t fifoCount = mpu.getFIFOCount();
    if (fifoCount >= 1024) {
      // Buffer is close to overflowing
      mpu.resetFIFO();
      Serial.println("FIFO reset");
    }
    */
  }
  // Record the end time
  unsigned long endTime = millis();

  // Calculate and print the time taken for the loop
  Serial.print("Time taken for the loop: ");
  Serial.print(endTime - startTime);
  Serial.println(" ms");

}

//Not used as of now, but can be utilized to dynamically change the brightness based on acceleration
float calculateTotalAcceleration(int16_t ax, int16_t ay, int16_t az) {
  // Convert accelerometer values to g's
  float axG = ax / 16384.0;  
  float ayG = ay / 16384.0;
  float azG = az / 16384.0;

  // Calculate the total acceleration magnitude
  float totalAcceleration = sqrt(axG * axG + ayG * ayG + azG * azG);

  return totalAcceleration;  // Total acceleration in g's
}


int calculateBrightness(int distance) {
    
    float brightnessFactor = 5;
    int brightness = 255 - (int)(brightnessFactor * distance);
    return max(0, min(255, brightness));
}


