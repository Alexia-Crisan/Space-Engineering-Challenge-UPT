#include "sensors.hpp"
#include <cstdlib>

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

float Sensors::readBME280_Temp() {}
float Sensors::readBME280_Hum() {}
float Sensors::readBME280_Press() {}
float Sensors::readDHT22_Temp() {}
float Sensors::readDHT22_Hum() {}

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

        readBME280_Temp();
        readBME280_Hum();
        readBME280_Press();
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
        float t = (readBME280_Temp() + readDHT22_Temp()) / 2.0f;
        float h = (readBME280_Hum() + readDHT22_Hum()) / 2.0f;
        float p = readBME280_Press();

        tempBuffer.add(t);
        humBuffer.add(h);
        pressBuffer.add(p);

        std::cout << "[SENSOR] Avg Temp: " << tempBuffer.average()
                  << " | Avg Hum: " << humBuffer.average()
                  << " | Avg Press: " << pressBuffer.average() << std::endl;

        break;
    }
    }
}

SensorData Sensors::getAverages() const
{
    SensorData data;
    data.temperature = tempBuffer.average();
    data.humidity = humBuffer.average();
    data.pressure = pressBuffer.average();
    return data;
}
