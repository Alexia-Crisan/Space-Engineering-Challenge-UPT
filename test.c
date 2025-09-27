#include <iostream>
#include <chrono>
#include <thread>

#define LED_PIN 0

int main()
{
    if (wiringPiSetup() == -1)
    {
        std::cerr << "Failed to init wiringPi!" << std::endl;
        return 1;
    }

    pinMode(LED_PIN, OUTPUT);

    std::cout << "Blinking LED on GPIO17 (Ctrl+C to stop)" << std::endl;

    while (true)
    {
        digitalWrite(LED_PIN, HIGH);
        std::this_thread::sleep_for(std::chrono::milliseconds(500));

        digitalWrite(LED_PIN, LOW);
        std::this_thread::sleep_for(std::chrono::milliseconds(500));
    }

    return 0;
}
