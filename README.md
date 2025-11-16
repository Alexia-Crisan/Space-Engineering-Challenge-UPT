# Subsurface Exploration Rover - Space Engineering Challenge 2025

**Land Rover Edition — Subsurface Exploration Mission**  
**Team: _[Cepheus]_**

This repository contains the full hardware, software, and documentation package for our robotic rover developed for the **2025 Space Engineering Challenge by UPT**, focused on **subsurface autonomous and remote-controlled exploration** in cave-like environments.

---

## Project Overview

Our rover is designed to traverse uneven underground terrain, navigate tight passages, detect mission targets, acquire objects, and transmit real-time environmental data - all while operating without direct line-of-sight control or external computation.

---

## Mission Objectives

### **1. Navigation**

- Remote-controlled via wireless link (IEEE 802.11, 2.4 GHz)
- No direct visual contact with the rover during missions
- QR-code checkpoint detection
- HTTP request submission including:
  - Unique checkpoint value
  - Measured ambient temperature
  - Measured air pressure

### **2. Object Sampling**

- Identify, pick up, and transport a mission-specified object
- Mass ≤ 100 g
- Secure handling using onboard manipulator

### **3. Telemetry & Data Reporting**

- Live transmission of:
  - Temperature
  - Relative humidity
  - Additional mission-critical state data
- Full mission logging (start/end, telemetry, system status)

---

## Hardware Architecture

### **Mechanical Platform**

- 30 × 30 × 30 cm footprint (compliant with constraints)
- 4-kg target mass
- High-traction drivetrain for rough/gravel surfaces
- Designed to climb obstacles up to **10 cm**
- Zero-radius or tight-radius turning capability (≤ 50 cm)

### **Electronics**

- **Raspberry Pi 4**  
  Main onboard computer for sensor processing, camera handling, and mission logic.

- **Raspberry Pi NoIR Camera Module 3 (11.9 MP)**  
  High-resolution camera without IR filter for low-light environments.

- **BME280 Sensor**  
  Combined temperature, humidity, and pressure sensor with internal calibration.

- **TB6612FNG Dual Motor Driver**  
  Efficient and reversible DC motor control for the tracked chassis.

- **LED Lights for Camera**  
  Low-light illumination to improve QR detection and object visibility.

- **Voltage Sensor (Battery Monitoring)**  
  Monitors battery level to prevent over-discharge.

- **BMS 3S (Battery Protection Module)**  
  Overcharge, over-discharge, over-current protection + cell balancing.

- **Step-Down Converter LM2596**  
  Regulates battery voltage to safe operating levels for all electronics.

- **Emergency Stop Button**  
  Hardware kill-switch for safety compliance.

- **MG995 Servo Motor**  
  High-torque servo used for the object pickup mechanism.

## Software Implementation

All rover functionalities - motor control, sensor acquisition, camera processing, Wi-Fi communication, and checkpoint handling - are implemented in **C#**, using a fully asynchronous architecture based on **Tasks**. This approach allows us to run critical operations in parallel, such as video capture, real-time telemetry, sensor polling, and control logic, without blocking the main execution thread. The Task-driven design provides a robust, scalable, and easily extensible software structure, ideal for the unpredictable conditions of a subsurface exploration mission.
