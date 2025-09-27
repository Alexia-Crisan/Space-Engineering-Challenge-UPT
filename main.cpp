#include "sensors.hpp"
#include <thread>
#include <chrono>

int main()
{
    Sensors sensors(10);

    while (true)
    {
        sensors.sensorTask();
    }

    return 0;
}
