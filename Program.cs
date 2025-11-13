using System;
using System.Threading;

namespace SensorApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[SYSTEM] Initializing sensors and motors...");

            using var sensors = new Sensors(10);
            var motor = new MotorController();

            Console.WriteLine("[SYSTEM] Ready.");
            Console.WriteLine("Use arrow keys to control the robot. Press ESC to quit.");

            var sensorThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        sensors.SensorTask();
                        Thread.Sleep(500);
                    }
                }
                catch (ThreadInterruptedException)
                {
                    Console.WriteLine("[SENSORS] Thread stopped.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Sensor thread exception: {ex.Message}");
                }
            })
            {
                IsBackground = true,
                Name = "SensorThread"
            };

            sensorThread.Start();
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.UpArrow:
                            motor.Forward();
                            break;
                        case ConsoleKey.DownArrow:
                            motor.Backward();
                            break;
                        case ConsoleKey.LeftArrow:
                            motor.Left();
                            break;
                        case ConsoleKey.RightArrow:
                            motor.Right();
                            break;
                        case ConsoleKey.Escape:
                            Console.WriteLine("[SYSTEM] Exiting...");
                            motor.Stop();
                            motor.Cleanup();
                            sensorThread.Interrupt();
                            return;
                        default:
                            motor.Stop();
                            break;
                    }
                }
                else
                {
                    motor.Stop();
                }

                Thread.Sleep(100);
            }
        }
    }
}
