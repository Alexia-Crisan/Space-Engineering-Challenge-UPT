#ifndef SENSORS_HPP
#define SENSORS_HPP

#include <deque>
#include <numeric>
#include <iostream>

#define DHT22_PIN
#define BME280_PIN

struct SensorData
{
    float temperature;
    float humidity;
    float pressure;
};

enum SensorState
{
    IDLE,
    CALIBRATE,
    CALCULATE
};

class SensorBuffer
{
    std::deque<float> buffer;
    int maxSize;

public:
    explicit SensorBuffer(int n);
    void add(float value);
    float average() const;
};

class Sensors
{
private:
    SensorBuffer tempBuffer;
    SensorBuffer humBuffer;
    SensorBuffer pressBuffer;

    SensorState state;

public:
    explicit Sensors(int bufferSize = 10);

    void sensorTask();

    SensorData getAverages() const;

    float readBME280_Temp();
    float readBME280_Hum();
    float readBME280_Press();
    float readDHT22_Temp();
    float readDHT22_Hum();
};

#endif
