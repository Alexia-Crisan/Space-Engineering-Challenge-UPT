#include <pigpio.h>
#include "sensors.hpp"
#include <thread>
#include <chrono>
#include <iostream>

int main()
{
    if (gpioInitialise() < 0)
    {
        std::cerr << "pigpio initialisation failed!" << std::endl;
        return 1;
    }

    Sensors sensors(10);

    while (true)
    {
        sensors.sensorTask();
        std::this_thread::sleep_for(std::chrono::seconds(1));
    }

    gpioTerminate();
    return 0;
}
