#include "sensors.hpp"
#include <cstdlib>
#include <pigpio.h>
#include <chrono>
#include <thread>
#include <iostream>
#include <vector>

// ================= SensorBuffer =================
SensorBuffer::SensorBuffer(int n) : maxSize(n) {}

void SensorBuffer::add(float value)
{
    if (buffer.size() >= maxSize)
        buffer.pop_front();

    buffer.push_back(value);
}

float SensorBuffer::average() const
{
    if (buffer.empty())
        return 0.0f;

    float sum = std::accumulate(buffer.begin(), buffer.end(), 0.0f);
    return sum / buffer.size();
}

// ================= Sensors =================
Sensors::Sensors(int bufferSize)
    : tempBuffer(bufferSize),
      humBuffer(bufferSize),
      pressBuffer(bufferSize),
      state(IDLE) {}

// float Sensors::readBME280_Temp() {}
// float Sensors::readBME280_Hum() {}
// float Sensors::readBME280_Press() {}

bool readDHT22(float &temperature, float &humidity)
{
    int data[5] = {0, 0, 0, 0, 0};
    int bitidx = 0;
    int laststate = 1;
    int counter = 0;

    gpioSetMode(DHT22_PIN, PI_OUTPUT);
    gpioWrite(DHT22_PIN, 0);
    gpioDelay(1000);
    gpioWrite(DHT22_PIN, 1);
    gpioSetMode(DHT22_PIN, PI_INPUT);

    // std::cout << "[DEBUG] Reading DHT22 pulses..." << std::endl;

    for (int i = 0; i < 84; i++)
    {
        counter = 0;
        while (gpioRead(DHT22_PIN) == laststate)
        {
            counter++;
            gpioDelay(1);
            if (counter == 255)
                break;
        }

        // std::cout << "[DEBUG] Pulse " << i << ": " << counter << " ticks" << std::endl;

        laststate = gpioRead(DHT22_PIN);

        if ((i >= 4) && (i % 2 == 0))
        {
            data[bitidx / 8] <<= 1;
            if (counter > 16)
                data[bitidx / 8] |= 1;
            bitidx++;
        }
    }

    // std::cout << "[DEBUG] Bits read: ";
    // for (int i = 0; i < 5; i++)
    //     std::cout << data[i] << " ";
    // std::cout << std::endl;

    if (bitidx >= 40)
    {
        int checksum = data[0] + data[1] + data[2] + data[3];
        // std::cout << "[DEBUG] Checksum calc: " << checksum << ", received: " << data[4] << std::endl;

        if ((checksum & 0xFF) != data[4])
        {
            // std::cout << "[DEBUG] Checksum mismatch!" << std::endl;
            return false;
        }

        humidity = ((data[0] << 8) + data[1]) * 0.1f;
        temperature = ((data[2] << 8) + data[3]) * 0.1f;

        // std::cout << "[DEBUG] Temp: " << temperature << ", Hum: " << humidity << std::endl;

        return true;
    }

    // std::cout << "[DEBUG] Not enough bits read!" << std::endl;
    return false;
}

// ================= Sensors DHT22 Functions =================

float Sensors::readDHT22_Temp()
{
    float t = 0, h = 0;
    if (readDHT22(t, h))
        return t;
    return tempBuffer.average();
}

float Sensors::readDHT22_Hum()
{
    float t = 0, h = 0;
    if (readDHT22(t, h))
        return h;
    return humBuffer.average();
}

SensorData Sensors::getAverages() const
{
    SensorData data;
    data.temperature = tempBuffer.average();
    data.humidity = humBuffer.average();
    data.pressure = pressBuffer.average();
    return data;
}

// ================= FSM TASK =================
void Sensors::sensorTask()
{
    switch (state)
    {
    case IDLE:
        std::cout << "[SENSOR] State: IDLE" << std::endl;
        state = CALIBRATE;
        break;

    case CALIBRATE:
    {
        static int calibCounter = 0;
        int calibTarget = 10;
        std::cout << "[SENSOR] Calibrating..." << std::endl;

        // readBME280_Temp();
        // readBME280_Hum();
        // readBME280_Press();
        readDHT22_Temp();
        readDHT22_Hum();

        calibCounter++;
        if (calibCounter >= calibTarget)
        {
            state = CALCULATE;
            std::cout << "[SENSOR] Calibration done." << std::endl;
        }

        break;
    }

    case CALCULATE:
    {
        // float t = (readBME280_Temp() + readDHT22_Temp()) / 2.0f;
        // float h = (readBME280_Hum() + readDHT22_Hum()) / 2.0f;
        // float p = readBME280_Press();

        float t = readDHT22_Temp();
        float h = readDHT22_Hum();

        tempBuffer.add(t);
        humBuffer.add(h);
        // pressBuffer.add(p);

        std::cout << "[SENSOR] Avg Temp: " << tempBuffer.average()
                  << " | Avg Hum: " << humBuffer.average()
                  << " | Avg Press: " << pressBuffer.average() << std::endl;

        break;
    }
    }
}
